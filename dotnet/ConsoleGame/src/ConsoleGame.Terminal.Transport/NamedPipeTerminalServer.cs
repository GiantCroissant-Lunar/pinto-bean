using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ConsoleGame.Terminal.Transport;

public sealed class NamedPipeTerminalServer : IAsyncDisposable
{
    private readonly string _pipeName;
    private readonly TerminalSession _session;
    private readonly ILogger? _logger;
    private readonly CancellationTokenSource _cts = new();
    private readonly List<Task> _clientTasks = new();
    private Task? _acceptLoopTask;

    public NamedPipeTerminalServer(string pipeName, TerminalSession session, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(pipeName))
        {
            throw new ArgumentException("Pipe name cannot be null or whitespace.", nameof(pipeName));
        }

        _session = session ?? throw new ArgumentNullException(nameof(session));
        _pipeName = pipeName;
        _logger = logger;
    }

    public void Start()
    {
        if (_acceptLoopTask is not null)
        {
            throw new InvalidOperationException("Named pipe server already started.");
        }

        _acceptLoopTask = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Named pipe server listening on {PipeName}", _pipeName);

        while (!cancellationToken.IsCancellationRequested)
        {
            var pipe = CreateServerStream();
            try
            {
                await pipe.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                pipe.Dispose();
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Named pipe wait connection failed");
                pipe.Dispose();
                await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken).ConfigureAwait(false);
                continue;
            }

            var clientTask = HandleClientAsync(pipe, cancellationToken);
            lock (_clientTasks)
            {
                _clientTasks.Add(clientTask);
            }

            _ = clientTask.ContinueWith(t =>
            {
                lock (_clientTasks)
                {
                    _clientTasks.Remove(t);
                }
            }, TaskScheduler.Default);
        }
    }

    private NamedPipeServerStream CreateServerStream()
    {
        return new NamedPipeServerStream(
            _pipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.WriteThrough);
    }

    private async Task HandleClientAsync(NamedPipeServerStream stream, CancellationToken cancellationToken)
    {
        var sendLock = new SemaphoreSlim(1, 1);
        var clientName = $"pipe:{_pipeName}:{Guid.NewGuid():N}";
        TerminalClientHandle? handle = null;

        try
        {
            handle = _session.RegisterClient(clientName, async (frame, ct) =>
            {
                await sendLock.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    await TerminalFrameCodec.WriteFrameAsync(stream, frame, ct).ConfigureAwait(false);
                }
                finally
                {
                    sendLock.Release();
                }
            });

            while (!cancellationToken.IsCancellationRequested && stream.IsConnected)
            {
                var frame = await TerminalFrameCodec.ReadFrameAsync(stream, cancellationToken).ConfigureAwait(false);
                if (frame is null)
                {
                    break;
                }

                switch (frame.Value.Type)
                {
                    case TerminalFrameType.Input:
                        await handle.ForwardFrameAsync(frame.Value, cancellationToken).ConfigureAwait(false);
                        break;
                    case TerminalFrameType.Control:
                        await handle.BroadcastControlAsync(frame.Value.Payload, cancellationToken).ConfigureAwait(false);
                        break;
                    default:
                        _logger?.LogWarning("Ignoring unsupported frame type {FrameType} from named pipe client {ClientName}", frame.Value.Type, clientName);
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected during shutdown
        }
        catch (IOException ex) when (!stream.IsConnected)
        {
            _logger?.LogDebug(ex, "Named pipe client disconnected abruptly for {ClientName}", clientName);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Named pipe client handler failed for {ClientName}", clientName);
        }
        finally
        {
            if (handle is not null)
            {
                await handle.DisposeAsync().ConfigureAwait(false);
            }

            sendLock.Dispose();

            if (stream.IsConnected)
            {
                try
                {
                    stream.Disconnect();
                }
                catch
                {
                    // ignore
                }
            }

            stream.Dispose();
            _logger?.LogInformation("Named pipe client {ClientName} disconnected", clientName);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();

        if (_acceptLoopTask is not null)
        {
            try
            {
                await _acceptLoopTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
        }

        Task[] clientTasks;
        lock (_clientTasks)
        {
            clientTasks = _clientTasks.ToArray();
        }

        if (clientTasks.Length > 0)
        {
            try
            {
                await Task.WhenAll(clientTasks).ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        }

        _cts.Dispose();
        _logger?.LogInformation("Named pipe server {PipeName} stopped", _pipeName);
    }
}

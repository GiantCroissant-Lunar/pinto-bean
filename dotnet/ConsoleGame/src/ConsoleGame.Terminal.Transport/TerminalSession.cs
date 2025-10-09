using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ConsoleGame.Terminal.Transport;

public sealed class TerminalSessionOptions
{
    public int OutputBufferSize { get; set; } = 4096;
}

public sealed class TerminalSession : IAsyncDisposable
{
    private readonly Stream _stdin;
    private readonly Stream _stdout;
    private readonly Stream _stderr;
    private readonly SemaphoreSlim _stdinLock = new(1, 1);
    private readonly ConcurrentDictionary<Guid, ClientRegistration> _clients = new();
    private readonly ILogger? _logger;
    private readonly TerminalSessionOptions _options;
    private readonly CancellationTokenSource _cts = new();
    private Task? _stdoutTask;
    private Task? _stderrTask;
    private int _started;

    public TerminalSession(Stream stdin, Stream stdout, Stream stderr, TerminalSessionOptions? options = null, ILogger? logger = null)
    {
        _stdin = stdin ?? throw new ArgumentNullException(nameof(stdin));
        _stdout = stdout ?? throw new ArgumentNullException(nameof(stdout));
        _stderr = stderr ?? throw new ArgumentNullException(nameof(stderr));

        if (!_stdin.CanWrite)
        {
            throw new ArgumentException("STDIN stream must be writable", nameof(stdin));
        }

        if (!_stdout.CanRead)
        {
            throw new ArgumentException("STDOUT stream must be readable", nameof(stdout));
        }

        if (!_stderr.CanRead)
        {
            throw new ArgumentException("STDERR stream must be readable", nameof(stderr));
        }

        _options = options ?? new TerminalSessionOptions();
        _logger = logger;
    }

    public CancellationToken SessionCancellationToken => _cts.Token;

    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            return;
        }

        _stdoutTask = Task.Run(() => PumpOutputAsync(_stdout, TerminalFrameType.Stdout, _cts.Token));
        _stderrTask = Task.Run(() => PumpOutputAsync(_stderr, TerminalFrameType.Stderr, _cts.Token));
    }

    public TerminalClientHandle RegisterClient(string? name, Func<TerminalFrame, CancellationToken, ValueTask> sendAsync)
    {
        ArgumentNullException.ThrowIfNull(sendAsync);

        var id = Guid.NewGuid();
        var registration = new ClientRegistration(id, name ?? id.ToString(), sendAsync);
        if (!_clients.TryAdd(id, registration))
        {
            throw new InvalidOperationException("Failed to register client.");
        }

        _logger?.LogInformation("Terminal client {ClientName} ({ClientId}) connected", registration.Name, registration.Id);
        return new TerminalClientHandle(this, registration);
    }

    internal async ValueTask ForwardInputAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        await _stdinLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _stdin.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
            await _stdin.FlushAsync(cancellationToken).ConfigureAwait(false);
            _logger?.LogDebug("Forwarded {Length} bytes to STDIN: {Preview}", payload.Length, FormatPayloadPreview(payload.Span));
        }
        finally
        {
            _stdinLock.Release();
        }
    }

    internal ValueTask ForwardInputAsync(TerminalFrame frame, CancellationToken cancellationToken)
    {
        if (frame.Type != TerminalFrameType.Input)
        {
            throw new ArgumentException("Only input frames can be forwarded to STDIN.", nameof(frame));
        }

        return ForwardInputAsync(frame.Payload, cancellationToken);
    }

    internal ValueTask BroadcastControlAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken)
    {
        return BroadcastAsync(new TerminalFrame(TerminalFrameType.Control, payload.ToArray()), cancellationToken);
    }

    internal ValueTask RemoveClientAsync(Guid id, string name)
    {
        if (_clients.TryRemove(id, out _))
        {
            _logger?.LogInformation("Terminal client {ClientName} ({ClientId}) disconnected", name, id);
        }

        return ValueTask.CompletedTask;
    }

    private async Task PumpOutputAsync(Stream source, TerminalFrameType frameType, CancellationToken cancellationToken)
    {
        var buffer = new byte[_options.OutputBufferSize];
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                if (read <= 0)
                {
                    break;
                }

                var payload = buffer.AsSpan(0, read).ToArray();
                _logger?.LogDebug("Read {Length} bytes from {FrameType}: {Preview}", read, frameType, FormatPayloadPreview(payload));
                await BroadcastAsync(new TerminalFrame(frameType, payload), cancellationToken).ConfigureAwait(false);
            }
            _logger?.LogInformation("Output pump for {FrameType} reached end of stream", frameType);
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Terminal output pump failed for {FrameType}", frameType);
        }
    }

    private async ValueTask BroadcastAsync(TerminalFrame frame, CancellationToken cancellationToken)
    {
        if (_clients.IsEmpty)
        {
            return;
        }

        foreach (var kvp in _clients.ToArray())
        {
            var registration = kvp.Value;
            try
            {
                await registration.SendAsync(frame, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to deliver frame to client {ClientName} ({ClientId}); disconnecting", registration.Name, registration.Id);
                await RemoveClientAsync(registration.Id, registration.Name).ConfigureAwait(false);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        var tasks = new List<Task>();
        if (_stdoutTask is { } stdout)
        {
            tasks.Add(stdout);
        }

        if (_stderrTask is { } stderr)
        {
            tasks.Add(stderr);
        }

        if (tasks.Count > 0)
        {
            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch
            {
                // swallow exceptions during shutdown
            }
        }

        _stdinLock.Dispose();
        _cts.Dispose();
    }

    internal sealed record ClientRegistration(Guid Id, string Name, Func<TerminalFrame, CancellationToken, ValueTask> Sender)
    {
        public ValueTask SendAsync(TerminalFrame frame, CancellationToken cancellationToken)
        {
            return Sender(frame, cancellationToken);
        }
    }

    private static string FormatPayloadPreview(ReadOnlySpan<byte> payload)
    {
        if (payload.IsEmpty)
        {
            return "<empty>";
        }

        const int maxPreviewBytes = 80;
        var previewLength = Math.Min(payload.Length, maxPreviewBytes);

        string preview;
        try
        {
            preview = Encoding.UTF8.GetString(payload[..previewLength]);
        }
        catch
        {
            preview = Convert.ToHexString(payload[..previewLength].ToArray());
        }

        preview = preview
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");

        if (payload.Length > maxPreviewBytes)
        {
            preview += $"…(+{payload.Length - maxPreviewBytes} bytes)";
        }

        return preview;
    }
}

public sealed class TerminalClientHandle : IAsyncDisposable
{
    private readonly TerminalSession _session;
    private readonly Guid _id;
    private readonly string _name;
    private bool _disposed;

    internal TerminalClientHandle(TerminalSession session, TerminalSession.ClientRegistration registration)
    {
        _session = session;
        _id = registration.Id;
        _name = registration.Name;
    }

    public Guid Id => _id;

    public string Name => _name;

    public ValueTask ForwardInputAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        return _session.ForwardInputAsync(payload, cancellationToken);
    }

    public ValueTask ForwardFrameAsync(TerminalFrame frame, CancellationToken cancellationToken = default)
    {
        return _session.ForwardInputAsync(frame, cancellationToken);
    }

    public ValueTask BroadcastControlAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        return _session.BroadcastControlAsync(payload, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        return _session.RemoveClientAsync(_id, _name);
    }
}

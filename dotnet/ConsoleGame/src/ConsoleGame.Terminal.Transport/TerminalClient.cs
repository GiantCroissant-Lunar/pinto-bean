using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace ConsoleGame.Terminal.Transport;

public interface ITerminalClient : IAsyncDisposable
{
    ValueTask ConnectAsync(CancellationToken cancellationToken = default);
    ValueTask SendInputAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default);
    IAsyncEnumerable<TerminalFrame> ReadFramesAsync(CancellationToken cancellationToken = default);
}

public sealed class NamedPipeTerminalClient : ITerminalClient
{
    private readonly string _pipeName;
    private readonly Func<NamedPipeClientStream> _streamFactory;
    private readonly ILogger? _logger;
    private NamedPipeClientStream? _stream;
    private bool _disposed;

    public NamedPipeTerminalClient(string pipeName, ILogger? logger = null)
        : this(pipeName, () => new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough), logger)
    {
    }

    internal NamedPipeTerminalClient(string pipeName, Func<NamedPipeClientStream> streamFactory, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(pipeName))
        {
            throw new ArgumentException("Pipe name cannot be null or whitespace", nameof(pipeName));
        }

        _pipeName = pipeName;
        _streamFactory = streamFactory ?? throw new ArgumentNullException(nameof(streamFactory));
        _logger = logger;
    }

    public async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_stream is not null)
        {
            throw new InvalidOperationException("Client already connected.");
        }

        var stream = _streamFactory();
        try
        {
            await stream.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            stream.Dispose();
            throw;
        }

        _stream = stream;
        _logger?.LogInformation("Connected to named pipe {PipeName}", _pipeName);
    }

    public async ValueTask SendInputAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("Client is not connected.");
        }

        var frame = new TerminalFrame(TerminalFrameType.Input, payload.ToArray());
        await TerminalFrameCodec.WriteFrameAsync(_stream, frame, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<TerminalFrame> ReadFramesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_stream is null)
        {
            throw new InvalidOperationException("Client is not connected.");
        }

        while (!cancellationToken.IsCancellationRequested && _stream.IsConnected)
        {
            TerminalFrame? frame;
            try
            {
                frame = await TerminalFrameCodec.ReadFrameAsync(_stream, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch (IOException ex)
            {
                _logger?.LogWarning(ex, "Pipe read failed; disconnecting client {PipeName}", _pipeName);
                yield break;
            }

            if (frame is null)
            {
                yield break;
            }

            yield return frame.Value;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_stream is not null)
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
        }

        _logger?.LogInformation("Named pipe client {PipeName} disposed", _pipeName);
    }
}

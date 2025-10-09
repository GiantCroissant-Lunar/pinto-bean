using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleGame.Terminal.Transport;

public enum TerminalFrameType : byte
{
    Stdout = 1,
    Stderr = 2,
    Input = 3,
    Control = 4
}

public readonly struct TerminalFrame
{
    public TerminalFrame(TerminalFrameType type, byte[] payload)
    {
        Type = type;
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }

    public TerminalFrameType Type { get; }

    public byte[] Payload { get; }
}

public static class TerminalFrameCodec
{
    private const int HeaderSize = 5; // 1 byte type + 4 byte length (little-endian)

    public static async ValueTask WriteFrameAsync(Stream stream, TerminalFrame frame, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var header = new byte[HeaderSize];
        header[0] = (byte)frame.Type;
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(1), frame.Payload.Length);

        await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        if (frame.Payload.Length > 0)
        {
            await stream.WriteAsync(frame.Payload, cancellationToken).ConfigureAwait(false);
        }
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<TerminalFrame?> ReadFrameAsync(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var header = new byte[HeaderSize];
        var read = await FillBufferAsync(stream, header, cancellationToken).ConfigureAwait(false);
        if (read == 0)
        {
            return null; // EOF
        }

        if (read < HeaderSize)
        {
            throw new IOException("Unexpected end of stream while reading terminal frame header.");
        }

        var type = (TerminalFrameType)header[0];
        var length = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(1));
        if (length < 0)
        {
            throw new IOException("Terminal frame payload length cannot be negative.");
        }

        var payload = new byte[length];
        if (length > 0)
        {
            read = await FillBufferAsync(stream, payload, cancellationToken).ConfigureAwait(false);
            if (read < length)
            {
                throw new IOException("Unexpected end of stream while reading terminal frame payload.");
            }
        }

        return new TerminalFrame(type, payload);
    }

    private static async ValueTask<int> FillBufferAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(total), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return total;
    }
}

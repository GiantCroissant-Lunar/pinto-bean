using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ConsoleGame.Terminal.Transport;
using FluentAssertions;
using Xunit;

namespace ConsoleGame.Terminal.Transport.Tests;

public class TerminalFrameCodecTests
{
    [Fact]
    public async Task WriteAndReadFrame_RoundTripsSuccessfully()
    {
        var stream = new MemoryStream();
        var frame = new TerminalFrame(TerminalFrameType.Stdout, new byte[] { 1, 2, 3, 4 });

        await TerminalFrameCodec.WriteFrameAsync(stream, frame, CancellationToken.None);
        stream.Position = 0;

        var readFrame = await TerminalFrameCodec.ReadFrameAsync(stream, CancellationToken.None);
        readFrame.Should().NotBeNull();
        readFrame!.Value.Type.Should().Be(TerminalFrameType.Stdout);
        readFrame.Value.Payload.Should().Equal(new byte[] { 1, 2, 3, 4 });
    }

    [Fact]
    public async Task ReadFrame_ReturnsNullOnEof()
    {
        var stream = new MemoryStream();
        var frame = await TerminalFrameCodec.ReadFrameAsync(stream, CancellationToken.None);
        frame.Should().BeNull();
    }

    [Fact]
    public async Task WriteFrame_ThrowsForNullStream()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await TerminalFrameCodec.WriteFrameAsync(null!, new TerminalFrame(), CancellationToken.None);
        });
    }
}

using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConsoleGame.Terminal.Transport;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.IO.Pipelines;
using Xunit;

namespace ConsoleGame.Terminal.Transport.Tests;

public class TerminalSessionTests
{
    [Fact]
    public async Task BroadcastsStdoutToAllClients()
    {
        var stdinPipe = new Pipe();
        var stdoutPipe = new Pipe();
        var stderrPipe = new Pipe();

        await using var session = CreateSession(stdinPipe, stdoutPipe, stderrPipe);
        session.Start();

        var framesFromFirst = new List<TerminalFrame>();
        var framesFromSecond = new List<TerminalFrame>();
        var firstReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var firstHandle = session.RegisterClient("client-1", (frame, _) =>
        {
            lock (framesFromFirst)
            {
                framesFromFirst.Add(frame);
            }

            firstReceived.TrySetResult();
            return ValueTask.CompletedTask;
        });

        await using var secondHandle = session.RegisterClient("client-2", (frame, _) =>
        {
            lock (framesFromSecond)
            {
                framesFromSecond.Add(frame);
            }

            secondReceived.TrySetResult();
            return ValueTask.CompletedTask;
        });

        var payload = Encoding.UTF8.GetBytes("hello world");
        await stdoutPipe.Writer.WriteAsync(payload);
        await stdoutPipe.Writer.FlushAsync();

        await Task.WhenAll(firstReceived.Task.WaitAsync(TimeSpan.FromSeconds(1)), secondReceived.Task.WaitAsync(TimeSpan.FromSeconds(1)));

        framesFromFirst.Should().ContainSingle(f => f.Type == TerminalFrameType.Stdout && f.Payload.SequenceEqual(payload));
        framesFromSecond.Should().ContainSingle(f => f.Type == TerminalFrameType.Stdout && f.Payload.SequenceEqual(payload));

        await stdoutPipe.Writer.CompleteAsync();
        await stderrPipe.Writer.CompleteAsync();
        await stdinPipe.Reader.CompleteAsync();
    }

    [Fact]
    public async Task ForwardInputWritesToStdinStream()
    {
        var stdinPipe = new Pipe();
        var stdoutPipe = new Pipe();
        var stderrPipe = new Pipe();

        await using var session = CreateSession(stdinPipe, stdoutPipe, stderrPipe);
        session.Start();

        await using var handle = session.RegisterClient("client", (_, _) => ValueTask.CompletedTask);

        var payload = Encoding.UTF8.GetBytes("command\n");
        await handle.ForwardInputAsync(payload, CancellationToken.None);

        var readResult = await stdinPipe.Reader.ReadAsync();
        var buffer = readResult.Buffer.ToArray();
        buffer.Should().Equal(payload);
        stdinPipe.Reader.AdvanceTo(readResult.Buffer.End);

        await stdoutPipe.Writer.CompleteAsync();
        await stderrPipe.Writer.CompleteAsync();
        await stdinPipe.Reader.CompleteAsync();
    }

    private static TerminalSession CreateSession(Pipe stdinPipe, Pipe stdoutPipe, Pipe stderrPipe)
    {
        return new TerminalSession(
            stdinPipe.Writer.AsStream(),
            stdoutPipe.Reader.AsStream(),
            stderrPipe.Reader.AsStream(),
            new TerminalSessionOptions { OutputBufferSize = 1024 },
            NullLogger<TerminalSession>.Instance);
    }
}

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using FluentAssertions;
using Xunit;

namespace ConsoleGame.TerminalHost.SmokeTests;

public sealed class TerminalHostSmokeTests
{
    [Fact]
    public async Task TerminalHost_Should_Stream_ConsoleGame_Output()
    {
        var port = GetFreeTcpPort();
        using var globalTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));

        await using var host = await TerminalHostProcess.StartAsync(port, globalTimeout.Token);

        using var client = new ClientWebSocket();
        await ConnectWithRetriesAsync(client, new Uri($"ws://127.0.0.1:{port}/"), globalTimeout.Token);

        var initialResponse = await ReceiveStringAsync(client, globalTimeout.Token);
        if (string.IsNullOrWhiteSpace(initialResponse))
        {
            await SendStringAsync(client, "\n", globalTimeout.Token);
            initialResponse = await ReceiveStringAsync(client, globalTimeout.Token);
        }

        initialResponse.Should()
            .NotBeNullOrWhiteSpace("the terminal host should stream ConsoleGame output")
            .And.Contain("ConsoleGame.App", "ConsoleGame.App prints a startup banner that proves the process is running");

        if (client.State == WebSocketState.Open)
        {
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "test-complete", globalTimeout.Token);
        }
    }

    private static async Task ConnectWithRetriesAsync(ClientWebSocket client, Uri uri, CancellationToken cancellationToken)
    {
        const int maxAttempts = 10;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await client.ConnectAsync(uri, cancellationToken);
                return;
            }
            catch (WebSocketException) when (attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            }
        }

        throw new InvalidOperationException($"Unable to connect to terminal host at {uri} after {maxAttempts} attempts.");
    }

    private static async Task SendStringAsync(ClientWebSocket client, string value, CancellationToken cancellationToken)
    {
        var payload = Encoding.UTF8.GetBytes(value);
        await client.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Text, true, cancellationToken);
    }

    private static async Task<string> ReceiveStringAsync(ClientWebSocket client, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            var builder = new StringBuilder();
            while (true)
            {
                var segment = new ArraySegment<byte>(buffer);
                var result = await client.ReceiveAsync(segment, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.Count > 0)
                {
                    builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }

                if (result.EndOfMessage)
                {
                    break;
                }
            }

            return builder.ToString();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed class TerminalHostProcess : IAsyncDisposable
    {
        private TerminalHostProcess(Process process, int port)
        {
            Process = process;
            Port = port;
        }

        private Process Process { get; }
        public int Port { get; }

        public static async Task<TerminalHostProcess> StartAsync(int port, CancellationToken cancellationToken)
        {
            var (hostProjectDir, hostAssemblyPath) = ResolveTerminalHostPaths();

            if (!File.Exists(hostAssemblyPath))
            {
                throw new FileNotFoundException($"Terminal host assembly not found at '{hostAssemblyPath}'. Ensure the solution is built before running smoke tests.");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{hostAssemblyPath}\"",
                WorkingDirectory = hostProjectDir,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            startInfo.Environment["CONSOLE_GAME_WS_PORT"] = port.ToString(CultureInfo.InvariantCulture);

            var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start ConsoleGame.TerminalHost process.");

            try
            {
                await WaitForPortAsync(port, TimeSpan.FromSeconds(10), cancellationToken);
                return new TerminalHostProcess(process, port);
            }
            catch
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync(cancellationToken);
                }

                process.Dispose();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (!Process.HasExited)
                {
                    Process.Kill(entireProcessTree: true);
                }

                await Process.WaitForExitAsync();
            }
            catch (InvalidOperationException)
            {
                // Process already exited.
            }
            finally
            {
                Process.Dispose();
            }
        }

        private static (string projectDirectory, string assemblyPath) ResolveTerminalHostPaths()
        {
            var testBaseDirectory = AppContext.BaseDirectory;
            var configuration = new DirectoryInfo(testBaseDirectory).Parent?.Name ?? "Debug";
            var solutionRoot = Path.GetFullPath(Path.Combine(testBaseDirectory, "..", "..", "..", "..", ".."));
            var hostProjectDirectory = Path.Combine(solutionRoot, "src", "ConsoleGame.TerminalHost");
            var hostAssemblyPath = Path.Combine(hostProjectDirectory, "bin", configuration, "net8.0", "ConsoleGame.TerminalHost.dll");
            return (hostProjectDirectory, hostAssemblyPath);
        }

        private static async Task WaitForPortAsync(int port, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    using var client = new TcpClient();
                    await client.ConnectAsync(IPAddress.Loopback, port, cancellationToken);
                    return;
                }
                catch (SocketException)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
                }
            }

            throw new TimeoutException($"Port {port} did not open within {timeout.TotalSeconds:F1} seconds.");
        }
    }
}

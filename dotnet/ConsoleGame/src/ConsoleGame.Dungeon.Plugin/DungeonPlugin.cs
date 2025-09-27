using ConsoleGame.Contracts;
using ConsoleGame.Dungeon.Core;
using ConsoleGame.Dungeon.Logging;
using ConsoleGame.Dungeon.UI.Terminal;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace ConsoleGame.Dungeon.Plugin;

internal sealed class UnloadSentinel
{
    private readonly string _name;

    public UnloadSentinel(string name)
    {
        _name = name;
    }

    ~UnloadSentinel()
    {
        Console.WriteLine($"[Sentinel] {_name} finalized");
    }
}

internal sealed class NativeStderrCapture : IDisposable
{
    private const int STD_ERROR_HANDLE = -12;
    private const uint DUPLICATE_SAME_ACCESS = 0x00000002;

    private readonly string _logPath;
    private readonly ILogService? _log;
    private readonly CancellationTokenSource _cts = new();
    private readonly SafeFileHandle? _pipeRead;
    private readonly SafeFileHandle? _pipeWrite;
    private readonly SafeFileHandle? _originalDuplicate;
    private readonly IntPtr _originalHandle;
    private readonly FileStream? _logStream;
    private readonly FileStream? _readerStream;
    private readonly FileStream? _originalStream;
    private readonly Thread? _readerThread;
    private readonly StreamWriter? _pipeWriter;
    private readonly object _disposeGate = new();
    private bool _disposed;

    private NativeStderrCapture(string logPath, ILogService? log)
    {
        _logPath = logPath;
        _log = log;

        var directory = Path.GetDirectoryName(_logPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _logStream = new FileStream(_logPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        var header = Encoding.UTF8.GetBytes($"# Native stderr capture started {DateTime.Now:u}{Environment.NewLine}");
        _logStream.Write(header, 0, header.Length);
        _logStream.Flush();

        if (!CreatePipe(out var readPipe, out var writePipe, IntPtr.Zero, 0))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create pipe for stderr capture.");
        }

        _pipeRead = readPipe;
        _pipeWrite = writePipe;

        var readHandle = new SafeFileHandle(_pipeRead.DangerousGetHandle(), ownsHandle: false);
        _readerStream = new FileStream(readHandle, FileAccess.Read, 4096, isAsync: false);

        _originalHandle = GetStdHandle(STD_ERROR_HANDLE);
        if (_originalHandle == IntPtr.Zero || _originalHandle == new IntPtr(-1))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to query current stderr handle.");
        }

        if (!DuplicateHandle(GetCurrentProcess(), _originalHandle, GetCurrentProcess(), out var duplicated, 0, true, DUPLICATE_SAME_ACCESS))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to duplicate stderr handle.");
        }

        _originalDuplicate = duplicated;
        var duplicateHandle = new SafeFileHandle(_originalDuplicate.DangerousGetHandle(), ownsHandle: false);
        _originalStream = new FileStream(duplicateHandle, FileAccess.Write, 4096, isAsync: false);

        if (!SetStdHandle(STD_ERROR_HANDLE, _pipeWrite.DangerousGetHandle()))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to redirect stderr to capture pipe.");
        }

        var writerHandle = new SafeFileHandle(_pipeWrite.DangerousGetHandle(), ownsHandle: false);
        _pipeWriter = new StreamWriter(new FileStream(writerHandle, FileAccess.Write, 4096, isAsync: false))
        {
            AutoFlush = true
        };
        Console.SetError(_pipeWriter);

        _readerThread = new Thread(ReadLoop)
        {
            IsBackground = true,
            Name = "NativeStderrCapture"
        };
        _readerThread.Start();
    }

    public static NativeStderrCapture? TryStart(string logPath, ILogService? log)
    {
        if (!OperatingSystem.IsWindows())
        {
            log?.Info("Native stderr capture is only supported on Windows; skipping.");
            return null;
        }

        try
        {
            var capture = new NativeStderrCapture(logPath, log);
            log?.Info($"Native stderr capture active at '{logPath}'.");
            return capture;
        }
        catch (Exception ex)
        {
            log?.Warn($"Failed to start native stderr capture: {ex.Message}");
            return null;
        }
    }

    public string LogPath => _logPath;

    private void ReadLoop()
    {
        if (_readerStream is null || _logStream is null)
        {
            return;
        }

        var buffer = new byte[4096];
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                var read = _readerStream.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    break;
                }

                _logStream.Write(buffer, 0, read);
                _logStream.Flush();

                if (_originalStream is not null)
                {
                    _originalStream.Write(buffer, 0, read);
                    _originalStream.Flush();
                }
            }
        }
        catch (ObjectDisposedException)
        {
            // Expected during shutdown
        }
        catch (IOException)
        {
            // Pipe closed; safe to exit
        }
        catch (Exception ex)
        {
            _log?.Warn($"Native stderr capture loop terminated: {ex.Message}");
        }
    }

    public void Dispose()
    {
        lock (_disposeGate)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
        }

        try
        {
            _cts.Cancel();
            try { _pipeWriter?.Flush(); } catch { }
            try { _pipeWriter?.Dispose(); } catch { }
            try { _pipeWrite?.Dispose(); } catch { }

            try { _readerStream?.Dispose(); } catch { }
            try { _pipeRead?.Dispose(); } catch { }

            if (_readerThread is not null)
            {
                try { _readerThread.Join(TimeSpan.FromSeconds(5)); } catch { }
            }

            SetStdHandle(STD_ERROR_HANDLE, _originalHandle);
            var restored = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
            Console.SetError(restored);

            try { _originalStream?.Dispose(); } catch { }
            try { _originalDuplicate?.Dispose(); } catch { }

            try { _logStream?.Flush(); } catch { }
            try { _logStream?.Dispose(); } catch { }
        }
        catch (Exception ex)
        {
            _log?.Warn($"Failed to dispose native stderr capture cleanly: {ex.Message}");
        }
        finally
        {
            _cts.Dispose();
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, IntPtr lpPipeAttributes, int nSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetStdHandle(int nStdHandle, IntPtr hHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, IntPtr hSourceHandle, IntPtr hTargetProcessHandle, out SafeFileHandle lpTargetHandle, uint dwDesiredAccess, bool bInheritHandle, uint dwOptions);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();
}

internal sealed class CompositionRoot : IDisposable
{
    public DungeonViewModel ViewModel { get; }
    public IAppLoop Loop { get; }
    public ILogService LogService { get; }

    private UnloadSentinel? _sentinel = new("DungeonPlugin.CompositionRoot");

    public CompositionRoot(DungeonViewModel viewModel, AppLoop appLoop, BareAppLoop bareAppLoop, IAudioService audio, ILogService logService)
    {
        ViewModel = viewModel;
        LogService = logService;

        var disableReason = DungeonComposition.GetAudioDisableReason();
        if (disableReason is not null)
        {
            LogService.Warn(disableReason);
        }
        else if (!audio.IsEnabled)
        {
            LogService.Warn("Audio service reported disabled.");
        }

        var bareLoopEnv = Environment.GetEnvironmentVariable("CONSOLEGAME_DEBUG_BARE_LOOP");
        if (!string.IsNullOrWhiteSpace(bareLoopEnv) && (string.Equals(bareLoopEnv, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(bareLoopEnv, "true", StringComparison.OrdinalIgnoreCase)))
        {
            LogService.Info("Using bare app loop (CONSOLEGAME_DEBUG_BARE_LOOP).");
            Loop = bareAppLoop;
        }
        else
        {
            Loop = appLoop;
        }
    }

    public void Dispose()
    {
        _sentinel = null;
    }
}

[Plugin("consolegame.dungeon", "Dungeon", "0.1.0", Description = "Dungeon TUI with reactive state and audio effects")]
public sealed class DungeonPlugin : IPlugin, IRuntimePlugin, IDisposable
{
    public string Name => "Dungeon";
    public IPluginContext? Context { get; set; }
    public string Describe() => "A tiny dungeon demo";

    private CompositionRoot? _root;
    private string? _pluginDirectory;

    private CompositionRoot EnsureRoot(IServiceProvider? services)
        => _root ??= DungeonComposition.Resolve<CompositionRoot>(services);

    public Task ConfigureAsync(CancellationToken cancellationToken = default)
    {
        var root = EnsureRoot(Context?.Services);
        if (Context?.PluginDirectory is { Length: > 0 } pluginDir)
        {
            _pluginDirectory = pluginDir;
            var logService = root.LogService;
            logService.SetBaseDirectory(pluginDir);
            logService.Info($"Log directory set to {pluginDir}");
        }
        return Task.CompletedTask;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var root = EnsureRoot(Context?.Services);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, Context?.ShutdownToken ?? default);
        var logService = root.LogService;
        NativeStderrCapture? stderrCapture = null;

        try
        {
            var captureEnabled = !string.Equals(Environment.GetEnvironmentVariable("CONSOLEGAME_DISABLE_NATIVE_CAPTURE"), "1", StringComparison.OrdinalIgnoreCase);
            if (OperatingSystem.IsWindows() && captureEnabled)
            {
                var baseDir = ResolvePluginDirectory();
                if (!string.IsNullOrWhiteSpace(baseDir))
                {
                    try
                    {
                        var assetsDir = Path.Combine(baseDir!, "assets");
                        Directory.CreateDirectory(assetsDir);
                        var stderrLogPath = Path.Combine(assetsDir, "dungeon-plugin-native-stderr.log");
                        stderrCapture = NativeStderrCapture.TryStart(stderrLogPath, logService);
                    }
                    catch (Exception ex)
                    {
                        logService?.Warn($"Failed to prepare native stderr capture: {ex.Message}");
                    }
                }
            }

            await root.Loop.RunAsync(linkedCts.Token);
        }
        finally
        {
            stderrCapture?.Dispose();
            if (stderrCapture is not null)
            {
                logService?.Info($"Native stderr log file: {stderrCapture.LogPath}");
            }
            DungeonComposition.Reset();
            _root = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    public void Dispose()
    {
        DungeonComposition.Reset();
        _root = null;
    }

    private string? ResolvePluginDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_pluginDirectory))
        {
            return _pluginDirectory;
        }

        if (!string.IsNullOrWhiteSpace(Context?.PluginDirectory))
        {
            _pluginDirectory = Context!.PluginDirectory;
            return _pluginDirectory;
        }

        var asmDir = Path.GetDirectoryName(typeof(DungeonPlugin).Assembly.Location);
        if (!string.IsNullOrWhiteSpace(asmDir))
        {
            _pluginDirectory = asmDir;
            return _pluginDirectory;
        }

        return AppContext.BaseDirectory;
    }
}

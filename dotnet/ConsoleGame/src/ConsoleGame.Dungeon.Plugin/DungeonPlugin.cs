using ConsoleGame.Contracts;
using Microsoft.Win32.SafeHandles;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Gui;
using LibVLCSharp.Shared;

namespace ConsoleGame.Dungeon.Plugin;

public readonly record struct MoveInput(int Dx, int Dy);
public readonly record struct PlayerMoved((int X, int Y) Pos);

internal enum LogLevel
{
    Info,
    Warning,
    Error
}

internal readonly record struct LogEntry(LogLevel Level, string Message, Exception? Exception, DateTime Timestamp)
{
    public override string ToString()
    {
        var prefix = Level switch
        {
            LogLevel.Info => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            _ => Level.ToString().ToUpperInvariant()
        };
        var baseMessage = $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{prefix}] {Message}";
        return Exception is null ? baseMessage : $"{baseMessage} ({Exception.GetType().Name}: {Exception.Message})";
    }
}

internal interface ILogService
{
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? exception = null);
    bool HasEntries { get; }
    IReadOnlyList<LogEntry> Snapshot();
    IReadOnlyList<LogEntry> Flush();
    string? LogPath { get; }
    void SetBaseDirectory(string baseDirectory);
}

internal sealed class LogBuffer : ILogService
{
    private readonly List<LogEntry> _entries = new();
    private readonly object _sync = new();
    private string? _logFilePath;
    private string? _overrideBaseDirectory;
    public string? LogPath { get { lock (_sync) { EnsureLogPathInitialized(); return _logFilePath; } } }

    public bool HasEntries
    {
        get
        {
            lock (_sync)
            {
                return _entries.Count > 0;
            }
        }
    }

    public void Info(string message) => Add(LogLevel.Info, message, null);
    public void Warn(string message) => Add(LogLevel.Warning, message, null);
    public void Error(string message, Exception? exception = null) => Add(LogLevel.Error, message, exception);

    public IReadOnlyList<LogEntry> Snapshot()
    {
        lock (_sync)
        {
            return _entries.ToArray();
        }
    }

    public IReadOnlyList<LogEntry> Flush()
    {
        lock (_sync)
        {
            var copy = _entries.ToArray();
            _entries.Clear();
            return copy;
        }
    }

    private void Add(LogLevel level, string message, Exception? exception)
    {
        lock (_sync)
        {
            var entry = new LogEntry(level, message, exception, DateTime.Now);
            _entries.Add(entry);

            EnsureLogPathInitialized();
            if (_logFilePath is not null)
            {
                TryAppendToFile(entry);
            }
        }
    }

    public void SetBaseDirectory(string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory)) return;
        lock (_sync)
        {
            _overrideBaseDirectory = baseDirectory;
            _logFilePathInitialized = false;
            _logFilePath = null;
        }
    }

    private void EnsureLogPathInitialized()
    {
        if (_logFilePathInitialized)
        {
            return;
        }

        _logFilePathInitialized = true;
        try
        {
            var baseDir = _overrideBaseDirectory ?? Path.GetDirectoryName(typeof(LogBuffer).Assembly.Location) ?? AppContext.BaseDirectory;
            var assetsDir = Path.Combine(baseDir, "assets");
            Directory.CreateDirectory(assetsDir);
            _logFilePath = Path.Combine(assetsDir, "dungeon-plugin.log");
            if (!string.IsNullOrEmpty(_logFilePath) && !File.Exists(_logFilePath))
            {
                File.WriteAllText(_logFilePath, $"# Dungeon plugin log created {DateTime.Now:u}{Environment.NewLine}");
            }
        }
        catch
        {
            _logFilePath = null;
        }
    }

    private void TryAppendToFile(LogEntry entry)
    {
        if (_logFilePath is null)
        {
            return;
        }

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine(entry.ToString());
            if (entry.Exception is { } ex)
            {
                sb.AppendLine(ex.ToString());
            }
            File.AppendAllText(_logFilePath, sb.ToString());
        }
        catch
        {
            // Swallow file IO issues; logging should not break gameplay
        }
    }

    private bool _logFilePathInitialized;
}

internal interface IAudioService : IDisposable
{
    void Play(string pathOrUrl);
}

internal sealed class NoAudioService : IAudioService
{
    private readonly ILogService _log;
    private readonly string _reason;

    public NoAudioService(ILogService log, string reason)
    {
        _log = log;
        _reason = reason;
        _log.Warn($"Audio disabled: {_reason}");
    }

    public void Play(string pathOrUrl)
    {
        // Intentionally no-op
    }

    public void Dispose()
    {
        // Nothing to release
    }
}

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

internal sealed class VlcAudioService : IAudioService
{
    private static readonly (string Name, string[] Options)[] BackendOrder = new[]
    {
        ("waveout", new[]{ "--quiet", "--aout=waveout" }),
        ("wasapi", new[]{ "--quiet", "--aout=wasapi" }),
        ("directsound", new[]{ "--quiet", "--aout=directsound" }),
        ("default", new[]{ "--quiet" })
    };

    private LibVLC _vlc;
    private MediaPlayer? _player;
    private Media? _currentMedia;
    private readonly object _gate = new();
    private bool _audioEnabled;
    private string _outputModule = "unknown";
    private readonly ILogService _log;
    private bool _logHooked;
    private int _fallbackScheduled;
    public VlcAudioService(ILogService log)
    {
        _log = log;
        Core.Initialize();
        LibVLC? created = null;
        Exception? lastError = null;

        foreach (var attempt in BackendOrder)
        {
            try
            {
                created = attempt.Options.Length == 0 ? new LibVLC() : new LibVLC(attempt.Options);
                _audioEnabled = true;
                _outputModule = attempt.Name;
                HookLibVlcLogs(created);
                _log.Info($"LibVLC audio initialized using '{_outputModule}' module.");
                break;
            }
            catch (Exception ex)
            {
                lastError = ex;
                _log.Warn($"LibVLC audio init failed for module '{attempt.Name}': {ex.Message}");
                created?.Dispose();
                created = null;
            }
        }

        if (created is null)
        {
            // Disable audio but keep LibVLC available so the plugin can continue running.
            try
            {
                created = new LibVLC("--quiet", "--no-audio");
                _audioEnabled = false;
                _outputModule = "disabled";
                HookLibVlcLogs(created);
                if (lastError is not null)
                {
                    _log.Error("Audio disabled; all LibVLC audio backends failed to initialize.", lastError);
                }
                else
                {
                    _log.Error("Audio disabled; LibVLC audio backends unavailable.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize LibVLC for audio playback.", ex);
            }
        }

        _vlc = created;
    }
    public void Play(string pathOrUrl)
    {
        if (!_audioEnabled)
        {
            _log.Warn($"Audio playback skipped because module '{_outputModule}' is unavailable. Requested media: {Path.GetFileName(pathOrUrl)}");
            return;
        }
        try
        {
            lock (_gate)
            {
                _player ??= new MediaPlayer(_vlc);
                if (_player.IsPlaying)
                {
                    _player.Stop();
                }
                _currentMedia?.Dispose();
                _currentMedia = new Media(_vlc, pathOrUrl, FromType.FromPath);
                _player.Play(_currentMedia);
            }
        }
        catch (Exception ex)
        {
            // best-effort; ignore playback errors
            _log.Error($"Audio playback failed for '{pathOrUrl}'.", ex);
        }
    }
    public void Dispose()
    {
        try
        {
            lock (_gate)
            {
                if (_player is { IsPlaying: true })
                {
                    _player.Stop();
                }
                _currentMedia?.Dispose();
                _player?.Dispose();
            }
        }
        finally
        {
            DetachLogHook(_vlc);
            _vlc.Dispose();
        }
    }

    private void HookLibVlcLogs(LibVLC vlc)
    {
        vlc.Log += HandleVlcLog;
        _logHooked = true;
    }

    private void DetachLogHook(LibVLC vlc)
    {
        if (!_logHooked)
        {
            return;
        }

        try { vlc.Log -= HandleVlcLog; } catch { }
        _logHooked = false;
    }

    private void HandleVlcLog(object? sender, LogEventArgs e)
    {
        var message = e.FormattedLog ?? e.Message ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(message) && message.Contains("directsound output error", StringComparison.OrdinalIgnoreCase))
        {
            ScheduleDirectSoundFallback();
        }

        switch (e.Level)
        {
            case LibVLCSharp.Shared.LogLevel.Error:
                _log.Error($"LibVLC: {message}");
                break;
            case LibVLCSharp.Shared.LogLevel.Warning:
                _log.Warn($"LibVLC: {message}");
                break;
            case LibVLCSharp.Shared.LogLevel.Notice:
            case LibVLCSharp.Shared.LogLevel.Debug:
            default:
                _log.Info($"LibVLC: {message}");
                break;
        }
    }

    private void ScheduleDirectSoundFallback()
    {
        if (!_audioEnabled)
        {
            return;
        }

        if (!string.Equals(_outputModule, "directsound", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _fallbackScheduled, 1, 0) != 0)
        {
            return;
        }

        _log.Warn("DirectSound reported playback errors; attempting to switch audio backends.");
        Task.Run(AttemptFallbackAfterDirectSoundFailure);
    }

    private void AttemptFallbackAfterDirectSoundFailure()
    {
        try
        {
            foreach (var candidate in BackendOrder)
            {
                if (string.Equals(candidate.Name, "directsound", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(candidate.Name, _outputModule, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (TryReinitializeAudio(candidate.Name, candidate.Options))
                {
                    return;
                }
            }

            DisableAudioDueToFailure();
        }
        finally
        {
            Interlocked.Exchange(ref _fallbackScheduled, 0);
        }
    }

    private bool TryReinitializeAudio(string moduleName, string[] options)
    {
        LibVLC? replacement = null;
        LibVLC? previous = null;

        try
        {
            replacement = options.Length == 0 ? new LibVLC() : new LibVLC(options);

            lock (_gate)
            {
                previous = _vlc;
                DetachLogHook(previous);
                _player?.Dispose();
                _player = null;
                _currentMedia?.Dispose();
                _currentMedia = null;

                _vlc = replacement;
                HookLibVlcLogs(_vlc);
                _outputModule = moduleName;
                _audioEnabled = !string.Equals(moduleName, "disabled", StringComparison.OrdinalIgnoreCase);
                replacement = null;
            }

            previous?.Dispose();
            _log.Info($"LibVLC audio switched to '{_outputModule}' backend.");
            return _audioEnabled;
        }
        catch (Exception ex)
        {
            _log.Warn($"LibVLC audio init failed for fallback module '{moduleName}': {ex.Message}");
            replacement?.Dispose();
            return false;
        }
    }

    private void DisableAudioDueToFailure()
    {
        lock (_gate)
        {
            _audioEnabled = false;
            _currentMedia?.Dispose();
            _currentMedia = null;
            _player?.Dispose();
            _player = null;
        }

        _log.Error("LibVLC audio disabled after DirectSound failures; see native stderr log for details.");
    }
}

public sealed class DungeonViewModel : ReactiveObject
{
    private (int X, int Y) _player = (1, 1);
    public (int X, int Y) Player
    {
        get => _player;
        set => this.RaiseAndSetIfChanged(ref _player, value);
    }

    public int Width { get; }
    public int Height { get; }

    public ObservableCollection<(int X, int Y, char Glyph)> Entities { get; } = new();

    public DungeonViewModel(int width = 20, int height = 10)
    {
        Width = width; Height = height;
        // Add a player entity glyph for completeness, though we render from Player directly
        Entities.Add((Player.X, Player.Y, '@'));
    }

    public void MoveBy(int dx, int dy)
    {
        var nx = Math.Clamp(Player.X + dx, 0, Width - 1);
        var ny = Math.Clamp(Player.Y + dy, 0, Height - 1);
        Player = (nx, ny);
    }
}

public interface IAppLoop { Task RunAsync(CancellationToken ct); }

internal sealed class MessageChannel<T>
{
    private readonly List<Action<T>> _handlers = new();
    private readonly object _gate = new();

    public IDisposable Subscribe(Action<T> handler)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        lock (_gate)
        {
            _handlers.Add(handler);
        }

        return Disposable.Create(() =>
        {
            lock (_gate)
            {
                _handlers.Remove(handler);
            }
        });
    }

    public void Publish(T message)
    {
        Action<T>[] snapshot;
        lock (_gate)
        {
            snapshot = _handlers.ToArray();
        }

        foreach (var handler in snapshot)
        {
            handler(message);
        }
    }
}

internal sealed class MoveInputHandler
{
    private readonly DungeonViewModel _vm;
    private readonly MessageChannel<PlayerMoved> _movedChannel;
    public MoveInputHandler(DungeonViewModel vm, MessageChannel<PlayerMoved> movedChannel) { _vm = vm; _movedChannel = movedChannel; }
    public void Handle(MoveInput message)
    {
        _vm.MoveBy(message.Dx, message.Dy);
        _movedChannel.Publish(new PlayerMoved(_vm.Player));
    }
}

internal sealed class PlayerMovedHandler
{
    private readonly Action _render;
    private readonly IAudioService _audio;
    private readonly string? _stepSound;
    public PlayerMovedHandler(Action render, IAudioService audio, string? stepSoundPath)
    {
        _render = render; _audio = audio; _stepSound = stepSoundPath;
    }
    public void Handle(PlayerMoved message)
    {
        _render();
        if (!string.IsNullOrWhiteSpace(_stepSound))
        {
            try { _audio.Play(_stepSound!); } catch { /* best-effort */ }
        }
    }
}

internal sealed class AppLoop : IAppLoop
{
    private readonly DungeonViewModel _vm;
    private readonly MessageChannel<MoveInput> _moveChannel = new();
    private readonly MessageChannel<PlayerMoved> _movedChannel = new();
    private readonly IAudioService _audio;
    private readonly string? _stepSoundPath;
    private readonly ILogService _log;

    public AppLoop(DungeonViewModel vm, IAudioService audio, ILogService log, string? stepSoundPath = null)
    {
        _vm = vm; _audio = audio; _log = log;
        if (!string.IsNullOrWhiteSpace(stepSoundPath)) { _stepSoundPath = stepSoundPath; }
        else
        {
            try
            {
                var asmDir = Path.GetDirectoryName(typeof(AppLoop).Assembly.Location);
                var candidates = new[] { "step.wav", "step.ogg", Path.Combine("assets","step.wav"), Path.Combine("assets","step.ogg") };
                foreach (var c in candidates)
                {
                    var p = asmDir is null ? null : Path.Combine(asmDir, c);
                    if (!string.IsNullOrEmpty(p) && File.Exists(p)) { _stepSoundPath = p; break; }
                }
                // If no sound exists, generate a tiny step.wav into assets
                if (string.IsNullOrEmpty(_stepSoundPath) && asmDir is not null)
                {
                    _stepSoundPath = AudioFileGenerator.GenerateStepWav(asmDir);
                }
            }
            catch { /* ignore */ }
        }

        if (!string.IsNullOrEmpty(_stepSoundPath))
        {
            _log.Info($"Using step sound asset '{_stepSoundPath}'.");
        }
        else
        {
            _log.Warn("No step sound asset found; movement will be silent.");
        }
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var headlessEnv = Environment.GetEnvironmentVariable("CONSOLEGAME_TUI_HEADLESS");
        var isHeadless = string.Equals(headlessEnv, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(headlessEnv, "true", StringComparison.OrdinalIgnoreCase);

        if (isHeadless)
        {
            _log.Info("Running dungeon loop in headless mode.");
            try
            {
                await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _log.Info("Headless mode cancelled.");
            }
            finally
            {
                FlushLogs();
            }

            return;
        }

        Application.Init();
        var top = new Toplevel() { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };

        var window = new Window { Title = "Dungeon", X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill() };
        top.Add(window);

        // Label-based grid
        var rows = new List<Label>(_vm.Height);
        for (int y = 0; y < _vm.Height; y++)
        {
            var lbl = new Label() { Text = string.Empty, X = 0, Y = y, Width = _vm.Width, Height = 1 };
            rows.Add(lbl); window.Add(lbl);
        }

        void Render()
        {
            for (int y = 0; y < _vm.Height; y++)
            {
                var chars = new char[_vm.Width];
                Array.Fill(chars, '.');
                if (y == _vm.Player.Y && _vm.Player.X >= 0 && _vm.Player.X < _vm.Width)
                {
                    chars[_vm.Player.X] = '@';
                }
                rows[y].Text = new string(chars);
            }
        }

        // Inputs -> VM update
        var miHandler = new MoveInputHandler(_vm, _movedChannel);
        var pmHandler = new PlayerMovedHandler(Render, _audio, _stepSoundPath);
        var disposables = new CompositeDisposable();
        var sub1 = _moveChannel.Subscribe(miHandler.Handle);
        var sub2 = _movedChannel.Subscribe(pmHandler.Handle);
        disposables.Add(sub1);
        disposables.Add(sub2);

        // Key handling publishes MoveInput
        EventHandler<Key> keyHandler = (object? _, Key key) =>
        {
            if (key == Key.CursorLeft) { _moveChannel.Publish(new MoveInput(-1, 0)); }
            else if (key == Key.CursorRight) { _moveChannel.Publish(new MoveInput(1, 0)); }
            else if (key == Key.CursorUp) { _moveChannel.Publish(new MoveInput(0, -1)); }
            else if (key == Key.CursorDown) { _moveChannel.Publish(new MoveInput(0, 1)); }
            else if (key == Key.Esc)
            {
                // Allow explicit quit via Esc or Q / Ctrl+Q
                Application.RequestStop();
            }
        };
        top.KeyDown += keyHandler;

        Render();
        using var reg = ct.Register(() => Application.RequestStop());
        try
        {
            // Run on the same thread as Init and view setup to avoid cross-thread issues.
            Application.Run(top);
        }
        finally
        {
            top.KeyDown -= keyHandler;
            Application.RequestStop();
            Application.Shutdown();
            window.Dispose();
            top.Dispose();
            disposables.Dispose();
            FlushLogs();
        }

        return;
    }

    private void FlushLogs()
    {
        var entries = _log.Flush();
        foreach (var entry in entries)
        {
            var line = entry.ToString();
            if (entry.Level == LogLevel.Error)
            {
                Console.Error.WriteLine(line);
            }
            else
            {
                Console.WriteLine(line);
            }

            if (entry.Exception is { } ex)
            {
                Console.Error.WriteLine(ex);
            }
        }

        var logPath = _log.LogPath;
        if (!string.IsNullOrWhiteSpace(logPath))
        {
            Console.WriteLine($"Log file: {logPath}");
        }
    }
}

internal static class AudioFileGenerator
{
    // Generate a short 90ms mono 16-bit PCM sine burst WAV at 440Hz with a quick attack/decay
    public static string GenerateStepWav(string pluginDir)
    {
        var assetsDir = Path.Combine(pluginDir, "assets");
        Directory.CreateDirectory(assetsDir);
        var path = Path.Combine(assetsDir, "step.wav");
        if (File.Exists(path)) return path;

        const int sampleRate = 44100;
        const double durationSec = 0.09; // 90 ms
        const double freq = 440.0; // A4
        int samples = (int)(sampleRate * durationSec);
        short[] data = new short[samples];

        for (int n = 0; n < samples; n++)
        {
            double t = (double)n / sampleRate;
            // Simple envelope: 10ms attack, 60ms sustain, 20ms decay
            double attack = 0.01, decayStart = 0.07;
            double amp;
            if (t < attack) amp = t / attack; // ramp up
            else if (t < decayStart) amp = 1.0; // sustain
            else amp = Math.Max(0, 1.0 - (t - decayStart) / (durationSec - decayStart)); // decay
            double s = Math.Sin(2 * Math.PI * freq * t) * amp;
            data[n] = (short)(s * short.MaxValue * 0.5);
        }

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        int channels = 1;
        int bitsPerSample = 16;
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        short blockAlign = (short)(channels * bitsPerSample / 8);
        int subchunk2Size = data.Length * blockAlign;
        int chunkSize = 36 + subchunk2Size;

        // RIFF header
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(chunkSize);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        // fmt chunk
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16); // PCM
        bw.Write((short)1); // AudioFormat PCM
        bw.Write((short)channels);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write(blockAlign);
        bw.Write((short)bitsPerSample);
        // data chunk
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(subchunk2Size);
        foreach (var sample in data) bw.Write(sample);

        return path;
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

            if (_readerThread is not null)
            {
                if (!_readerThread.Join(TimeSpan.FromSeconds(1)))
                {
                    try { _readerThread.Join(TimeSpan.FromSeconds(2)); } catch { }
                }
            }

            try { _readerStream?.Dispose(); } catch { }
            try { _pipeRead?.Dispose(); } catch { }

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

    private readonly IAudioService _audio;
    private UnloadSentinel? _sentinel = new("DungeonPlugin.CompositionRoot");

    public CompositionRoot()
    {
        ViewModel = new DungeonViewModel();
        LogService = new LogBuffer();
        _audio = CreateAudioService();
        Loop = new AppLoop(ViewModel, _audio, LogService);
    }

    public void Dispose()
    {
        _audio.Dispose();
        _sentinel = null;
    }

    private IAudioService CreateAudioService()
    {
        var disableEnv = Environment.GetEnvironmentVariable("CONSOLEGAME_DISABLE_AUDIO");
        if (!string.IsNullOrWhiteSpace(disableEnv) &&
            (string.Equals(disableEnv, "1", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(disableEnv, "true", StringComparison.OrdinalIgnoreCase)))
        {
            return new NoAudioService(LogService, "CONSOLEGAME_DISABLE_AUDIO=1");
        }

        return new VlcAudioService(LogService);
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

    public Task ConfigureAsync(CancellationToken cancellationToken = default)
    {
        _root ??= new CompositionRoot();
        if (Context?.PluginDirectory is { Length: > 0 } pluginDir)
        {
            _pluginDirectory = pluginDir;
            var logService = _root.LogService;
            logService.SetBaseDirectory(pluginDir);
            logService.Info($"Log directory set to {pluginDir}");
        }
        return Task.CompletedTask;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _root ??= new CompositionRoot();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, Context?.ShutdownToken ?? default);
    var logService = _root.LogService;
        NativeStderrCapture? stderrCapture = null;

        try
        {
            if (OperatingSystem.IsWindows())
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

            await _root.Loop.RunAsync(linkedCts.Token);
        }
        finally
        {
            stderrCapture?.Dispose();
            if (stderrCapture is not null)
            {
                logService?.Info($"Native stderr log file: {stderrCapture.LogPath}");
            }
        }
    }

    public void Dispose()
    {
        _root?.Dispose();
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

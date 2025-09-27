using System.IO;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using ConsoleGame.Contracts;
using ConsoleGame.Dungeon.Core;
using ConsoleGame.Dungeon.Logging;
using Terminal.Gui;

namespace ConsoleGame.Dungeon.UI.Terminal;

internal sealed class MessageChannel<T>
{
    private readonly List<Action<T>> _handlers = new();
    private readonly object _gate = new();

    public IDisposable Subscribe(Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

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

    public MoveInputHandler(DungeonViewModel vm, MessageChannel<PlayerMoved> movedChannel)
    {
        _vm = vm;
        _movedChannel = movedChannel;
    }

    public void Handle(MoveInput input)
    {
        _vm.MoveBy(input.Dx, input.Dy);
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
        _render = render;
        _audio = audio;
        _stepSound = stepSoundPath;
    }

    public void Handle(PlayerMoved message)
    {
        _render();
        if (_audio.IsEnabled && !string.IsNullOrWhiteSpace(_stepSound))
        {
            try
            {
                _audio.Play(_stepSound);
            }
            catch
            {
                // best-effort only
            }
        }
    }
}

internal static class StepSoundProvider
{
    public static string? EnsureStepSoundAvailable(ILogService log)
    {
        try
        {
            var asmDir = Path.GetDirectoryName(typeof(AppLoop).Assembly.Location);
            if (string.IsNullOrWhiteSpace(asmDir))
            {
                log.Warn("Unable to determine assembly directory; step sound disabled.");
                return null;
            }

            var candidates = new[]
            {
                "step.wav",
                "step.ogg",
                Path.Combine("assets", "step.wav"),
                Path.Combine("assets", "step.ogg")
            };

            foreach (var candidate in candidates)
            {
                var fullPath = Path.Combine(asmDir, candidate);
                if (File.Exists(fullPath))
                {
                    log.Info($"Using step sound asset '{fullPath}'.");
                    return fullPath;
                }
            }

            var generated = AudioFileGenerator.GenerateStepWav(asmDir);
            if (!string.IsNullOrWhiteSpace(generated))
            {
                log.Info($"Generated step sound asset '{generated}'.");
                return generated;
            }
        }
        catch (Exception ex)
        {
            log.Warn($"Failed to prepare step sound asset: {ex.Message}");
        }

        log.Warn("No step sound asset found; movement will be silent.");
        return null;
    }
}

public sealed class AppLoop : IAppLoop
{
    private readonly DungeonViewModel _vm;
    private readonly IAudioService _audio;
    private readonly ILogService _log;
    private readonly MessageChannel<MoveInput> _moveChannel = new();
    private readonly MessageChannel<PlayerMoved> _movedChannel = new();
    private readonly string? _stepSoundPath;

    public AppLoop(DungeonViewModel vm, IAudioService audio, ILogService log)
    {
        _vm = vm;
        _audio = audio;
        _log = log;
        _stepSoundPath = StepSoundProvider.EnsureStepSoundAvailable(log);
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
        var top = new Toplevel { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        var window = new Window { Title = "Dungeon", X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill() };
        top.Add(window);

        var rows = new List<Label>(_vm.Height);
        for (var y = 0; y < _vm.Height; y++)
        {
            var lbl = new Label { Text = string.Empty, X = 0, Y = y, Width = _vm.Width, Height = 1 };
            rows.Add(lbl);
            window.Add(lbl);
        }

        void Render()
        {
            for (var y = 0; y < _vm.Height; y++)
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

        var moveHandler = new MoveInputHandler(_vm, _movedChannel);
        var movedHandler = new PlayerMovedHandler(Render, _audio, _stepSoundPath);
        var disposables = new CompositeDisposable
        {
            _moveChannel.Subscribe(moveHandler.Handle),
            _movedChannel.Subscribe(movedHandler.Handle)
        };

        EventHandler<Key> keyHandler = (_, key) =>
        {
            if (key == Key.CursorLeft) _moveChannel.Publish(new MoveInput(-1, 0));
            else if (key == Key.CursorRight) _moveChannel.Publish(new MoveInput(1, 0));
            else if (key == Key.CursorUp) _moveChannel.Publish(new MoveInput(0, -1));
            else if (key == Key.CursorDown) _moveChannel.Publish(new MoveInput(0, 1));
            else if (key == Key.Esc) Application.RequestStop();
        };

        top.KeyDown += keyHandler;
        Render();

        using var reg = ct.Register(() => Application.RequestStop());
        try
        {
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
    public static string? GenerateStepWav(string pluginDir)
    {
        try
        {
            var assetsDir = Path.Combine(pluginDir, "assets");
            Directory.CreateDirectory(assetsDir);
            var path = Path.Combine(assetsDir, "step.wav");
            if (File.Exists(path)) return path;

            const int sampleRate = 44100;
            const double durationSec = 0.09;
            const double freq = 440.0;
            var samples = (int)(sampleRate * durationSec);
            var data = new short[samples];

            for (var n = 0; n < samples; n++)
            {
                var t = n / (double)sampleRate;
                const double attack = 0.01;
                const double decayStart = 0.07;
                double amp;
                if (t < attack) amp = t / attack;
                else if (t < decayStart) amp = 1.0;
                else amp = Math.Max(0, 1.0 - (t - decayStart) / (durationSec - decayStart));
                var s = Math.Sin(2 * Math.PI * freq * t) * amp;
                data[n] = (short)(s * short.MaxValue * 0.5);
            }

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs);

            const short channels = 1;
            const short bitsPerSample = 16;
            var byteRate = sampleRate * channels * bitsPerSample / 8;
            var blockAlign = (short)(channels * bitsPerSample / 8);
            var subchunk2Size = samples * channels * bitsPerSample / 8;

            bw.Write(Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + subchunk2Size);
            bw.Write(Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);
            bw.Write(channels);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write(blockAlign);
            bw.Write(bitsPerSample);
            bw.Write(Encoding.ASCII.GetBytes("data"));
            bw.Write(subchunk2Size);

            foreach (var sample in data)
            {
                bw.Write(sample);
            }

            return path;
        }
        catch
        {
            return null;
        }
    }
}

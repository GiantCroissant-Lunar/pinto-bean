using ConsoleGame.Contracts;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Pure.DI;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Terminal.Gui;

namespace ConsoleGame.Dungeon.Plugin;

public readonly record struct MoveInput(int Dx, int Dy);
public readonly record struct PlayerMoved((int X, int Y) Pos);

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

internal sealed class MoveInputHandler : IMessageHandler<MoveInput>
{
    private readonly DungeonViewModel _vm;
    private readonly IPublisher<PlayerMoved> _movedPub;
    public MoveInputHandler(DungeonViewModel vm, IPublisher<PlayerMoved> movedPub) { _vm = vm; _movedPub = movedPub; }
    public void Handle(MoveInput message)
    {
        _vm.MoveBy(message.Dx, message.Dy);
        _movedPub.Publish(new PlayerMoved(_vm.Player));
    }
}

internal sealed class PlayerMovedHandler : IMessageHandler<PlayerMoved>
{
    private readonly Action _render;
    public PlayerMovedHandler(Action render) { _render = render; }
    public void Handle(PlayerMoved message) => _render();
}

internal sealed class AppLoop : IAppLoop
{
    private readonly DungeonViewModel _vm;
    private IPublisher<MoveInput>? _movePub;
    private ISubscriber<MoveInput>? _moveSub;
    private ISubscriber<PlayerMoved>? _movedSub;
    private IPublisher<PlayerMoved>? _movedPub;
    private readonly CompositeDisposable _disp = new();

    public AppLoop(DungeonViewModel vm) { _vm = vm; }

    public void ConfigureMessaging(IPublisher<MoveInput> movePub, ISubscriber<MoveInput> moveSub, ISubscriber<PlayerMoved> movedSub, IPublisher<PlayerMoved> movedPub)
    { _movePub = movePub; _moveSub = moveSub; _movedSub = movedSub; _movedPub = movedPub; }

    public Task RunAsync(CancellationToken ct)
    {
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
        if (_moveSub is null || _movedSub is null || _movePub is null || _movedPub is null)
            throw new InvalidOperationException("Messaging not configured");
        var miHandler = new MoveInputHandler(_vm, _movedPub);
        var pmHandler = new PlayerMovedHandler(Render);
    var disposables = new CompositeDisposable();
    var sub1 = _moveSub.Subscribe(miHandler);
    var sub2 = _movedSub.Subscribe(pmHandler);
    disposables.Add(sub1);
    disposables.Add(sub2);

        // Key handling publishes MoveInput
        top.KeyDown += (object? _, Key key) =>
        {
            if (key == Key.CursorLeft) { _movePub.Publish(new MoveInput(-1, 0)); }
            else if (key == Key.CursorRight) { _movePub.Publish(new MoveInput(1, 0)); }
            else if (key == Key.CursorUp) { _movePub.Publish(new MoveInput(0, -1)); }
            else if (key == Key.CursorDown) { _movePub.Publish(new MoveInput(0, 1)); }
            else if (key == Key.Esc)
            {
                // Allow explicit quit via Esc or Q / Ctrl+Q
                Application.RequestStop();
            }
        };

        // Initial render
        Render();

        using var reg = ct.Register(() => Application.RequestStop());
        try
        {
            // Run on the same thread as Init and view setup to avoid cross-thread issues.
            Application.Run(top);
        }
        finally
        {
            Application.RequestStop();
            Application.Shutdown();
            disposables.Dispose();
            _disp.Dispose();
        }

        return Task.CompletedTask;
    }
}

internal partial class Composition
{
    private static void Setup() => DI.Setup()
        .Bind<DungeonViewModel>().To<DungeonViewModel>()
        .Bind<IAppLoop>().To<AppLoop>()
        .Root<IAppLoop>("CreateLoop");
}

internal sealed class CompositionRoot
{
    public ServiceProvider ServiceProvider { get; }
    public DungeonViewModel ViewModel { get; } = new();
    public IAppLoop Loop { get; }

    public CompositionRoot()
    {
        var services = new ServiceCollection();
        services.AddMessagePipe();
        var provider = services.BuildServiceProvider();
        ServiceProvider = provider as ServiceProvider ?? throw new InvalidOperationException("No ServiceProvider");
    var movePub = ServiceProvider.GetRequiredService<IPublisher<MoveInput>>();
    var moveSub = ServiceProvider.GetRequiredService<ISubscriber<MoveInput>>();
    var movedSub = ServiceProvider.GetRequiredService<ISubscriber<PlayerMoved>>();
    var movedPub = ServiceProvider.GetRequiredService<IPublisher<PlayerMoved>>();
    // Create AppLoop and wire messaging
    var loop = new Composition().CreateLoop;
    (loop as AppLoop)!.ConfigureMessaging(movePub, moveSub, movedSub, movedPub);
    Loop = loop;
    }
}

[Plugin("consolegame.dungeon", "Dungeon", "0.1.0", Description = "Dungeon TUI with Reactive state and MessagePipe")]
public sealed class DungeonPlugin : IPlugin, IRuntimePlugin
{
    public string Name => "Dungeon";
    public IPluginContext? Context { get; set; }
    public string Describe() => "A tiny dungeon demo";

    private CompositionRoot? _root;

    public Task ConfigureAsync(CancellationToken cancellationToken = default)
    {
        _root ??= new CompositionRoot();
        return Task.CompletedTask;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _root ??= new CompositionRoot();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, Context?.ShutdownToken ?? default);
        await _root.Loop.RunAsync(linkedCts.Token);
    }
}

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;
using Pure.DI;

namespace ConsoleGame.Plugin.Battle;

internal partial class BattlePureDiRoot
{
    [Conditional("DI")]
    private static void Setup() => DI.Setup()
        .Hint(Hint.Resolve, "Off")
        .Bind<ILogger<BattlePlugin>>().To(_ => CreateLoggerInternal())
        .Root<ILogger<BattlePlugin>>(nameof(CreateLogger));

    private static ILogger<BattlePlugin> CreateLoggerInternal()
    {
        var factory = BattlePureDiRootScope.Current;
        return factory.CreateLogger<BattlePlugin>();
    }
}

internal static class BattlePureDiAdapter
{
    public static ILogger<BattlePlugin> CreateLogger(ILoggerFactory loggerFactory)
    {
        using var scope = BattlePureDiRootScope.Enter(loggerFactory);
        return new BattlePureDiRoot().CreateLogger;
    }
}

internal static class BattlePureDiRootScope
{
    private sealed class Scope(ILoggerFactory? previous) : IDisposable
    {
        private readonly ILoggerFactory? _previous = previous;
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            State.Value = _previous;
        }
    }

    private static readonly AsyncLocal<ILoggerFactory?> State = new();

    public static IDisposable Enter(ILoggerFactory loggerFactory)
    {
        var scope = new Scope(State.Value);
        State.Value = loggerFactory;
        return scope;
    }

    public static ILoggerFactory Current => State.Value ?? throw new InvalidOperationException("BattlePureDiRootScope was not configured for the current context.");
}

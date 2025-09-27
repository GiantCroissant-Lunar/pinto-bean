using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ConsoleGame.App;
using ConsoleGame.TestPlugins.Leaky;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ConsoleGame.Tests;

public sealed class PluginUnloadTests
{
    [Fact]
    public async Task SamplePlugin_UnloadsCleanly()
    {
        var pluginAssemblyPath = Path.Combine(AppContext.BaseDirectory, "ConsoleGame.TestPlugins.Sample.dll");
        Assert.True(File.Exists(pluginAssemblyPath), $"Plugin assembly missing at '{pluginAssemblyPath}'");

        using var services = new ServiceCollection().BuildServiceProvider();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        var originalHeadless = Environment.GetEnvironmentVariable("CONSOLEGAME_TUI_HEADLESS");
        try
        {
            Environment.SetEnvironmentVariable("CONSOLEGAME_TUI_HEADLESS", "1");

            var result = await SelfLoader.RunPluginAsync(pluginAssemblyPath, services, cts.Token);

            Assert.True(result.Unloaded, $"Expected collectible AssemblyLoadContext to unload. Duration={result.Duration.TotalMilliseconds}ms");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CONSOLEGAME_TUI_HEADLESS", originalHeadless);
        }
    }

    [Fact]
    public async Task LeakyPlugin_FailsToUnloadWithoutCleanup()
    {
        var pluginAssemblyPath = Path.Combine(AppContext.BaseDirectory, "ConsoleGame.TestPlugins.Leaky.dll");
        Assert.True(File.Exists(pluginAssemblyPath), $"Leaky plugin assembly missing at '{pluginAssemblyPath}'");

        using var services = new ServiceCollection().BuildServiceProvider();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        try
        {
            var result = await SelfLoader.RunPluginAsync(pluginAssemblyPath, services, cts.Token);

            Assert.False(result.Unloaded, "Expected leaky plugin to keep collectible AssemblyLoadContext alive");
        }
        finally
        {
            LeakyUnloadPlugin.ResetLeak();
        }
    }
}

using System.Globalization;
using ConsoleGame.App;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConsoleGame.App.Hosting;

internal sealed class PluginRuntimeHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly ILogger<PluginRuntimeHostedService> _logger;
    private readonly IOptions<PluginHostOptions> _options;

    public PluginRuntimeHostedService(
        IServiceProvider serviceProvider,
        IHostApplicationLifetime applicationLifetime,
        ILogger<PluginRuntimeHostedService> logger,
        IOptions<PluginHostOptions> options)
    {
        _serviceProvider = serviceProvider;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pluginPath = ResolvePluginPath();
        if (pluginPath is null)
        {
            _logger.LogWarning("No plugin assembly located. The host will remain idle.");
            return;
        }

        var autoCancelMilliseconds = ResolveAutoCancel();
        if (autoCancelMilliseconds is { } autoValue)
        {
            _logger.LogInformation("Auto-cancel configured after {Milliseconds} ms", autoValue);
        }

        if (_options.Value.DebugUnload)
        {
            Environment.SetEnvironmentVariable("CONSOLEGAME_DEBUG_UNLOAD", "1");
        }

        using var autoCancelCts = autoCancelMilliseconds is { } ms
            ? new CancellationTokenSource(TimeSpan.FromMilliseconds(ms))
            : null;

        using var linkedCts = autoCancelCts is not null
            ? CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, autoCancelCts.Token)
            : CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        try
        {
            _logger.LogInformation("Launching plugin from {PluginPath}", pluginPath);
            var result = await SelfLoader.RunPluginAsync(pluginPath, _serviceProvider, linkedCts.Token).ConfigureAwait(false);
            _logger.LogInformation("Plugin unloaded: {Unloaded} after {Duration} ms", result.Unloaded, result.Duration.TotalMilliseconds);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Plugin execution cancelled due to host shutdown request");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plugin runtime failure for {PluginPath}", pluginPath);
        }
        finally
        {
            if (!stoppingToken.IsCancellationRequested)
            {
                _applicationLifetime.StopApplication();
            }
        }
    }

    private string? ResolvePluginPath()
    {
        var envPlugin = Environment.GetEnvironmentVariable("CONSOLEGAME_PLUGIN_PATH");
        if (TryValidatePluginPath(envPlugin, out var resolved))
        {
            _logger.LogInformation("Using plugin from CONSOLEGAME_PLUGIN_PATH: {Plugin}", resolved);
            return resolved;
        }

        if (TryValidatePluginPath(_options.Value.PluginPath, out resolved))
        {
            _logger.LogInformation("Using plugin path from configuration: {Plugin}", resolved);
            return resolved;
        }

        foreach (var probe in _options.Value.ProbePaths)
        {
            if (TryValidatePluginPath(probe, out resolved))
            {
                _logger.LogInformation("Using plugin path from probes: {Plugin}", resolved);
                return resolved;
            }
        }

        var baseDir = AppContext.BaseDirectory;
        var configDir = new DirectoryInfo(baseDir).Parent?.Name ?? "Debug";
        var tfmDir = new DirectoryInfo(baseDir).Name;
        var projectRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));

        var dungeonPath = Path.Combine(projectRoot, "ConsoleGame.Dungeon.Plugin", "bin", configDir, tfmDir, "ConsoleGame.Dungeon.Plugin.dll");
        if (TryValidatePluginPath(dungeonPath, out resolved))
        {
            _logger.LogInformation("Using default dungeon plugin at {Plugin}", resolved);
            return resolved;
        }

        var terminalLibPath = Path.Combine(projectRoot, "ConsoleGame.TerminalLib", "bin", configDir, tfmDir, "ConsoleGame.TerminalLib.dll");
        if (TryValidatePluginPath(terminalLibPath, out resolved))
        {
            _logger.LogInformation("Using TerminalLib sample plugin at {Plugin}", resolved);
            return resolved;
        }

        return null;
    }

    private static bool TryValidatePluginPath(string? candidate, out string resolved)
    {
        resolved = string.Empty;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        var fullPath = Path.GetFullPath(candidate);
        if (!File.Exists(fullPath))
        {
            return false;
        }

        resolved = fullPath;
        return true;
    }

    private int? ResolveAutoCancel()
    {
        if (_options.Value.AutoCancelMilliseconds is { } configured && configured > 0)
        {
            return configured;
        }

        var envValue = Environment.GetEnvironmentVariable("CONSOLEGAME_AUTOCANCEL_MS");
        if (!string.IsNullOrWhiteSpace(envValue) &&
            int.TryParse(envValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) &&
            parsed > 0)
        {
            return parsed;
        }

        return null;
    }
}

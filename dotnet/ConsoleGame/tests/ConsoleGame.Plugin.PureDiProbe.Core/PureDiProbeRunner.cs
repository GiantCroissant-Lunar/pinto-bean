using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleGame.Plugin.PureDiProbe.Core;

public static class PureDiProbeRunner
{
    public static async Task<int> RunAsync(
        string[] args,
        Func<ProbeContext, ValueTask> configureAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(configureAsync);

        var builder = Host.CreateApplicationBuilder(args);
        ConfigureDefaults(builder);

        var context = new ProbeContext(
            builder.Services,
            builder.Configuration,
            builder.Logging,
            builder.Environment,
            cancellationToken);

        try
        {
            await configureAsync(context).ConfigureAwait(false);

            using var serviceProvider = builder.Services.BuildServiceProvider(validateScopes: true);
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("PureDiProbe");

            logger?.LogInformation("Service provider built successfully. Validating hosted services (if any)…");

            await StartHostedServicesAsync(serviceProvider, logger, cancellationToken).ConfigureAwait(false);
            await StopHostedServicesAsync(serviceProvider, logger, cancellationToken).ConfigureAwait(false);

            logger?.LogInformation("Pure DI probe completed successfully.");
            return 0;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            await WriteFailureAsync(ex).ConfigureAwait(false);
            return 1;
        }
    }

    private static void ConfigureDefaults(HostApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
        builder.Configuration.AddEnvironmentVariables("CONSOLEGAME_");

        builder.Logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        });
    }

    private static async Task StartHostedServicesAsync(IServiceProvider serviceProvider, ILogger? logger, CancellationToken cancellationToken)
    {
        var hostedServices = serviceProvider.GetServices<IHostedService>().ToArray();
        if (hostedServices.Length == 0)
        {
            return;
        }

        logger?.LogDebug("Starting {Count} hosted service(s) for probe validation.", hostedServices.Length);
        foreach (var hostedService in hostedServices)
        {
            await hostedService.StartAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task StopHostedServicesAsync(IServiceProvider serviceProvider, ILogger? logger, CancellationToken cancellationToken)
    {
        var hostedServices = serviceProvider.GetServices<IHostedService>().ToArray();
        if (hostedServices.Length == 0)
        {
            return;
        }

        logger?.LogDebug("Stopping {Count} hosted service(s) for probe validation.", hostedServices.Length);
        foreach (var hostedService in hostedServices.Reverse())
        {
            await hostedService.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static Task WriteFailureAsync([NotNull] Exception exception)
    {
        Console.Error.WriteLine("Pure DI probe failed:");
        Console.Error.WriteLine(exception);
        return Task.CompletedTask;
    }
}

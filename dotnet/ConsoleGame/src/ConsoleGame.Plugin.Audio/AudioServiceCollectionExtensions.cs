using ConsoleGame.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

namespace ConsoleGame.Plugin.Audio;

public static class AudioServiceCollectionExtensions
{
    public const string ConfigurationSectionName = "Audio";

    public static IServiceCollection AddAudioSubsystem(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<AudioOptions>()
            .Bind(configuration.GetSection(ConfigurationSectionName))
            .PostConfigure(options =>
            {
                var disableAudioEnv = Environment.GetEnvironmentVariable("CONSOLEGAME_DISABLE_AUDIO");
                if (!string.IsNullOrWhiteSpace(disableAudioEnv) && disableAudioEnv.Equals("1", StringComparison.OrdinalIgnoreCase))
                {
                    options.Enabled = false;
                    options.DisableReason = "Disabled via CONSOLEGAME_DISABLE_AUDIO=1";
                }
                else if (!string.IsNullOrWhiteSpace(disableAudioEnv) && disableAudioEnv.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    options.Enabled = false;
                    options.DisableReason = "Disabled via CONSOLEGAME_DISABLE_AUDIO=true";
                }
            })
            .ValidateDataAnnotations();

        services.AddSingleton(provider => AudioComposition.Resolve<IAudioService>(provider));
        services.AddHostedService<AudioCompositionCleanupHostedService>();

        return services;
    }
}

internal sealed class AudioCompositionCleanupHostedService : IHostedService
{
    private readonly ILogger<AudioCompositionCleanupHostedService> _logger;

    public AudioCompositionCleanupHostedService(ILogger<AudioCompositionCleanupHostedService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            AudioComposition.Reset();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Audio composition reset encountered an error");
        }

        return Task.CompletedTask;
    }
}

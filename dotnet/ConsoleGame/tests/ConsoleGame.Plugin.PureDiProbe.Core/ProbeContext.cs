using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleGame.Plugin.PureDiProbe.Core;

public sealed class ProbeContext
{
    internal ProbeContext(
        IServiceCollection services,
        IConfiguration configuration,
        ILoggingBuilder logging,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        Services = services;
        Configuration = configuration;
        Logging = logging;
        Environment = environment;
        CancellationToken = cancellationToken;
    }

    public IServiceCollection Services { get; }

    public IConfiguration Configuration { get; }

    public ILoggingBuilder Logging { get; }

    public IHostEnvironment Environment { get; }

    public CancellationToken CancellationToken { get; }
}

// Tier-3: Dependency injection extensions for service registry

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Extension methods for registering the service registry with dependency injection containers.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the PintoBean service registry to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddServiceRegistry(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // Register as singleton to maintain provider registrations across the application lifetime
        services.TryAddSingleton<IServiceRegistry, ServiceRegistry>();

        return services;
    }

    /// <summary>
    /// Adds the PintoBean service registry to the service collection with configuration.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">A delegate to configure the service registry.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.</exception>
    public static IServiceCollection AddServiceRegistry(this IServiceCollection services, Action<IServiceRegistry> configure)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        // Register the service registry with configuration
        services.TryAddSingleton<IServiceRegistry>(serviceProvider =>
        {
            var registry = new ServiceRegistry();
            configure(registry);
            return registry;
        });

        return services;
    }

    /// <summary>
    /// Adds selection strategies to the service collection with category defaults per RFC-0003.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional delegate to configure selection strategy options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddSelectionStrategies(this IServiceCollection services, Action<SelectionStrategyOptions>? configure = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // Ensure service registry is registered
        services.AddServiceRegistry();

        // Check if options are already registered
        var existingDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(SelectionStrategyOptions));
        if (existingDescriptor != null)
        {
            // Options already registered, just apply configuration if provided
            if (configure != null)
            {
                services.Remove(existingDescriptor);

                // If it was a singleton with an instance, get the instance and configure it
                if (existingDescriptor.ImplementationInstance is SelectionStrategyOptions existingOptions)
                {
                    configure(existingOptions);
                    services.AddSingleton(existingOptions);
                }
                else
                {
                    // It was registered with a factory, create new options and apply both configurations
                    var options = new SelectionStrategyOptions();
                    configure(options);
                    services.AddSingleton(options);
                }
            }
        }
        else
        {
            // Configure and register selection strategy options for the first time
            var options = new SelectionStrategyOptions();
            configure?.Invoke(options);
            services.TryAddSingleton(options);
        }

        // Register default strategy factory
        services.TryAddSingleton<ISelectionStrategyFactory, DefaultSelectionStrategyFactory>();

        return services;
    }

    /// <summary>
    /// Adds selection strategies to the service collection using the IOptions pattern.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddSelectionStrategies(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // Ensure service registry is registered
        services.AddServiceRegistry();

        // Check if SelectionStrategyOptions is already registered
        var existingDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(SelectionStrategyOptions));
        if (existingDescriptor != null)
        {
            // If already registered, ensure IOptions pattern is also available
            services.TryAddSingleton<IOptions<SelectionStrategyOptions>>(serviceProvider =>
            {
                var existingOptions = serviceProvider.GetRequiredService<SelectionStrategyOptions>();
                return Options.Create(existingOptions);
            });
        }
        else
        {
            // Configure using IOptions pattern
            services.AddOptions<SelectionStrategyOptions>();

            // Register the options as a singleton for backwards compatibility
            services.TryAddSingleton<SelectionStrategyOptions>(serviceProvider =>
                serviceProvider.GetRequiredService<IOptions<SelectionStrategyOptions>>().Value);
        }

        // Register default strategy factory
        services.TryAddSingleton<ISelectionStrategyFactory, DefaultSelectionStrategyFactory>();

        return services;
    }

    /// <summary>
    /// Adds selection strategies to the service collection using the IOptions pattern with configuration binding.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The configuration section to bind to SelectionStrategyOptions.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configuration"/> is null.</exception>
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Configuration binding is expected to work with SelectionStrategyOptions properties.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code.", Justification = "Configuration binding is expected to work with SelectionStrategyOptions properties.")]
    public static IServiceCollection AddSelectionStrategies(this IServiceCollection services, IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        // Ensure service registry is registered
        services.AddServiceRegistry();

        // Configure using IOptions pattern with configuration binding
        services.AddOptions<SelectionStrategyOptions>()
            .Bind(configuration);

        // Register the options as a singleton for backwards compatibility
        services.TryAddSingleton<SelectionStrategyOptions>(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<SelectionStrategyOptions>>().Value);

        // Register default strategy factory
        services.TryAddSingleton<ISelectionStrategyFactory, DefaultSelectionStrategyFactory>();

        return services;
    }

    /// <summary>
    /// Adds selection strategies to the service collection using the IOptions pattern with configuration section name.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="sectionName">The name of the configuration section to bind.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/>, <paramref name="configuration"/>, or <paramref name="sectionName"/> is null.</exception>
    public static IServiceCollection AddSelectionStrategies(this IServiceCollection services, IConfiguration configuration, string sectionName)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        if (sectionName == null) throw new ArgumentNullException(nameof(sectionName));

        return services.AddSelectionStrategies(configuration.GetSection(sectionName));
    }

    /// <summary>
    /// Configures the selection strategies to use PickOne for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection UsePickOneFor<TService>(this IServiceCollection services)
        where TService : class
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        return services.AddSelectionStrategies(options =>
            options.UseStrategyFor<TService>(SelectionStrategyType.PickOne));
    }

    /// <summary>
    /// Configures the selection strategies to use FanOut for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection UseFanOutFor<TService>(this IServiceCollection services)
        where TService : class
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        return services.AddSelectionStrategies(options =>
            options.UseStrategyFor<TService>(SelectionStrategyType.FanOut));
    }

    /// <summary>
    /// Configures the selection strategies to use Sharded for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection UseShardedFor<TService>(this IServiceCollection services)
        where TService : class
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        return services.AddSelectionStrategies(options =>
            options.UseStrategyFor<TService>(SelectionStrategyType.Sharded));
    }

    /// <summary>
    /// Configures the selection strategies to use PickOne as the default for the specified service category.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="category">The service category to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection UsePickOneForCategory(this IServiceCollection services, ServiceCategory category)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        return services.AddSelectionStrategies(options =>
            options.SetCategoryDefault(category, SelectionStrategyType.PickOne));
    }

    /// <summary>
    /// Configures the selection strategies to use FanOut as the default for the specified service category.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="category">The service category to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection UseFanOutForCategory(this IServiceCollection services, ServiceCategory category)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        return services.AddSelectionStrategies(options =>
            options.SetCategoryDefault(category, SelectionStrategyType.FanOut));
    }

    /// <summary>
    /// Configures the selection strategies to use Sharded as the default for the specified service category.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="category">The service category to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection UseShardedForCategory(this IServiceCollection services, ServiceCategory category)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        return services.AddSelectionStrategies(options =>
            options.SetCategoryDefault(category, SelectionStrategyType.Sharded));
    }

    /// <summary>
    /// Adds the resilience executor to the service collection with the default pass-through implementation.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddResilienceExecutor(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // Register the default pass-through implementation
        services.TryAddSingleton<IResilienceExecutor>(NoOpResilienceExecutor.Instance);

        return services;
    }

    /// <summary>
    /// Adds the resilience executor to the service collection with a custom implementation.
    /// </summary>
    /// <typeparam name="TImplementation">The resilience executor implementation type.</typeparam>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    [UnconditionalSuppressMessage("Trimming", "IL2091:Target generic argument does not satisfy 'DynamicallyAccessedMemberTypes' requirements", Justification = "TImplementation is expected to have a public constructor.")]
    public static IServiceCollection AddResilienceExecutor<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IServiceCollection services)
        where TImplementation : class, IResilienceExecutor
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.TryAddSingleton<IResilienceExecutor, TImplementation>();

        return services;
    }

    /// <summary>
    /// Adds the resilience executor to the service collection with a factory.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="factory">The factory to create the resilience executor.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="factory"/> is null.</exception>
    public static IServiceCollection AddResilienceExecutor(this IServiceCollection services, Func<IServiceProvider, IResilienceExecutor> factory)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        services.TryAddSingleton(factory);

        return services;
    }

    /// <summary>
    /// Adds the Polly-based resilience executor to the service collection with default configuration.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddPollyResilience(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // Register options with defaults
        services.AddOptions<PollyResilienceExecutorOptions>();

        // Register the Polly-based resilience executor
        services.TryAddSingleton<IResilienceExecutor, PollyResilienceExecutor>();

        return services;
    }

    /// <summary>
    /// Adds the Polly-based resilience executor to the service collection with custom configuration.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">A delegate to configure the Polly resilience executor options.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.</exception>
    public static IServiceCollection AddPollyResilience(this IServiceCollection services, Action<PollyResilienceExecutorOptions> configure)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        // Register and configure options
        services.AddOptions<PollyResilienceExecutorOptions>()
            .Configure(configure);

        // Register the Polly-based resilience executor
        services.TryAddSingleton<IResilienceExecutor, PollyResilienceExecutor>();

        return services;
    }

    /// <summary>
    /// Adds the Polly-based resilience executor to the service collection with configuration binding.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The configuration section to bind to PollyResilienceExecutorOptions.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configuration"/> is null.</exception>
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Configuration binding is expected to work with PollyResilienceExecutorOptions properties.")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code.", Justification = "Configuration binding is expected to work with PollyResilienceExecutorOptions properties.")]
    public static IServiceCollection AddPollyResilience(this IServiceCollection services, IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        // Register and bind options to configuration
        services.AddOptions<PollyResilienceExecutorOptions>()
            .Bind(configuration);

        // Register the Polly-based resilience executor
        services.TryAddSingleton<IResilienceExecutor, PollyResilienceExecutor>();

        return services;
    }

    /// <summary>
    /// Adds the OpenTelemetry-backed aspect runtime to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="sourceName">The name of the ActivitySource for tracing.</param>
    /// <param name="meterName">The name of the Meter for metrics.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services, sourceName, or meterName is null.</exception>
    public static IServiceCollection AddOpenTelemetryAspectRuntime(this IServiceCollection services, string sourceName, string meterName)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (sourceName == null) throw new ArgumentNullException(nameof(sourceName));
        if (meterName == null) throw new ArgumentNullException(nameof(meterName));

        services.TryAddSingleton<IAspectRuntime>(serviceProvider => 
            new OtelAspectRuntime(sourceName, meterName));

        return services;
    }

    /// <summary>
    /// Adds the no-operation aspect runtime to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
    public static IServiceCollection AddNoOpAspectRuntime(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.TryAddSingleton<IAspectRuntime>(NoOpAspectRuntime.Instance);

        return services;
    }
}

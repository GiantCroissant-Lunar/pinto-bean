// Tier-2: Analytics service façade using RealizeService code generation

using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.CodeGen;
using Yokan.PintoBean.Runtime;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace PintoBean.Analytics.Demo.Console;

/// <summary>
/// Tier-2 Analytics service façade that realizes the IAnalytics contract.
/// The source generator will create the implementation that delegates to registered providers
/// via the service registry with proper strategy routing (FanOut/Sharded).
/// </summary>
[RealizeService(typeof(IAnalytics))]
public partial class Analytics : IAnalytics
{
    private readonly IServiceRegistry _registry;
    private readonly IResilienceExecutor _resilienceExecutor;
    private readonly IAspectRuntime _aspectRuntime;
    private readonly ISelectionStrategyFactory _strategyFactory;

    public Analytics(IServiceRegistry registry, IResilienceExecutor resilienceExecutor, IAspectRuntime aspectRuntime, ISelectionStrategyFactory strategyFactory)
    {
        _registry = registry;
        _resilienceExecutor = resilienceExecutor;
        _aspectRuntime = aspectRuntime;
        _strategyFactory = strategyFactory;
    }

    public async Task Track(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default)
    {
        // Get the strategy and use it to select providers
        var strategy = _strategyFactory.CreateStrategy<IAnalytics>();
        var registrations = _registry.GetRegistrations<IAnalytics>().ToList();
        
        // Add metadata for sharded strategy (event name for shard key extraction)
        var metadata = new Dictionary<string, object>
        {
            ["EventName"] = analyticsEvent.EventName
        };
        var context = new SelectionContext<IAnalytics>(registrations, metadata);
        var result = strategy.SelectProviders(context);

        // Invoke all selected providers (this supports FanOut and Sharded properly)
        var tasks = result.SelectedProviders.Select(provider => 
            provider.Track(analyticsEvent, cancellationToken));
        
        await Task.WhenAll(tasks);
    }
}
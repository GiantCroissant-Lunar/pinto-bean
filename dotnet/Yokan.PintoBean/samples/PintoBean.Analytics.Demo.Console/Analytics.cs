// Tier-2: Analytics service façade using RealizeService code generation

using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.CodeGen;
using Yokan.PintoBean.Runtime;
using System.Threading;
using System.Threading.Tasks;

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

    public Analytics(IServiceRegistry registry, IResilienceExecutor resilienceExecutor, IAspectRuntime aspectRuntime)
    {
        _registry = registry;
        _resilienceExecutor = resilienceExecutor;
        _aspectRuntime = aspectRuntime;
    }

    public async Task Track(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default)
    {
        // Delegate to the service registry with proper strategy routing
        // The registry will automatically apply the selected strategy (FanOut/Sharded)
        await _registry.For<IAnalytics>().InvokeAsync(
            async (provider, ct) => await provider.Track(analyticsEvent, ct),
            cancellationToken);
    }
}
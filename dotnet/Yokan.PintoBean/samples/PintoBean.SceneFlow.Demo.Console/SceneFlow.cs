using System.Threading;
using System.Threading.Tasks;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.CodeGen;

namespace PintoBean.SceneFlow.Demo.ConsoleApp;

/// <summary>
/// Tier-2: SceneFlow façade that realizes the ISceneFlow contract through code generation.
/// This partial class will have its implementation generated automatically, 
/// delegating to registered providers via the service registry with PickOne strategy.
/// </summary>
[RealizeService(typeof(ISceneFlow))]
public partial class SceneFlow
{
    // The generator should create:
    // - Constructor taking IServiceRegistry, IResilienceExecutor, IAspectRuntime
    // - Implementation of LoadAsync that delegates to the registry
    // 
    // User implementation can go here for additional functionality
}
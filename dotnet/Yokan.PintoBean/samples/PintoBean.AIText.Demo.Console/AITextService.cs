using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.CodeGen;
using Yokan.PintoBean.Runtime;

namespace PintoBean.AIText.Demo.Console;

/// <summary>
/// Generated façade for AI text services with streaming support.
/// This demonstrates the complete façade → router → backend pattern with IAsyncEnumerable streaming.
/// </summary>
[RealizeService(typeof(IAIText))]
public partial class AITextService
{
    // The generator will create:
    // - Constructor taking IServiceRegistry, IResilienceExecutor, IAspectRuntime
    // - Implementation of all IAIText methods that delegate to the registry
    // - Streaming methods that properly handle IAsyncEnumerable<T> return types
    // 
    // User implementation can go here for additional functionality
}
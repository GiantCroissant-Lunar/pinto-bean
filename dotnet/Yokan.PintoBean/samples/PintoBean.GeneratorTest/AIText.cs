using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.CodeGen;
using Yokan.PintoBean.Runtime;

namespace PintoBean.GeneratorTest;

/// <summary>
/// Test implementation of AI text service using the RealizeService attribute.
/// This should trigger our source generator to create the façade partial.
/// </summary>
[RealizeService(typeof(IAIText))]
public partial class AIText
{
    // The generator should create:
    // - Constructor taking IServiceRegistry, IResilienceExecutor, IAspectRuntime
    // - Implementation of GenerateTextAsync and CompleteTextAsync that delegate to the registry
    // 
    // User implementation can go here for additional functionality
}
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.CodeGen;
using Yokan.PintoBean.Runtime;

namespace PintoBean.GeneratorTest;

/// <summary>
/// Test implementation of a service using the RealizeService attribute.
/// This should trigger our source generator to create the façade partial.
/// </summary>
[RealizeService(typeof(IHelloService))]
public partial class HelloService
{
    // The generator should create:
    // - Constructor taking IServiceRegistry, IResilienceExecutor, IAspectRuntime
    // - Implementation of SayHelloAsync and SayGoodbyeAsync that delegate to the registry
    // 
    // User implementation can go here for additional functionality
}
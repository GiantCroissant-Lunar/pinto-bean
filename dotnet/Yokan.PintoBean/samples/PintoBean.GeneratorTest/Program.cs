using PintoBean.GeneratorTest;

Console.WriteLine("PintoBean Generator Test");
Console.WriteLine("========================");

// This test verifies that our source generator creates the façade implementation.
// The HelloService class should implement IHelloService through generated code.

var service = typeof(HelloService);
Console.WriteLine($"HelloService type: {service}");

var interfaces = service.GetInterfaces();
Console.WriteLine($"Implemented interfaces: {string.Join(", ", interfaces.Select(i => i.Name))}");

var constructors = service.GetConstructors();
Console.WriteLine($"Constructors: {constructors.Length}");

if (constructors.Length > 0)
{
    var ctor = constructors[0];
    var parameters = ctor.GetParameters();
    Console.WriteLine($"Constructor parameters: {string.Join(", ", parameters.Select(p => p.ParameterType.Name))}");
}

var methods = service.GetMethods().Where(m => m.DeclaringType == service);
Console.WriteLine($"Declared methods: {string.Join(", ", methods.Select(m => m.Name))}");

Console.WriteLine();
Console.WriteLine("If the generator worked correctly, you should see:");
Console.WriteLine("- IHelloService in implemented interfaces");
Console.WriteLine("- Constructor with IServiceRegistry, IResilienceExecutor, IAspectRuntime parameters");
Console.WriteLine("- SayHelloAsync and SayGoodbyeAsync methods");
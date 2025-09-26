using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Yokan.PintoBean.Abstractions;
using Yokan.PintoBean.Runtime;

// Console demo application showing plugin loading and swapping
Console.WriteLine("PintoBean Plugin Host Demo Console");
Console.WriteLine("==================================");
Console.WriteLine();

// Display version information
Console.WriteLine($"Abstractions Version: {PintoBeanAbstractions.Version}");
Console.WriteLine($"Runtime Version: {PintoBeanRuntime.Version}");
Console.WriteLine();

// Setup plugin host and service registry
var serviceRegistry = new ServiceRegistry();
using var pluginHost = PluginHostNet.Create();

// Setup event handlers for plugin lifecycle
pluginHost.PluginLoaded += (sender, e) =>
{
    Console.WriteLine($"🔌 Plugin loaded: {e.Handle.Descriptor.Id} v{e.Handle.Descriptor.Version}");
};

pluginHost.PluginUnloaded += (sender, e) =>
{
    Console.WriteLine($"🔌 Plugin unloaded: {e.PluginId} v{e.Descriptor.Version}");
};

pluginHost.PluginFailed += (sender, e) =>
{
    Console.WriteLine($"❌ Plugin failed: {e.PluginId} - {e.Operation}: {e.Exception.Message}");
};

// Create a sample request for testing
var request = new HelloRequest
{
    Name = "Plugin Demo",
    Language = "en"
};

try
{
    Console.WriteLine("📂 Setting up plugins directory structure...");
    
    // Create plugins directory structure
    var pluginsDir = Path.Combine(AppContext.BaseDirectory, "plugins");
    var v1Dir = Path.Combine(pluginsDir, "v1");
    var v2Dir = Path.Combine(pluginsDir, "v2");
    
    Directory.CreateDirectory(v1Dir);
    Directory.CreateDirectory(v2Dir);
    
    // Copy plugin assembly to both directories
    var sourceAssembly = Path.Combine(AppContext.BaseDirectory, "Yokan.PintoBean.Providers.SamplePlugin.dll");
    var v1Assembly = Path.Combine(v1Dir, "Yokan.PintoBean.Providers.SamplePlugin.dll");
    var v2Assembly = Path.Combine(v2Dir, "Yokan.PintoBean.Providers.SamplePlugin.dll");
    
    if (File.Exists(sourceAssembly))
    {
        File.Copy(sourceAssembly, v1Assembly, true);
        File.Copy(sourceAssembly, v2Assembly, true);
        Console.WriteLine("✅ Plugin assemblies prepared");
    }
    else
    {
        Console.WriteLine("❌ Plugin assembly not found. Make sure to build the solution first.");
        return;
    }
    
    // Create manifest files in plugin directories
    await CreatePluginManifests(v1Dir, v2Dir);
    
    Console.WriteLine();
    Console.WriteLine("🚀 STEP 1: Loading Plugin v1.0...");
    
    // Load plugin v1
    var v1Manifest = await LoadManifestAsync(Path.Combine(v1Dir, "plugin.manifest.json"));
    var v1Descriptor = CreatePluginDescriptor(v1Manifest, v1Assembly);
    
    var v1Handle = await pluginHost.LoadPluginAsync(v1Descriptor);
    await pluginHost.ActivateAsync(v1Handle.Id);
    
    // Register the provider from the plugin
    var v1Provider = CreateProviderFromPlugin(v1Handle, "Yokan.PintoBean.Providers.SamplePlugin.SampleHelloProvider");
    var v1Capabilities = ProviderCapabilities.Create(v1Manifest.Id)
        .WithPriority(Priority.Normal)
        .WithPlatform(Platform.Any)
        .WithTags(v1Manifest.Capabilities);
    
    var v1Registration = serviceRegistry.Register<IHelloService>(v1Provider, v1Capabilities);
    
    // Test the v1 provider
    var typedRegistry = serviceRegistry.For<IHelloService>();
    var v1Response = await typedRegistry.InvokeAsync((service, ct) => service.SayHelloAsync(request, ct));
    
    Console.WriteLine($"✨ v1 Response: {v1Response.Message}");
    Console.WriteLine($"   Service: {v1Response.ServiceInfo}");
    Console.WriteLine();
    
    Console.WriteLine("🔄 STEP 2: Swapping to Plugin v2.0...");
    
    // Load plugin v2
    var v2Manifest = await LoadManifestAsync(Path.Combine(v2Dir, "plugin.manifest.json"));
    var v2Descriptor = CreatePluginDescriptor(v2Manifest, v2Assembly);
    
    var v2Handle = await pluginHost.LoadPluginAsync(v2Descriptor);
    await pluginHost.ActivateAsync(v2Handle.Id);
    
    // Unregister v1 provider
    serviceRegistry.Unregister(v1Registration);
    await pluginHost.DeactivateAsync(v1Handle.Id);
    
    // Register v2 provider
    var v2Provider = CreateProviderFromPlugin(v2Handle, "Yokan.PintoBean.Providers.SamplePlugin.SampleHelloProviderV2");
    var v2Capabilities = ProviderCapabilities.Create(v2Manifest.Id)
        .WithPriority(Priority.Normal)
        .WithPlatform(Platform.Any)
        .WithTags(v2Manifest.Capabilities);
    
    serviceRegistry.Register<IHelloService>(v2Provider, v2Capabilities);
    
    // Test the v2 provider
    var v2Response = await typedRegistry.InvokeAsync((service, ct) => service.SayHelloAsync(request, ct));
    
    Console.WriteLine($"✨ v2 Response: {v2Response.Message}");
    Console.WriteLine($"   Service: {v2Response.ServiceInfo}");
    Console.WriteLine();
    
    // Test goodbye method as well
    Console.WriteLine("🔄 STEP 3: Testing Goodbye method with v2.0...");
    var goodbyeResponse = await typedRegistry.InvokeAsync((service, ct) => service.SayGoodbyeAsync(request, ct));
    
    Console.WriteLine($"✨ Goodbye Response: {goodbyeResponse.Message}");
    Console.WriteLine($"   Service: {goodbyeResponse.ServiceInfo}");
    Console.WriteLine();
    
    Console.WriteLine("✅ Plugin swap demo completed successfully!");
    Console.WriteLine("   - ✅ Plugin v1 loaded and executed");
    Console.WriteLine("   - ✅ Plugin v2 loaded and swapped");
    Console.WriteLine("   - ✅ Different behavior demonstrated between versions");
    Console.WriteLine("   - ✅ Plugin manifests used for configuration");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Demo failed: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

// Helper methods
[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access", Justification = "Simple JSON serialization for manifests")]
[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling", Justification = "Simple JSON serialization for manifests")]
static async Task CreatePluginManifests(string v1Dir, string v2Dir)
{
    var v1Manifest = new
    {
        id = "sample-hello-v1",
        version = "1.0.0",
        name = "Sample Hello Plugin v1",
        description = "Sample plugin implementation of IHelloService with basic functionality",
        author = "PintoBean Team",
        assemblies = new[] { "Yokan.PintoBean.Providers.SamplePlugin.dll" },
        capabilities = new[] { "hello-service", "greeting" },
        entryType = "Yokan.PintoBean.Providers.SamplePlugin.SampleHelloProvider",
        contractVersion = "0.1.0"
    };
    
    var v2Manifest = new
    {
        id = "sample-hello-v2",
        version = "2.0.0",
        name = "Sample Hello Plugin v2",
        description = "Enhanced sample plugin implementation of IHelloService with improved messaging",
        author = "PintoBean Team",
        assemblies = new[] { "Yokan.PintoBean.Providers.SamplePlugin.dll" },
        capabilities = new[] { "hello-service", "greeting", "enhanced" },
        entryType = "Yokan.PintoBean.Providers.SamplePlugin.SampleHelloProviderV2",
        contractVersion = "0.1.0"
    };
    
    var v1ManifestJson = JsonSerializer.Serialize(v1Manifest, new JsonSerializerOptions { WriteIndented = true });
    var v2ManifestJson = JsonSerializer.Serialize(v2Manifest, new JsonSerializerOptions { WriteIndented = true });
    
    await File.WriteAllTextAsync(Path.Combine(v1Dir, "plugin.manifest.json"), v1ManifestJson);
    await File.WriteAllTextAsync(Path.Combine(v2Dir, "plugin.manifest.json"), v2ManifestJson);
    
    Console.WriteLine("✅ Plugin manifests created");
}

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access", Justification = "Simple JSON deserialization for manifests")]
[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling", Justification = "Simple JSON deserialization for manifests")]
static async Task<PluginManifest> LoadManifestAsync(string manifestPath)
{
    var manifestContent = await File.ReadAllTextAsync(manifestPath);
    var manifest = JsonSerializer.Deserialize<PluginManifest>(manifestContent);
    return manifest ?? throw new InvalidOperationException($"Failed to deserialize manifest from {manifestPath}");
}

static PluginDescriptor CreatePluginDescriptor(PluginManifest manifest, string assemblyPath)
{
    return new PluginDescriptor(manifest.Id, manifest.Version, assemblyPath)
    {
        Name = manifest.Name,
        Description = manifest.Description,
        Author = manifest.Author,
        ContractVersion = manifest.ContractVersion,
        Capabilities = ProviderCapabilities.Create(manifest.Id)
            .WithPriority(Priority.Normal)
            .WithPlatform(Platform.Any)
            .WithTags(manifest.Capabilities)
            .AddMetadata("entryType", manifest.EntryType ?? "")
    };
}

static IHelloService CreateProviderFromPlugin(PluginHandle handle, string typeName)
{
    // First load the assembly from the plugin descriptor
    var assembly = handle.LoadContext.Load(handle.Descriptor.AssemblyPaths.First());
    
    // Get the type from the load context
    if (!handle.LoadContext.TryGetType(typeName, out var providerType) || providerType == null)
    {
        throw new ArgumentException($"Type {typeName} not found in plugin");
    }
    
    // Create instance using the load context
    var providerInstance = handle.LoadContext.CreateInstance(providerType) as IHelloService;
    if (providerInstance == null)
    {
        throw new ArgumentException($"Type {typeName} does not implement IHelloService");
    }
    
    return providerInstance;
}
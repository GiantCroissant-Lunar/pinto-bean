using System;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Factory for creating .NET-specific PluginHost instances using collectible AssemblyLoadContext.
/// Provides proper assembly unloading capabilities for .NET applications.
/// </summary>
public static class PluginHostNet
{
    /// <summary>
    /// Creates a new PluginHost instance that uses AlcLoadContext for collectible assembly loading.
    /// </summary>
    /// <returns>A new PluginHost configured with AlcLoadContext.</returns>
    public static PluginHost Create()
    {
        return new PluginHost(CreateAlcLoadContext);
    }

    /// <summary>
    /// Creates a new PluginHost instance with a custom load context factory.
    /// </summary>
    /// <param name="loadContextFactory">Custom factory to create load contexts for plugins.</param>
    /// <returns>A new PluginHost configured with the specified factory.</returns>
    public static PluginHost Create(Func<PluginDescriptor, ILoadContext> loadContextFactory)
    {
        return new PluginHost(loadContextFactory);
    }

    private static ILoadContext CreateAlcLoadContext(PluginDescriptor descriptor)
    {
        return new AlcLoadContext(descriptor.Id);
    }
}
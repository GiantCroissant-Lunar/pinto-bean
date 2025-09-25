using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Default implementation of IPluginHost that manages plugin lifecycles.
/// </summary>
public sealed class PluginHost : IPluginHost
{
    private readonly ConcurrentDictionary<string, PluginHandle> _plugins = new();
    private readonly Func<PluginDescriptor, ILoadContext> _loadContextFactory;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginHost"/> class.
    /// </summary>
    /// <param name="loadContextFactory">Factory to create load contexts for plugins. If null, uses FakeLoadContext.</param>
    public PluginHost(Func<PluginDescriptor, ILoadContext>? loadContextFactory = null)
    {
        _loadContextFactory = loadContextFactory ?? (descriptor => new FakeLoadContext(descriptor.Id));
    }

    /// <inheritdoc />
    public IReadOnlyCollection<PluginHandle> LoadedPlugins 
    { 
        get 
        { 
            ThrowIfDisposed();
            return _plugins.Values.ToList(); 
        } 
    }

    /// <inheritdoc />
    public event EventHandler<PluginLoadedEventArgs>? PluginLoaded;

    /// <inheritdoc />
    public event EventHandler<PluginUnloadedEventArgs>? PluginUnloaded;

    /// <inheritdoc />
    public event EventHandler<PluginFailedEventArgs>? PluginFailed;

    /// <inheritdoc />
    public async Task<PluginHandle> LoadPluginAsync(PluginDescriptor descriptor)
    {
        if (descriptor == null)
            throw new ArgumentNullException(nameof(descriptor));
        
        ThrowIfDisposed();

        if (_plugins.ContainsKey(descriptor.Id))
        {
            throw new InvalidOperationException($"Plugin '{descriptor.Id}' is already loaded.");
        }

        try
        {
            var loadContext = _loadContextFactory(descriptor);
            var handle = new PluginHandle(descriptor.Id, loadContext, descriptor);
            
            // Load the primary assembly (first in the list)
            if (descriptor.AssemblyPaths.Count > 0)
            {
                await Task.Run(() => loadContext.Load(descriptor.AssemblyPaths[0]));
            }

            if (_plugins.TryAdd(descriptor.Id, handle))
            {
                PluginLoaded?.Invoke(this, new PluginLoadedEventArgs(handle));
                return handle;
            }
            else
            {
                loadContext.Dispose();
                throw new InvalidOperationException($"Failed to add plugin '{descriptor.Id}' to collection.");
            }
        }
        catch (Exception ex)
        {
            PluginFailed?.Invoke(this, new PluginFailedEventArgs(descriptor.Id, "Load", ex));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ActivateAsync(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            throw new ArgumentException("Plugin ID cannot be null or whitespace.", nameof(pluginId));
        
        ThrowIfDisposed();

        if (!_plugins.TryGetValue(pluginId, out var handle))
        {
            return false;
        }

        if (handle.State == PluginState.Active)
        {
            return true; // Already active
        }

        if (handle.State == PluginState.Failed || handle.State == PluginState.Unloaded)
        {
            return false; // Cannot activate failed or unloaded plugins
        }

        try
        {
            // Perform contract version validation only if the plugin declares a contract version
            if (!string.IsNullOrWhiteSpace(handle.Descriptor.ContractVersion))
            {
                if (!ContractVersioning.IsCompatible(handle.Descriptor.ContractVersion))
                {
                    var errorMessage = ContractVersioning.GetCompatibilityErrorMessage(pluginId, handle.Descriptor.ContractVersion);
                    var versionException = new InvalidOperationException(errorMessage);
                    handle.State = PluginState.Failed;
                    handle.LastError = versionException;
                    PluginFailed?.Invoke(this, new PluginFailedEventArgs(pluginId, "Activate", versionException));
                    return false;
                }

                // Perform type identity validation for any loaded contract types
                // This checks that contract interfaces/types come from Tier-1 assemblies in the default context
                if (handle.LoadContext != null)
                {
                    await ValidateContractTypesAsync(pluginId, handle);
                }
            }

            await Task.Run(() =>
            {
                handle.State = PluginState.Active;
                handle.ActivatedAt = DateTimeOffset.UtcNow;
            });
            
            return true;
        }
        catch (Exception ex)
        {
            handle.State = PluginState.Failed;
            handle.LastError = ex;
            PluginFailed?.Invoke(this, new PluginFailedEventArgs(pluginId, "Activate", ex));
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateAsync(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            throw new ArgumentException("Plugin ID cannot be null or whitespace.", nameof(pluginId));
        
        ThrowIfDisposed();

        if (!_plugins.TryGetValue(pluginId, out var handle))
        {
            return false;
        }

        if (handle.State == PluginState.Deactivated)
        {
            return true; // Already deactivated
        }

        if (handle.State != PluginState.Active)
        {
            return false; // Can only deactivate active plugins
        }

        try
        {
            // If the plugin instance implements IQuiesceable, call QuiesceAsync first
            if (handle.Instance is IQuiesceable quiesceable)
            {
                await quiesceable.QuiesceAsync();
            }

            await Task.Run(() =>
            {
                handle.State = PluginState.Deactivated;
                handle.DeactivatedAt = DateTimeOffset.UtcNow;
            });
            
            return true;
        }
        catch (Exception ex)
        {
            handle.State = PluginState.Failed;
            handle.LastError = ex;
            PluginFailed?.Invoke(this, new PluginFailedEventArgs(pluginId, "Deactivate", ex));
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UnloadAsync(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            throw new ArgumentException("Plugin ID cannot be null or whitespace.", nameof(pluginId));
        
        ThrowIfDisposed();

        if (!_plugins.TryRemove(pluginId, out var handle))
        {
            return false;
        }

        try
        {
            // If the plugin is active and implements IQuiesceable, call QuiesceAsync first
            if (handle.State == PluginState.Active && handle.Instance is IQuiesceable quiesceable)
            {
                await quiesceable.QuiesceAsync();
            }

            await Task.Run(() =>
            {
                handle.State = PluginState.Unloaded;
                handle.LoadContext.Dispose();
            });
           
            PluginUnloaded?.Invoke(this, new PluginUnloadedEventArgs(pluginId, handle.Descriptor));
            return true;
        }
        catch (Exception ex)
        {
            // Re-add the plugin back to the collection since unload failed
            _plugins.TryAdd(pluginId, handle);
            handle.State = PluginState.Failed;
            handle.LastError = ex;
            PluginFailed?.Invoke(this, new PluginFailedEventArgs(pluginId, "Unload", ex));
            return false;
        }
    }

    /// <inheritdoc />
    public PluginHandle? GetPlugin(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return null;
        
        ThrowIfDisposed();
        
        return _plugins.TryGetValue(pluginId, out var handle) ? handle : null;
    }

    /// <inheritdoc />
    public bool IsLoaded(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;
        
        ThrowIfDisposed();
        
        return _plugins.ContainsKey(pluginId);
    }

    /// <summary>
    /// Validates that any contract types used by the plugin come from Tier-1 assemblies in the default context.
    /// </summary>
    /// <param name="pluginId">The plugin ID for error reporting.</param>
    /// <param name="handle">The plugin handle containing the load context.</param>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access", Justification = "Plugin contract validation requires assembly analysis which is necessary for security")]
    private async Task ValidateContractTypesAsync(string pluginId, PluginHandle handle)
    {
        // This is a basic implementation that can be extended based on specific contract discovery mechanisms
        // For now, we'll validate any types that appear to be contracts based on common patterns
        
        await Task.Run(() =>
        {
            if (handle.LoadContext == null || handle.Instance == null)
                return;

            var pluginAssembly = handle.Instance.GetType().Assembly;
            var referencedAssemblies = pluginAssembly.GetReferencedAssemblies();

            // Check for contract assemblies in the plugin's dependencies
            foreach (var referencedAssembly in referencedAssemblies)
            {
                var assemblyName = referencedAssembly.Name?.ToLowerInvariant() ?? "";
                bool isContractAssembly = assemblyName.Contains("contracts") ||
                                        assemblyName.Contains("models") ||
                                        assemblyName.Contains("abstractions");

                if (isContractAssembly)
                {
                    try
                    {
                        // Try to load the assembly and verify it's from the default context
                        var loadedAssembly = Assembly.Load(referencedAssembly);
                        var loadContext = AssemblyLoadContext.GetLoadContext(loadedAssembly);
                        
                        if (loadContext != AssemblyLoadContext.Default)
                        {
                            throw new InvalidOperationException(
                                $"Plugin '{pluginId}' references contract assembly '{referencedAssembly.Name}' " +
                                "that is not loaded in the default AssemblyLoadContext. " +
                                "Contract assemblies must be loaded in the default context to ensure type identity.");
                        }
                    }
                    catch (Exception ex) when (!(ex is InvalidOperationException))
                    {
                        // If we can't load the assembly for validation, that's also a problem
                        throw new InvalidOperationException(
                            $"Plugin '{pluginId}' references contract assembly '{referencedAssembly.Name}' " +
                            "that cannot be validated for type identity. " +
                            "Ensure all contract assemblies are properly available.", ex);
                    }
                }
            }
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        var plugins = _plugins.Values.ToList();
        foreach (var plugin in plugins)
        {
            try
            {
                plugin.LoadContext.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        _plugins.Clear();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PluginHost));
    }
}
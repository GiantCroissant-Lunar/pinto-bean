using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
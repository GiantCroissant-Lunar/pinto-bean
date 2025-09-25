using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Unity-specific implementation of IPluginHost that supports soft-swap functionality.
/// Implements quiesce/flip/dispose pattern without unloading assemblies.
/// </summary>
public sealed class PluginHostUnity : IPluginHost
{
    private readonly ConcurrentDictionary<string, PluginHandle> _plugins = new();
    private readonly ConcurrentDictionary<string, PluginHandle> _quiescingPlugins = new();
    private readonly Func<PluginDescriptor, ILoadContext> _loadContextFactory;
    private readonly Timer _cleanupTimer;
    private readonly object _swapLock = new();
    private bool _disposed;

    /// <summary>
    /// Default grace period in seconds for quiescing plugins.
    /// </summary>
    public const int DefaultGracePeriodSeconds = 5;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginHostUnity"/> class.
    /// </summary>
    /// <param name="loadContextFactory">Factory to create load contexts for plugins. If null, uses HybridClrLoadContext.</param>
    public PluginHostUnity(Func<PluginDescriptor, ILoadContext>? loadContextFactory = null)
    {
        _loadContextFactory = loadContextFactory ?? (descriptor => new HybridClrLoadContext(descriptor.Id));
        
        // Timer to clean up quiesced plugins that have exceeded their grace period
        _cleanupTimer = new Timer(CleanupQuiescedPlugins, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
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

    /// <summary>
    /// Gets the collection of plugins currently being quiesced.
    /// </summary>
    public IReadOnlyCollection<PluginHandle> QuiescingPlugins 
    { 
        get 
        { 
            ThrowIfDisposed();
            return _quiescingPlugins.Values.ToList(); 
        } 
    }

    /// <inheritdoc />
    public event EventHandler<PluginLoadedEventArgs>? PluginLoaded;

    /// <inheritdoc />
    public event EventHandler<PluginUnloadedEventArgs>? PluginUnloaded;

    /// <inheritdoc />
    public event EventHandler<PluginFailedEventArgs>? PluginFailed;

    /// <summary>
    /// Occurs when a plugin is quiesced for soft-swap.
    /// </summary>
    public event EventHandler<PluginQuiescedEventArgs>? PluginQuiesced;

    /// <summary>
    /// Occurs when a plugin soft-swap is completed.
    /// </summary>
    public event EventHandler<PluginSwappedEventArgs>? PluginSwapped;

    /// <inheritdoc />
    public async Task<PluginHandle> LoadPluginAsync(PluginDescriptor descriptor)
    {
        if (descriptor == null)
            throw new ArgumentNullException(nameof(descriptor));
        
        ThrowIfDisposed();

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
                // Plugin with this ID already exists - dispose the load context
                loadContext.Dispose();
                throw new InvalidOperationException($"Plugin '{descriptor.Id}' is already loaded.");
            }
        }
        catch (Exception ex)
        {
            PluginFailed?.Invoke(this, new PluginFailedEventArgs(descriptor.Id, "LoadPlugin", ex));
            throw;
        }
    }

    /// <summary>
    /// Performs a soft-swap of a plugin, quiescing the old version and activating the new version.
    /// </summary>
    /// <param name="pluginId">The ID of the plugin to swap.</param>
    /// <param name="newDescriptor">The descriptor for the new version.</param>
    /// <returns>true if the swap was initiated successfully; otherwise, false.</returns>
    public async Task<bool> SoftSwapAsync(string pluginId, PluginDescriptor newDescriptor)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            throw new ArgumentException("Plugin ID cannot be null or whitespace.", nameof(pluginId));
        if (newDescriptor == null)
            throw new ArgumentNullException(nameof(newDescriptor));
        
        ThrowIfDisposed();

        PluginHandle? oldHandle;
        lock (_swapLock)
        {
            if (!_plugins.TryGetValue(pluginId, out oldHandle))
            {
                return false; // Plugin not found
            }

            if (oldHandle.State != PluginState.Active)
            {
                return false; // Can only swap active plugins
            }

            // Step 1: Quiesce the old plugin
            oldHandle.State = PluginState.Quiescing;
            var graceSeconds = GetGracePeriod(oldHandle);
            oldHandle.QuiescedAt = DateTimeOffset.UtcNow;
            oldHandle.GracePeriodSeconds = graceSeconds;
            
            _quiescingPlugins.TryAdd(pluginId, oldHandle);
            PluginQuiesced?.Invoke(this, new PluginQuiescedEventArgs(oldHandle, graceSeconds));
        }

        try
        {
            // Step 2: Load and activate the new version (outside lock to allow async)
            var newHandle = await LoadPluginAsync(newDescriptor);
            newHandle.State = PluginState.Active;
            newHandle.ActivatedAt = DateTimeOffset.UtcNow;

            // Step 3: Replace the active plugin
            lock (_swapLock)
            {
                _plugins.TryUpdate(pluginId, newHandle, oldHandle);
            }
            
            PluginSwapped?.Invoke(this, new PluginSwappedEventArgs(oldHandle, newHandle));
            
            return true;
        }
        catch (Exception ex)
        {
            PluginFailed?.Invoke(this, new PluginFailedEventArgs(pluginId, "SoftSwap", ex));
            return false;
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

        if (handle.State == PluginState.Failed || handle.State == PluginState.Unloaded)
        {
            return false; // Cannot deactivate failed or unloaded plugins
        }

        try
        {
            await Task.Run(() =>
            {
                handle.State = PluginState.Deactivated;
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
                // In Unity/HybridCLR, we don't actually unload the assemblies
                // Just dispose the load context for cleanup
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
        ThrowIfDisposed();
        _plugins.TryGetValue(pluginId, out var handle);
        return handle;
    }

    /// <inheritdoc />
    public bool IsLoaded(string pluginId)
    {
        ThrowIfDisposed();
        return _plugins.ContainsKey(pluginId);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        _cleanupTimer?.Dispose();

        // Dispose all active plugins
        foreach (var handle in _plugins.Values)
        {
            try
            {
                handle.LoadContext.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        // Dispose all quiescing plugins
        foreach (var handle in _quiescingPlugins.Values)
        {
            try
            {
                handle.LoadContext.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        _plugins.Clear();
        _quiescingPlugins.Clear();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new InvalidOperationException("Plugin host has been disposed.");
    }

    private int GetGracePeriod(PluginHandle handle)
    {
        // Look for QuiesceAttribute on the plugin's main type
        try
        {
            if (handle.Descriptor.AssemblyPaths.Count == 0) 
                return DefaultGracePeriodSeconds;

            var assembly = handle.LoadContext.Load(handle.Descriptor.AssemblyPaths[0]);
#pragma warning disable IL2026 // Using member which has 'RequiresUnreferencedCodeAttribute'
            var types = assembly.GetTypes();
#pragma warning restore IL2026
            
            foreach (var type in types)
            {
                var attribute = type.GetCustomAttribute<QuiesceAttribute>();
                if (attribute != null)
                {
                    return attribute.Seconds;
                }
            }
        }
        catch
        {
            // If we can't determine the grace period, use default
        }

        return DefaultGracePeriodSeconds;
    }

    private void CleanupQuiescedPlugins(object? state)
    {
        if (_disposed) return;

        var now = DateTimeOffset.UtcNow;
        var toRemove = new List<string>();

        foreach (var kvp in _quiescingPlugins)
        {
            var handle = kvp.Value;
            if (handle.QuiescedAt.HasValue && handle.GracePeriodSeconds.HasValue)
            {
                var gracePeriod = TimeSpan.FromSeconds(handle.GracePeriodSeconds.Value);
                if (now - handle.QuiescedAt.Value >= gracePeriod)
                {
                    toRemove.Add(kvp.Key);
                }
            }
        }

        foreach (var pluginId in toRemove)
        {
            if (_quiescingPlugins.TryRemove(pluginId, out var handle))
            {
                try
                {
                    handle.LoadContext.Dispose();
                    PluginUnloaded?.Invoke(this, new PluginUnloadedEventArgs(pluginId, handle.Descriptor));
                }
                catch (Exception ex)
                {
                    PluginFailed?.Invoke(this, new PluginFailedEventArgs(pluginId, "CleanupQuiesced", ex));
                }
            }
        }
    }
}

/// <summary>
/// Provides data for the PluginQuiesced event.
/// </summary>
public sealed class PluginQuiescedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the plugin handle that was quiesced.
    /// </summary>
    public PluginHandle Handle { get; }

    /// <summary>
    /// Gets the grace period in seconds.
    /// </summary>
    public int GracePeriodSeconds { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginQuiescedEventArgs"/> class.
    /// </summary>
    /// <param name="handle">The plugin handle.</param>
    /// <param name="gracePeriodSeconds">The grace period in seconds.</param>
    public PluginQuiescedEventArgs(PluginHandle handle, int gracePeriodSeconds)
    {
        Handle = handle ?? throw new ArgumentNullException(nameof(handle));
        GracePeriodSeconds = gracePeriodSeconds;
    }
}

/// <summary>
/// Provides data for the PluginSwapped event.
/// </summary>
public sealed class PluginSwappedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the old plugin handle that was quiesced.
    /// </summary>
    public PluginHandle OldHandle { get; }

    /// <summary>
    /// Gets the new plugin handle that was activated.
    /// </summary>
    public PluginHandle NewHandle { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginSwappedEventArgs"/> class.
    /// </summary>
    /// <param name="oldHandle">The old plugin handle.</param>
    /// <param name="newHandle">The new plugin handle.</param>
    public PluginSwappedEventArgs(PluginHandle oldHandle, PluginHandle newHandle)
    {
        OldHandle = oldHandle ?? throw new ArgumentNullException(nameof(oldHandle));
        NewHandle = newHandle ?? throw new ArgumentNullException(nameof(newHandle));
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// A Unity-oriented load context that does not unload but supports safe soft-swap.
/// Only implements Load and CreateInstance methods from ILoadContext.
/// </summary>
public sealed class HybridClrLoadContext : ILoadContext
{
    private readonly Dictionary<string, Assembly> _loadedAssemblies = new();
    private readonly Dictionary<string, Type> _typeCache = new();
    private bool _disposed;

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HybridClrLoadContext"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this load context.</param>
    public HybridClrLoadContext(string? id = null)
    {
        Id = id ?? Guid.NewGuid().ToString();
    }

    /// <inheritdoc />
    public Assembly Load(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
            throw new ArgumentException("Assembly path cannot be null or whitespace.", nameof(assemblyPath));
        
        ThrowIfDisposed();

        if (_loadedAssemblies.TryGetValue(assemblyPath, out var existingAssembly))
        {
            return existingAssembly;
        }

        try
        {
            // In HybridCLR/Unity, we would use Assembly.LoadFrom or similar
            // For now, we simulate by loading from current domain
#pragma warning disable IL2026 // Using member which has 'RequiresUnreferencedCodeAttribute'
            var assembly = Assembly.LoadFrom(assemblyPath);
#pragma warning restore IL2026
            _loadedAssemblies[assemblyPath] = assembly;
            
            // Cache types from the loaded assembly
#pragma warning disable IL2026 // Using member which has 'RequiresUnreferencedCodeAttribute'
            foreach (var type in assembly.GetTypes())
#pragma warning restore IL2026
            {
                _typeCache[type.FullName!] = type;
            }
            
            return assembly;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load assembly from '{assemblyPath}': {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public Assembly Load(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        
        ThrowIfDisposed();

        var assembly = type.Assembly;
#pragma warning disable IL3000 // Assembly.Location always returns empty string for single-file apps
        var assemblyPath = assembly.Location;
#pragma warning restore IL3000
        
        if (!string.IsNullOrEmpty(assemblyPath) && !_loadedAssemblies.ContainsKey(assemblyPath))
        {
            _loadedAssemblies[assemblyPath] = assembly;
            
            // Cache types from the loaded assembly
#pragma warning disable IL2026 // Using member which has 'RequiresUnreferencedCodeAttribute'
            foreach (var assemblyType in assembly.GetTypes())
#pragma warning restore IL2026
            {
                _typeCache[assemblyType.FullName!] = assemblyType;
            }
        }
        
        return assembly;
    }

    /// <inheritdoc />
    public bool TryGetType(string typeName, out Type? type)
    {
        ThrowIfDisposed();
        return _typeCache.TryGetValue(typeName, out type);
    }

    /// <inheritdoc />
    public object CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, params object?[]? args)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        
        ThrowIfDisposed();

        try
        {
            // Ensure the type's assembly is loaded
            Load(type);
            
            return Activator.CreateInstance(type, args) 
                ?? throw new InvalidOperationException($"Failed to create instance of type {type.FullName}");
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            throw new InvalidOperationException($"Failed to create instance of type {type.FullName}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public T CreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params object?[]? args)
    {
        var instance = CreateInstance(typeof(T), args);
        return (T)instance;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        
        // HybridCLR contexts don't actually unload - this is a no-op for assemblies
        // but we still clean up our tracking data structures
        _loadedAssemblies.Clear();
        _typeCache.Clear();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new InvalidOperationException("Load context has been disposed.");
    }
}
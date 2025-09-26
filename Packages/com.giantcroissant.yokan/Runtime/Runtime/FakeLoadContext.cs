using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// A fake implementation of ILoadContext for testing purposes.
/// Does not actually load assemblies but simulates the behavior.
/// </summary>
public sealed class FakeLoadContext : ILoadContext
{
    private readonly Dictionary<string, Assembly> _loadedAssemblies = new();
    private readonly Dictionary<string, Type> _typeCache = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeLoadContext"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this load context.</param>
    public FakeLoadContext(string? id = null)
    {
        Id = id ?? Guid.NewGuid().ToString();
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Registers a fake assembly to be returned by Load operations.
    /// </summary>
    /// <param name="path">The assembly path.</param>
    /// <param name="assembly">The assembly to return.</param>
    public void RegisterAssembly(string path, Assembly assembly)
    {
        ThrowIfDisposed();
        _loadedAssemblies[path] = assembly;
        
        // Cache types from this assembly
        try
        {
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
            foreach (var type in assembly.GetTypes())
#pragma warning restore IL2026
            {
                _typeCache[type.FullName ?? type.Name] = type;
            }
        }
        catch
        {
            // Ignore errors when getting types (e.g., ReflectionTypeLoadException)
        }
    }

    /// <summary>
    /// Registers a fake type to be returned by TryGetType.
    /// </summary>
    /// <param name="typeName">The full name of the type.</param>
    /// <param name="type">The type to return.</param>
    public void RegisterType(string typeName, Type type)
    {
        ThrowIfDisposed();
        _typeCache[typeName] = type;
    }

    /// <inheritdoc />
    public Assembly Load(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
            throw new ArgumentException("Assembly path cannot be null or whitespace.", nameof(assemblyPath));
        
        ThrowIfDisposed();

        if (_loadedAssemblies.TryGetValue(assemblyPath, out var assembly))
        {
            return assembly;
        }

        throw new InvalidOperationException($"Assembly not found: {assemblyPath}. Use RegisterAssembly to add fake assemblies.");
    }

    /// <inheritdoc />
    public Assembly Load(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));
        
        ThrowIfDisposed();

        // Return the assembly that contains this type
        var assembly = type.Assembly;
        
        // If it's already in our cache, return it
        var existingEntry = _loadedAssemblies.FirstOrDefault(kvp => kvp.Value == assembly);
        if (existingEntry.Key != null)
        {
            return existingEntry.Value;
        }

        // Add it to our cache with a generated path
        var fakePath = $"fake://{assembly.GetName().Name}.dll";
        _loadedAssemblies[fakePath] = assembly;
        
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
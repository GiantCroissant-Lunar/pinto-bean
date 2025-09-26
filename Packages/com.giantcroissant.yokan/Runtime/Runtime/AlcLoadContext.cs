using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Implementation of ILoadContext using .NET's collectible AssemblyLoadContext.
/// Provides proper assembly isolation and unload capabilities for plugins.
/// </summary>
public sealed class AlcLoadContext : ILoadContext
{
    private readonly AssemblyLoadContext _assemblyLoadContext;
    private readonly List<Assembly> _loadedAssemblies = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlcLoadContext"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this load context.</param>
    public AlcLoadContext(string? id = null)
    {
        Id = id ?? Guid.NewGuid().ToString();
        _assemblyLoadContext = new CollectibleAssemblyLoadContext(Id);
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public bool IsDisposed => _disposed;

    /// <inheritdoc />
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", 
        Justification = "Loading assemblies from paths is required for plugin functionality.")]
    public Assembly Load(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
            throw new ArgumentException("Assembly path cannot be null or empty.", nameof(assemblyPath));

        ThrowIfDisposed();

        if (!File.Exists(assemblyPath))
            throw new FileNotFoundException($"Assembly file not found: {assemblyPath}", assemblyPath);

        var assembly = _assemblyLoadContext.LoadFromAssemblyPath(assemblyPath);
        _loadedAssemblies.Add(assembly);
        return assembly;
    }

    /// <inheritdoc />
    public Assembly Load(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        ThrowIfDisposed();

        var assembly = type.Assembly;
        if (!_loadedAssemblies.Contains(assembly))
        {
            _loadedAssemblies.Add(assembly);
        }
        return assembly;
    }

    /// <inheritdoc />
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", 
        Justification = "Type lookup by name is required for plugin functionality.")]
    public bool TryGetType(string typeName, out Type? type)
    {
        ThrowIfDisposed();

        type = null;
        if (string.IsNullOrWhiteSpace(typeName))
            return false;

        foreach (var assembly in _loadedAssemblies)
        {
            try
            {
                type = assembly.GetType(typeName);
                if (type != null)
                    return true;
            }
            catch
            {
                // Continue searching in other assemblies
            }
        }

        return false;
    }

    /// <inheritdoc />
    public object CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, params object?[]? args)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        ThrowIfDisposed();

        try
        {
            return Activator.CreateInstance(type, args) ?? throw new InvalidOperationException($"Failed to create instance of type '{type.FullName}'.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create instance of type '{type.FullName}': {ex.Message}", ex);
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
        _assemblyLoadContext.Unload();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new InvalidOperationException("Load context has been disposed.");
    }

    /// <summary>
    /// Internal collectible AssemblyLoadContext implementation.
    /// </summary>
    private sealed class CollectibleAssemblyLoadContext : AssemblyLoadContext
    {
        public CollectibleAssemblyLoadContext(string name) : base(name, isCollectible: true)
        {
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // Let the default context handle system assemblies and shared dependencies
            // to avoid type identity issues
            if (IsSystemAssembly(assemblyName))
            {
                return null; // Use default context
            }

            return null; // Fallback to default resolution
        }

        private static bool IsSystemAssembly(AssemblyName assemblyName)
        {
            var name = assemblyName.Name;
            return name != null && (
                name.StartsWith("System.", StringComparison.Ordinal) ||
                name.StartsWith("Microsoft.", StringComparison.Ordinal) ||
                name.Equals("mscorlib", StringComparison.Ordinal) ||
                name.Equals("netstandard", StringComparison.Ordinal) ||
                name.StartsWith("Yokan.PintoBean", StringComparison.Ordinal) // Share runtime types
            );
        }
    }
}
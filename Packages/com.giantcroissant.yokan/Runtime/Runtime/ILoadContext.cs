using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Represents an isolated assembly loading context for plugins.
/// Provides engine-agnostic abstraction over assembly loading mechanisms.
/// </summary>
public interface ILoadContext : IDisposable
{
    /// <summary>
    /// Gets the unique identifier for this load context.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets a value indicating whether this load context is disposed.
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// Loads an assembly from the specified file path.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly file.</param>
    /// <returns>The loaded assembly.</returns>
    /// <exception cref="ArgumentException">Thrown when assemblyPath is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the context is disposed.</exception>
    Assembly Load(string assemblyPath);

    /// <summary>
    /// Loads an assembly containing the specified type.
    /// </summary>
    /// <param name="type">The type whose assembly should be loaded.</param>
    /// <returns>The loaded assembly.</returns>
    /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the context is disposed.</exception>
    Assembly Load(Type type);

    /// <summary>
    /// Attempts to get a type by name from loaded assemblies.
    /// </summary>
    /// <param name="typeName">The full name of the type to find.</param>
    /// <param name="type">When this method returns, contains the type if found; otherwise, null.</param>
    /// <returns>true if the type was found; otherwise, false.</returns>
    bool TryGetType(string typeName, out Type? type);

    /// <summary>
    /// Creates an instance of the specified type.
    /// </summary>
    /// <param name="type">The type to instantiate.</param>
    /// <param name="args">Optional constructor arguments.</param>
    /// <returns>An instance of the specified type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the context is disposed or instantiation fails.</exception>
    object CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, params object?[]? args);

    /// <summary>
    /// Creates an instance of the specified type.
    /// </summary>
    /// <typeparam name="T">The type to instantiate.</typeparam>
    /// <param name="args">Optional constructor arguments.</param>
    /// <returns>An instance of the specified type.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the context is disposed or instantiation fails.</exception>
    T CreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params object?[]? args);
}
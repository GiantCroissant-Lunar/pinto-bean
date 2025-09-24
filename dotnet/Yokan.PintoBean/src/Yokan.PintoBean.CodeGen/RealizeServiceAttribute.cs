// Tier-2: Source generators and analyzers for Yokan PintoBean service platform

using System;

namespace Yokan.PintoBean.CodeGen;

/// <summary>
/// Marks a partial class as realizing one or more service contracts through code generation.
/// The marked class will have its contract implementation methods generated automatically,
/// delegating to registered providers via a typed registry.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is only allowed in Tier-2 (Generated Façades), Tier-3 (Adapters),
/// and Tier-4 (Providers). Usage in Tier-1 (Contracts/Models) will generate analyzer
/// error SG0001.
/// </para>
/// <para>
/// The source generator will create partial method implementations for all methods
/// defined in the specified contract types, routing calls through the service registry
/// with appropriate cross-cutting concerns (resilience, telemetry, etc.).
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RealizeServiceAttribute : Attribute
{
    /// <summary>
    /// Gets the service contract types that this class realizes.
    /// </summary>
    public Type[] Contracts { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RealizeServiceAttribute"/> class
    /// with the specified service contracts.
    /// </summary>
    /// <param name="contracts">The service contract types to realize. Cannot be empty.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="contracts"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="contracts"/> is empty.</exception>
    public RealizeServiceAttribute(params Type[] contracts)
    {
        if (contracts == null)
            throw new ArgumentNullException(nameof(contracts));

        if (contracts.Length == 0)
            throw new ArgumentException("At least one contract must be specified.", nameof(contracts));

        Contracts = contracts;
    }
}

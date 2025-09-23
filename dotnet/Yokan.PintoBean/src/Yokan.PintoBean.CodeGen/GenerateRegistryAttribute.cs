// Tier-2: Source generators and analyzers for Yokan PintoBean service platform

using System;

namespace Yokan.PintoBean.CodeGen;

/// <summary>
/// Marks a contract type for registry scaffolding generation. The source generator will
/// create typed selection scaffolding including selection modes, strategy interfaces,
/// and registry helpers for the specified contract.
/// </summary>
/// <remarks>
/// <para>
/// This attribute generates the following scaffolding for a contract named 'IMyService':
/// </para>
/// <list type="bullet">
/// <item><description><c>IMyServiceSelectionMode</c> - enumeration of available selection strategies</description></item>
/// <item><description><c>IMyServiceStrategy</c> - strategy interface for custom selection logic</description></item>
/// <item><description><c>IMyServiceRegistry</c> - typed registry with helpers for provider registration and strategy selection</description></item>
/// <item><description>DI extensions for registering providers and configuring strategies (PickOne, FanOut, Sharded when enabled)</description></item>
/// </list>
/// <para>
/// The generated registry scaffolding enables type-safe provider registration and 
/// strategy selection at both design-time and runtime.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateRegistryAttribute : Attribute
{
    /// <summary>
    /// Gets the service contract type for which to generate registry scaffolding.
    /// </summary>
    public Type Contract { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateRegistryAttribute"/> class
    /// with the specified service contract.
    /// </summary>
    /// <param name="contract">The service contract type for registry generation. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="contract"/> is null.</exception>
    public GenerateRegistryAttribute(Type contract)
    {
        Contract = contract ?? throw new ArgumentNullException(nameof(contract));
    }
}
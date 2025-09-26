using System;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Specifies the grace period in seconds for quiescing a provider during soft-swap operations.
/// Applied to provider types to override the default grace window.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public sealed class QuiesceAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the grace period in seconds.
    /// </summary>
    public int Seconds { get; set; } = 5;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuiesceAttribute"/> class.
    /// </summary>
    public QuiesceAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuiesceAttribute"/> class.
    /// </summary>
    /// <param name="seconds">The grace period in seconds. Must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when seconds is not positive.</exception>
    public QuiesceAttribute(int seconds)
    {
        if (seconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Grace period must be positive.");
        
        Seconds = seconds;
    }
}
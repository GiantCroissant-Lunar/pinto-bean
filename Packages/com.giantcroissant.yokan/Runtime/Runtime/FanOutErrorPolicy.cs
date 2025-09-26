// Tier-3: FanOut error handling policy definitions for result aggregation

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Defines error handling policies for FanOut aggregation operations.
/// Controls how the FanOut strategy handles failures when aggregating results from multiple providers.
/// </summary>
public enum FanOutErrorPolicy
{
    /// <summary>
    /// Continue processing all providers even if some fail.
    /// Failed operations are collected and available in aggregation metadata, but do not stop execution.
    /// This is the default behavior, suitable for telemetry-style operations where partial success is acceptable.
    /// </summary>
    Continue = 0,

    /// <summary>
    /// Stop processing immediately when the first provider fails.
    /// Throws the first encountered exception and does not invoke remaining providers.
    /// Suitable for critical operations where all providers must succeed or the entire operation should fail.
    /// </summary>
    FailFast = 1
}
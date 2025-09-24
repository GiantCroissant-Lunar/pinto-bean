// Tier-3: Priority enumeration for provider registration and selection

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Defines the priority level for service providers.
/// Higher values indicate higher priority in selection strategies.
/// </summary>
public enum Priority
{
    /// <summary>
    /// Low priority provider - used as fallback.
    /// </summary>
    Low = 10,

    /// <summary>
    /// Normal priority provider - default level.
    /// </summary>
    Normal = 50,

    /// <summary>
    /// High priority provider - preferred selection.
    /// </summary>
    High = 100,

    /// <summary>
    /// Critical priority provider - always preferred when available.
    /// </summary>
    Critical = 1000
}
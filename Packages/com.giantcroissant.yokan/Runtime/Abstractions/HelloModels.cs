// Tier-1: DTOs for hello service contract

namespace Yokan.PintoBean.Abstractions;

/// <summary>
/// Request DTO for hello service operations.
/// Contains the name and optional parameters for greeting operations.
/// </summary>
public sealed record HelloRequest
{
    /// <summary>
    /// The name to include in the greeting. Required.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional language/locale for the greeting (e.g., "en", "es", "fr").
    /// Defaults to "en" if not specified.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Optional context for the greeting (e.g., "formal", "casual", "business").
    /// Defaults to "casual" if not specified.
    /// </summary>
    public string? Context { get; init; }
}

/// <summary>
/// Response DTO for hello service operations.
/// Contains the generated greeting message and metadata.
/// </summary>
public sealed record HelloResponse
{
    /// <summary>
    /// The generated greeting or farewell message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The timestamp when the response was generated.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// The language/locale used for the response.
    /// </summary>
    public string Language { get; init; } = "en";

    /// <summary>
    /// Optional metadata about the service that generated the response.
    /// </summary>
    public string? ServiceInfo { get; init; }
}

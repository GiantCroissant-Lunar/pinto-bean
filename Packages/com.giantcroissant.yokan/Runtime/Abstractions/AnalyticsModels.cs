// Tier-1: DTOs for analytics service contract

using System;
using System.Collections.Generic;

namespace Yokan.PintoBean.Abstractions;

/// <summary>
/// Analytics event DTO for tracking operations.
/// Contains the event name and associated data for analytics operations.
/// </summary>
public sealed record AnalyticsEvent
{
    /// <summary>
    /// The name/identifier of the analytics event. Required.
    /// Examples: "player.level.complete", "system.startup", "user.login"
    /// </summary>
    public required string EventName { get; init; }

    /// <summary>
    /// Optional event data/properties as key-value pairs.
    /// Contains additional context and metrics for the event.
    /// </summary>
    public Dictionary<string, object>? Properties { get; init; }

    /// <summary>
    /// Optional user identifier associated with the event.
    /// Used for user-specific analytics and personalization.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Optional session identifier for grouping related events.
    /// Used for session-based analytics and flow tracking.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// The timestamp when the event occurred.
    /// Defaults to the current UTC time if not specified.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
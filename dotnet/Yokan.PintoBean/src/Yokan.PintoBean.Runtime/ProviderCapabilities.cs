// Tier-3: Provider capabilities model for service registration

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Describes the capabilities and metadata of a service provider.
/// Used for filtering and selection in the service registry.
/// </summary>
public sealed record ProviderCapabilities
{
    /// <summary>
    /// The unique identifier for this provider instance.
    /// </summary>
    public required string ProviderId { get; init; }

    /// <summary>
    /// The target platform for this provider.
    /// </summary>
    public Platform Platform { get; init; } = Platform.Any;

    /// <summary>
    /// The priority level of this provider.
    /// </summary>
    public Priority Priority { get; init; } = Priority.Normal;

    /// <summary>
    /// Tags associated with this provider for capability filtering.
    /// Examples: "analytics", "telemetry", "primary", "fallback"
    /// </summary>
    public ImmutableHashSet<string> Tags { get; init; } = ImmutableHashSet<string>.Empty;

    /// <summary>
    /// Optional metadata dictionary for provider-specific information.
    /// </summary>
    public ImmutableDictionary<string, object> Metadata { get; init; } = ImmutableDictionary<string, object>.Empty;

    /// <summary>
    /// The timestamp when this provider was registered.
    /// </summary>
    public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a new ProviderCapabilities instance with the specified provider ID.
    /// </summary>
    /// <param name="providerId">The unique provider identifier.</param>
    /// <returns>A new ProviderCapabilities instance.</returns>
    public static ProviderCapabilities Create(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider ID cannot be null or empty.", nameof(providerId));

        return new ProviderCapabilities { ProviderId = providerId };
    }

    /// <summary>
    /// Creates a copy of this instance with the specified platform.
    /// </summary>
    /// <param name="platform">The target platform.</param>
    /// <returns>A new ProviderCapabilities instance with the updated platform.</returns>
    public ProviderCapabilities WithPlatform(Platform platform)
        => this with { Platform = platform };

    /// <summary>
    /// Creates a copy of this instance with the specified priority.
    /// </summary>
    /// <param name="priority">The priority level.</param>
    /// <returns>A new ProviderCapabilities instance with the updated priority.</returns>
    public ProviderCapabilities WithPriority(Priority priority)
        => this with { Priority = priority };

    /// <summary>
    /// Creates a copy of this instance with the specified tags.
    /// </summary>
    /// <param name="tags">The capability tags.</param>
    /// <returns>A new ProviderCapabilities instance with the updated tags.</returns>
    public ProviderCapabilities WithTags(params string[] tags)
        => this with { Tags = tags?.ToImmutableHashSet() ?? ImmutableHashSet<string>.Empty };

    /// <summary>
    /// Creates a copy of this instance with additional tags.
    /// </summary>
    /// <param name="tags">The additional capability tags.</param>
    /// <returns>A new ProviderCapabilities instance with the combined tags.</returns>
    public ProviderCapabilities AddTags(params string[] tags)
    {
        if (tags == null || tags.Length == 0)
            return this;

        return this with { Tags = Tags.Union(tags) };
    }

    /// <summary>
    /// Creates a copy of this instance with the specified metadata.
    /// </summary>
    /// <param name="metadata">The metadata dictionary.</param>
    /// <returns>A new ProviderCapabilities instance with the updated metadata.</returns>
    public ProviderCapabilities WithMetadata(IReadOnlyDictionary<string, object> metadata)
        => this with { Metadata = metadata?.ToImmutableDictionary() ?? ImmutableDictionary<string, object>.Empty };

    /// <summary>
    /// Creates a copy of this instance with additional metadata.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>A new ProviderCapabilities instance with the additional metadata.</returns>
    public ProviderCapabilities AddMetadata(string key, object value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Metadata key cannot be null or empty.", nameof(key));

        return this with { Metadata = Metadata.SetItem(key, value) };
    }

    /// <summary>
    /// Checks if this provider has all the specified tags.
    /// </summary>
    /// <param name="requiredTags">The tags to check for.</param>
    /// <returns>True if all required tags are present, false otherwise.</returns>
    public bool HasTags(params string[] requiredTags)
        => requiredTags?.All(tag => Tags.Contains(tag)) ?? true;

    /// <summary>
    /// Checks if this provider has any of the specified tags.
    /// </summary>
    /// <param name="anyTags">The tags to check for.</param>
    /// <returns>True if any of the specified tags are present, false otherwise.</returns>
    public bool HasAnyTag(params string[] anyTags)
        => anyTags?.Any(tag => Tags.Contains(tag)) ?? false;
}
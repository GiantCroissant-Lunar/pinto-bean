// Tier-3: Provider selection cache interface for shared caching across strategies

using System;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Defines a cache for provider selection results that can be shared across multiple selection strategies.
/// Provides TTL-based caching with invalidation on provider registration changes.
/// </summary>
/// <typeparam name="TService">The service contract type.</typeparam>
public interface IProviderSelectionCache<TService>
    where TService : class
{
    /// <summary>
    /// Attempts to get a cached selection result.
    /// </summary>
    /// <param name="context">The selection context to generate cache key from.</param>
    /// <returns>The cached result if found and not expired, null otherwise.</returns>
    ISelectionResult<TService>? TryGet(ISelectionContext<TService> context);

    /// <summary>
    /// Gets a cached selection result, throwing if not found or expired.
    /// </summary>
    /// <param name="context">The selection context to generate cache key from.</param>
    /// <returns>The cached result.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the result is not found in cache or has expired.</exception>
    ISelectionResult<TService> Get(ISelectionContext<TService> context);

    /// <summary>
    /// Caches a selection result with optional TTL override.
    /// </summary>
    /// <param name="context">The selection context to generate cache key from.</param>
    /// <param name="result">The result to cache.</param>
    /// <param name="ttl">Optional TTL override for this entry.</param>
    void Set(ISelectionContext<TService> context, ISelectionResult<TService> result, TimeSpan? ttl = null);

    /// <summary>
    /// Removes a specific cached entry.
    /// </summary>
    /// <param name="context">The selection context to generate cache key from.</param>
    /// <returns>True if the entry was found and removed, false otherwise.</returns>
    bool Remove(ISelectionContext<TService> context);

    /// <summary>
    /// Clears all cached entries (typically called on provider registration changes).
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the number of cached entries currently stored.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the default TTL for cache entries.
    /// </summary>
    TimeSpan DefaultTtl { get; }
}
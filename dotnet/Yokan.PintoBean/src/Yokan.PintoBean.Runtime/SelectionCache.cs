// Tier-3: TTL-based selection cache for provider selection strategies

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Cache entry for selection results with TTL expiration.
/// </summary>
internal sealed class SelectionCacheEntry<TService>
    where TService : class
{
    public ISelectionResult<TService> Result { get; }
    public DateTime ExpiresAt { get; }

    public SelectionCacheEntry(ISelectionResult<TService> result, TimeSpan ttl)
    {
        Result = result;
        ExpiresAt = DateTime.UtcNow.Add(ttl);
    }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}

/// <summary>
/// TTL-based cache for selection strategy results.
/// Caches results by (contract, criteria) combination and invalidates on provider changes.
/// </summary>
internal sealed class SelectionCache<TService>
    where TService : class
{
    private readonly ConcurrentDictionary<string, SelectionCacheEntry<TService>> _cache = new();
    private readonly TimeSpan _defaultTtl;

    public SelectionCache(TimeSpan? defaultTtl = null)
    {
        _defaultTtl = defaultTtl ?? TimeSpan.FromMinutes(5); // Default 5-minute TTL
    }

    /// <summary>
    /// Attempts to get a cached selection result.
    /// </summary>
    /// <param name="context">The selection context to generate cache key from.</param>
    /// <returns>The cached result if found and not expired, null otherwise.</returns>
    public ISelectionResult<TService>? TryGet(ISelectionContext<TService> context)
    {
        var key = GenerateCacheKey(context);
        
        if (_cache.TryGetValue(key, out var entry))
        {
            if (!entry.IsExpired)
            {
                return entry.Result;
            }
            
            // Remove expired entry
            _cache.TryRemove(key, out _);
        }

        return null;
    }

    /// <summary>
    /// Caches a selection result.
    /// </summary>
    /// <param name="context">The selection context to generate cache key from.</param>
    /// <param name="result">The result to cache.</param>
    /// <param name="ttl">Optional TTL override for this entry.</param>
    public void Set(ISelectionContext<TService> context, ISelectionResult<TService> result, TimeSpan? ttl = null)
    {
        var key = GenerateCacheKey(context);
        var entry = new SelectionCacheEntry<TService>(result, ttl ?? _defaultTtl);
        _cache.AddOrUpdate(key, entry, (_, _) => entry);
    }

    /// <summary>
    /// Clears all cached entries (called on provider registration changes).
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Removes expired entries from the cache.
    /// </summary>
    public void CleanupExpired()
    {
        var keysToRemove = new List<string>();
        
        foreach (var kvp in _cache)
        {
            if (kvp.Value.IsExpired)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Generates a deterministic cache key from the selection context.
    /// </summary>
    private static string GenerateCacheKey(ISelectionContext<TService> context)
    {
        // Create a deterministic key based on:
        // 1. Service type
        // 2. Available provider IDs (sorted for consistency)
        // 3. Selection metadata/criteria

        var keyBuilder = new StringBuilder();
        keyBuilder.Append(typeof(TService).FullName);
        keyBuilder.Append('|');

        // Add sorted provider IDs for deterministic key
        var providerIds = context.Registrations
            .Select(r => r.Capabilities.ProviderId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        keyBuilder.AppendJoin(',', providerIds);
        keyBuilder.Append('|');

        // Add metadata if present (simple string representation to avoid JSON serialization issues)
        if (context.Metadata != null && context.Metadata.Count > 0)
        {
            var sortedMetadata = context.Metadata
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .Select(kvp => $"{kvp.Key}={kvp.Value}")
                .ToList();

            keyBuilder.AppendJoin(';', sortedMetadata);
        }

        // Generate SHA256 hash for consistent, bounded key length
        var keyString = keyBuilder.ToString();
        var keyBytes = Encoding.UTF8.GetBytes(keyString);
        var hashBytes = SHA256.HashData(keyBytes);
        
        return Convert.ToHexString(hashBytes);
    }
}
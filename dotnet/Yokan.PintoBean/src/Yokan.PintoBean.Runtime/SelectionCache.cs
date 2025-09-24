// Tier-3: TTL-based selection cache for provider selection strategies

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

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
/// TTL-based cache for selection strategy results with timer-based eviction.
/// Caches results by (contract, criteria) combination and invalidates on provider changes.
/// </summary>
public sealed class SelectionCache<TService> : IProviderSelectionCache<TService>, IDisposable
    where TService : class
{
    private readonly ConcurrentDictionary<string, SelectionCacheEntry<TService>> _cache = new();
    private readonly TimeSpan _defaultTtl;
    private readonly Timer _evictionTimer;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of the SelectionCache class.
    /// </summary>
    /// <param name="defaultTtl">Optional default TTL for cache entries. Defaults to 5 minutes if not specified.</param>
    public SelectionCache(TimeSpan? defaultTtl = null)
    {
        _defaultTtl = defaultTtl ?? TimeSpan.FromMinutes(5); // Default 5-minute TTL
        
        // Create timer for periodic cleanup (every 30 seconds)
        _evictionTimer = new Timer(CleanupExpiredCallback, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    /// <inheritdoc />
    public TimeSpan DefaultTtl => _defaultTtl;

    /// <inheritdoc />
    public int Count => _cache.Count;

    /// <inheritdoc />
    public ISelectionResult<TService>? TryGet(ISelectionContext<TService> context)
    {
        ThrowIfDisposed();
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

    /// <inheritdoc />
    public ISelectionResult<TService> Get(ISelectionContext<TService> context)
    {
        var result = TryGet(context);
        if (result == null)
        {
            throw new InvalidOperationException("Selection result not found in cache or has expired.");
        }
        return result;
    }

    /// <inheritdoc />
    public void Set(ISelectionContext<TService> context, ISelectionResult<TService> result, TimeSpan? ttl = null)
    {
        ThrowIfDisposed();
        var key = GenerateCacheKey(context);
        var entry = new SelectionCacheEntry<TService>(result, ttl ?? _defaultTtl);
        _cache.AddOrUpdate(key, entry, (_, _) => entry);
    }

    /// <inheritdoc />
    public bool Remove(ISelectionContext<TService> context)
    {
        ThrowIfDisposed();
        var key = GenerateCacheKey(context);
        return _cache.TryRemove(key, out _);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Timer callback for periodic cleanup of expired entries.
    /// </summary>
    private void CleanupExpiredCallback(object? state)
    {
        if (!_disposed)
        {
            CleanupExpired();
        }
    }

    /// <summary>
    /// Removes expired entries from the cache.
    /// </summary>
    public void CleanupExpired()
    {
        if (_disposed)
            return;

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
    /// Throws if the cache has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SelectionCache<TService>));
        }
    }

    /// <summary>
    /// Disposes the cache and its timer.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _evictionTimer?.Dispose();
            _cache.Clear();
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

/// <summary>
/// Hash and equality helper methods for SelectionContext objects.
/// </summary>
public static class SelectionContextHashHelper
{
    /// <summary>
    /// Generates a deterministic hash code for a selection context.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <param name="context">The selection context to hash.</param>
    /// <returns>A hash code representing the context.</returns>
    public static int GetHashCode<TService>(ISelectionContext<TService> context)
        where TService : class
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var hash = new HashCode();
        
        // Add service type
        hash.Add(typeof(TService));
        
        // Add sorted provider IDs for deterministic hash
        var providerIds = context.Registrations
            .Select(r => r.Capabilities.ProviderId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        
        foreach (var providerId in providerIds)
        {
            hash.Add(providerId);
        }
        
        // Add sorted metadata keys and values
        if (context.Metadata != null && context.Metadata.Count > 0)
        {
            var sortedMetadata = context.Metadata
                .OrderBy(kvp => kvp.Key, StringComparer.Ordinal)
                .ToList();
                
            foreach (var kvp in sortedMetadata)
            {
                hash.Add(kvp.Key);
                hash.Add(kvp.Value);
            }
        }
        
        return hash.ToHashCode();
    }

    /// <summary>
    /// Determines if two selection contexts are equivalent for caching purposes.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <param name="left">First context to compare.</param>
    /// <param name="right">Second context to compare.</param>
    /// <returns>True if the contexts are equivalent for caching, false otherwise.</returns>
    public static bool AreEquivalent<TService>(ISelectionContext<TService> left, ISelectionContext<TService> right)
        where TService : class
    {
        if (ReferenceEquals(left, right))
            return true;
            
        if (left == null || right == null)
            return false;
            
        // Check service type
        if (left.ServiceType != right.ServiceType)
            return false;
            
        // Check provider registrations (order-independent)
        var leftProviderIds = left.Registrations
            .Select(r => r.Capabilities.ProviderId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
            
        var rightProviderIds = right.Registrations
            .Select(r => r.Capabilities.ProviderId)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
            
        if (!leftProviderIds.SequenceEqual(rightProviderIds))
            return false;
            
        // Check metadata (order-independent)
        var leftMetadata = left.Metadata ?? new Dictionary<string, object>();
        var rightMetadata = right.Metadata ?? new Dictionary<string, object>();
        
        if (leftMetadata.Count != rightMetadata.Count)
            return false;
            
        foreach (var kvp in leftMetadata)
        {
            if (!rightMetadata.TryGetValue(kvp.Key, out var rightValue) || 
                !Equals(kvp.Value, rightValue))
            {
                return false;
            }
        }
        
        return true;
    }
}
// Tier-3: Default selection strategy implementations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Default PickOne selection strategy that selects a single provider based on filtering and priority.
/// Implements capability filter → platform filter → priority → deterministic tie-break with TTL cache.
/// </summary>
/// <typeparam name="TService">The service contract type.</typeparam>
public sealed class PickOneSelectionStrategy<TService> : ISelectionStrategy<TService>, ISelectionStrategy, IDisposable
    where TService : class
{
    private readonly SelectionCache<TService> _cache;
    private readonly IServiceRegistry? _registry;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the PickOneSelectionStrategy class.
    /// </summary>
    /// <param name="registry">Optional service registry for cache invalidation on provider changes.</param>
    /// <param name="cacheTtl">Optional TTL for cached selections. Defaults to 5 minutes.</param>
    public PickOneSelectionStrategy(IServiceRegistry? registry = null, TimeSpan? cacheTtl = null)
    {
        _cache = new SelectionCache<TService>(cacheTtl);
        _registry = registry;

        // Subscribe to provider changes for cache invalidation
        if (_registry != null)
        {
            _registry.ProviderChanged += OnProviderChanged;
        }
    }

    /// <inheritdoc />
    public SelectionStrategyType StrategyType => SelectionStrategyType.PickOne;

    /// <inheritdoc />
    public Type ServiceType => typeof(TService);

    /// <inheritdoc />
    public ISelectionResult<TService> SelectProviders(ISelectionContext<TService> context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (context.Registrations.Count == 0)
        {
            throw new InvalidOperationException(
                $"No providers registered for service contract '{typeof(TService).Name}'.");
        }

        // Try cache first
        var cachedResult = _cache.TryGet(context);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        // Apply filtering and selection logic
        var selected = SelectProviderInternal(context);
        var selectedProvider = (TService)selected.Provider;

        var result = SelectionResult<TService>.Single(
            selectedProvider,
            SelectionStrategyType.PickOne,
            new Dictionary<string, object>
            {
                ["Priority"] = selected.Capabilities.Priority,
                ["RegisteredAt"] = selected.Capabilities.RegisteredAt,
                ["ProviderId"] = selected.Capabilities.ProviderId,
                ["Platform"] = selected.Capabilities.Platform,
                ["Tags"] = selected.Capabilities.Tags.ToList(),
                ["SelectionMethod"] = "Enhanced PickOne with cache"
            });

        // Cache the result
        _cache.Set(context, result);

        return result;
    }

    /// <inheritdoc />
    public bool CanHandle(ISelectionContext<TService> context)
    {
        // PickOne can handle any context with at least one registration
        return context?.Registrations?.Count > 0;
    }

    /// <summary>
    /// Internal selection logic with filtering and deterministic tie-break.
    /// </summary>
    private static IProviderRegistration SelectProviderInternal(ISelectionContext<TService> context)
    {
        var candidates = context.Registrations.AsEnumerable();

        // Step 1: Capability filter (filter by tags if specified in metadata)
        candidates = ApplyCapabilityFilter(candidates, context.Metadata);

        // Step 2: Platform filter (ensure platform compatibility)
        candidates = ApplyPlatformFilter(candidates);

        var candidateList = candidates.ToList();
        if (candidateList.Count == 0)
        {
            throw new InvalidOperationException(
                $"No compatible providers found for service contract '{typeof(TService).Name}' after applying filters.");
        }

        // Step 3: Priority selection with deterministic tie-break
        return SelectByPriorityWithTieBreak(candidateList);
    }

    /// <summary>
    /// Applies capability filtering based on required tags in selection metadata.
    /// </summary>
    private static IEnumerable<IProviderRegistration> ApplyCapabilityFilter(
        IEnumerable<IProviderRegistration> candidates, 
        IDictionary<string, object>? metadata)
    {
        if (metadata == null || !metadata.TryGetValue("RequiredTags", out var tagsObj))
        {
            return candidates; // No capability filter specified
        }

        var requiredTags = tagsObj switch
        {
            string[] stringArray => stringArray,
            IEnumerable<string> enumerable => enumerable.ToArray(),
            string singleTag => new[] { singleTag },
            _ => Array.Empty<string>()
        };

        if (requiredTags.Length == 0)
        {
            return candidates;
        }

        return candidates.Where(r => r.Capabilities.HasTags(requiredTags));
    }

    /// <summary>
    /// Applies platform compatibility filtering.
    /// </summary>
    private static IEnumerable<IProviderRegistration> ApplyPlatformFilter(IEnumerable<IProviderRegistration> candidates)
    {
        return candidates.Where(r => PlatformDetector.IsCompatible(r.Capabilities.Platform));
    }

    /// <summary>
    /// Selects provider by priority with deterministic tie-break using stable hash.
    /// </summary>
    private static IProviderRegistration SelectByPriorityWithTieBreak(IList<IProviderRegistration> candidates)
    {
        // Group by priority, select highest priority group
        var highestPriority = candidates.Max(r => (int)r.Capabilities.Priority);
        var topPriorityCandidates = candidates
            .Where(r => (int)r.Capabilities.Priority == highestPriority)
            .ToList();

        if (topPriorityCandidates.Count == 1)
        {
            return topPriorityCandidates[0];
        }

        // Deterministic tie-break using stable hash of provider ID
        // This ensures the same provider is always selected for the same set of equal-priority providers
        return topPriorityCandidates
            .OrderBy(r => ComputeStableHash(r.Capabilities.ProviderId))
            .ThenBy(r => r.Capabilities.RegisteredAt) // Secondary sort for additional stability
            .First();
    }

    /// <summary>
    /// Computes a stable hash value for deterministic tie-breaking.
    /// </summary>
    private static int ComputeStableHash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return 0;

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToInt32(hashBytes, 0);
    }

    /// <summary>
    /// Handles provider change events for cache invalidation.
    /// </summary>
    private void OnProviderChanged(object? sender, ProviderChangedEventArgs e)
    {
        // Invalidate cache when providers change for this service type
        if (e.ServiceType == typeof(TService))
        {
            _cache.Clear();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            if (_registry != null)
            {
                _registry.ProviderChanged -= OnProviderChanged;
            }
            _disposed = true;
        }
    }
}

/// <summary>
/// Registry for default selection strategies provided by the runtime.
/// </summary>
public static class DefaultSelectionStrategies
{
    /// <summary>
    /// Creates a default PickOne strategy for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <param name="registry">Optional service registry for cache invalidation on provider changes.</param>
    /// <param name="cacheTtl">Optional TTL for cached selections. Defaults to 5 minutes.</param>
    /// <returns>A PickOne selection strategy instance.</returns>
    public static ISelectionStrategy<TService> CreatePickOne<TService>(
        IServiceRegistry? registry = null, 
        TimeSpan? cacheTtl = null)
        where TService : class
    {
        return new PickOneSelectionStrategy<TService>(registry, cacheTtl);
    }
}
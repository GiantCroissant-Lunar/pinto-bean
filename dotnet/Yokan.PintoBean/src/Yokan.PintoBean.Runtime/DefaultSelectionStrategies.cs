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
    private readonly IProviderSelectionCache<TService> _cache;
    private readonly IServiceRegistry? _registry;
    private readonly IAspectRuntime _aspectRuntime;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the PickOneSelectionStrategy class.
    /// </summary>
    /// <param name="registry">Optional service registry for cache invalidation on provider changes.</param>
    /// <param name="cacheTtl">Optional TTL for cached selections. Defaults to 5 minutes.</param>
    /// <param name="aspectRuntime">Optional aspect runtime for telemetry. Defaults to no-op.</param>
    public PickOneSelectionStrategy(IServiceRegistry? registry = null, TimeSpan? cacheTtl = null, IAspectRuntime? aspectRuntime = null)
    {
        _cache = new SelectionCache<TService>(cacheTtl);
        _registry = registry;
        _aspectRuntime = aspectRuntime ?? NoOpAspectRuntime.Instance;

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
            // Record cache hit
            _aspectRuntime.RecordMetric("strategy.pickone.cache.hit", 1, 
                ("service", typeof(TService).Name),
                ("strategy", "PickOne"));
            
            // Record chosen provider
            _aspectRuntime.RecordMetric("strategy.pickone.provider.selected", 1,
                ("service", typeof(TService).Name),
                ("provider", cachedResult.SelectionMetadata?.TryGetValue("ProviderId", out var pid) == true ? pid.ToString() ?? "unknown" : "unknown"),
                ("source", "cache"));
            
            return cachedResult;
        }

        // Record cache miss
        _aspectRuntime.RecordMetric("strategy.pickone.cache.miss", 1,
            ("service", typeof(TService).Name),
            ("strategy", "PickOne"));

        // Apply filtering and selection logic
        var selected = SelectProviderInternal(context);
        var selectedProvider = (TService)selected.Provider;

        // Record chosen provider
        _aspectRuntime.RecordMetric("strategy.pickone.provider.selected", 1,
            ("service", typeof(TService).Name),
            ("provider", selected.Capabilities.ProviderId),
            ("source", "selection"));

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

            // Dispose the cache if it implements IDisposable
            if (_cache is IDisposable disposableCache)
            {
                disposableCache.Dispose();
            }

            _disposed = true;
        }
    }
}

/// <summary>
/// Default FanOut selection strategy that invokes all matched providers and aggregates results/failures.
/// Supports fire-and-forget for void operations and aggregation for Task/ValueTask return types.
/// </summary>
/// <typeparam name="TService">The service contract type.</typeparam>
public sealed class FanOutSelectionStrategy<TService> : ISelectionStrategy<TService>, ISelectionStrategy, IDisposable
    where TService : class
{
    private readonly IServiceRegistry? _registry;
    private readonly IResilienceExecutor? _resilienceExecutor;
    private readonly IAspectRuntime _aspectRuntime;
    private readonly FanOutErrorPolicy _errorPolicy;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the FanOutSelectionStrategy class.
    /// </summary>
    /// <param name="registry">Optional service registry for provider change monitoring.</param>
    /// <param name="resilienceExecutor">Optional resilience executor for fault handling.</param>
    /// <param name="aspectRuntime">Optional aspect runtime for telemetry. Defaults to no-op.</param>
    /// <param name="errorPolicy">Error handling policy for aggregation. Defaults to Continue.</param>
    public FanOutSelectionStrategy(IServiceRegistry? registry = null, IResilienceExecutor? resilienceExecutor = null, IAspectRuntime? aspectRuntime = null, FanOutErrorPolicy errorPolicy = FanOutErrorPolicy.Continue)
    {
        _registry = registry;
        _resilienceExecutor = resilienceExecutor;
        _aspectRuntime = aspectRuntime ?? NoOpAspectRuntime.Instance;
        _errorPolicy = errorPolicy;
    }

    /// <inheritdoc />
    public SelectionStrategyType StrategyType => SelectionStrategyType.FanOut;

    /// <inheritdoc />
    public Type ServiceType => typeof(TService);

    /// <inheritdoc />
    public ISelectionResult<TService> SelectProviders(ISelectionContext<TService> context)
    {
        var startTime = DateTime.UtcNow;
        
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        if (context.Registrations.Count == 0)
        {
            throw new InvalidOperationException(
                $"No providers registered for service contract '{typeof(TService).Name}'.");
        }

        try
        {
            // Apply filtering to get all compatible providers
            var compatibleProviders = FilterCompatibleProviders(context);

            if (!compatibleProviders.Any())
            {
                // Record failure (no compatible providers)
                _aspectRuntime.RecordMetric("strategy.fanout.failures", 1,
                    ("service", typeof(TService).Name),
                    ("reason", "no_compatible_providers"));
                    
                throw new InvalidOperationException(
                    $"No compatible providers found for service contract '{typeof(TService).Name}' after applying filters.");
            }

            // Return all compatible providers for fan-out invocation
            var selectedProviders = compatibleProviders
                .Select(r => (TService)r.Provider)
                .ToList();

            // Record fanout size
            _aspectRuntime.RecordMetric("strategy.fanout.size", selectedProviders.Count,
                ("service", typeof(TService).Name),
                ("strategy", "FanOut"));

            // Record duration
            var duration = DateTime.UtcNow - startTime;
            _aspectRuntime.RecordMetric("strategy.fanout.duration", duration.TotalMilliseconds,
                ("service", typeof(TService).Name),
                ("strategy", "FanOut"));

            var selectionMetadata = new Dictionary<string, object>
            {
                ["ProviderCount"] = selectedProviders.Count,
                ["ProviderIds"] = compatibleProviders.Select(r => r.Capabilities.ProviderId).ToList(),
                ["SelectionMethod"] = "FanOut (all compatible providers)",
                ["StrategyType"] = "FanOut",
                ["ErrorPolicy"] = _errorPolicy.ToString()
            };

            return new SelectionResult<TService>(
                selectedProviders,
                SelectionStrategyType.FanOut,
                selectionMetadata);
        }
        catch (Exception)
        {
            // Record general failure
            _aspectRuntime.RecordMetric("strategy.fanout.failures", 1,
                ("service", typeof(TService).Name),
                ("reason", "selection_failed"));
            throw;
        }
    }

    /// <inheritdoc />
    public bool CanHandle(ISelectionContext<TService> context)
    {
        // FanOut can handle any context with at least one registration
        return context?.Registrations?.Count > 0;
    }

    /// <summary>
    /// Filters providers to get only those compatible with the current platform and capabilities.
    /// </summary>
    private static IEnumerable<IProviderRegistration> FilterCompatibleProviders(ISelectionContext<TService> context)
    {
        var candidates = context.Registrations.AsEnumerable();

        // Step 1: Apply capability filter (filter by tags if specified in metadata)
        candidates = ApplyCapabilityFilter(candidates, context.Metadata);

        // Step 2: Apply platform filter (ensure platform compatibility)
        candidates = ApplyPlatformFilter(candidates);

        // Step 3: Only return active providers
        return candidates.Where(r => r.IsActive);
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

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            // FanOut doesn't maintain internal state that needs cleanup
            // but we follow the pattern for consistency
            _disposed = true;
        }
    }
}

/// <summary>
/// Default Sharded selection strategy that routes requests by key using a key extraction function.
/// Commonly used for analytics with event name prefix routing.
/// </summary>
/// <typeparam name="TService">The service contract type.</typeparam>
public sealed class ShardedSelectionStrategy<TService> : ISelectionStrategy<TService>, ISelectionStrategy, IDisposable
    where TService : class
{
    private readonly IProviderSelectionCache<TService> _cache;
    private readonly IServiceRegistry? _registry;
    private readonly Func<IDictionary<string, object>?, string> _keyExtractor;
    private readonly IReadOnlyDictionary<string, string>? _explicitShardMap;
    private readonly IAspectRuntime _aspectRuntime;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the ShardedSelectionStrategy class.
    /// </summary>
    /// <param name="keyExtractor">Function to extract shard key from selection metadata.</param>
    /// <param name="registry">Optional service registry for cache invalidation on provider changes.</param>
    /// <param name="cacheTtl">Optional TTL for cached selections. Defaults to 5 minutes.</param>
    /// <param name="aspectRuntime">Optional aspect runtime for telemetry. Defaults to no-op.</param>
    public ShardedSelectionStrategy(
        Func<IDictionary<string, object>?, string> keyExtractor,
        IServiceRegistry? registry = null,
        TimeSpan? cacheTtl = null,
        IAspectRuntime? aspectRuntime = null)
        : this(keyExtractor, explicitShardMap: null, registry, cacheTtl, aspectRuntime)
    {
    }

    /// <summary>
    /// Initializes a new instance of the ShardedSelectionStrategy class with explicit shard mapping.
    /// </summary>
    /// <param name="keyExtractor">Function to extract shard key from selection metadata.</param>
    /// <param name="explicitShardMap">Optional explicit mapping from shard keys to provider IDs. When provided, takes precedence over consistent hashing.</param>
    /// <param name="registry">Optional service registry for cache invalidation on provider changes.</param>
    /// <param name="cacheTtl">Optional TTL for cached selections. Defaults to 5 minutes.</param>
    /// <param name="aspectRuntime">Optional aspect runtime for telemetry. Defaults to no-op.</param>
    public ShardedSelectionStrategy(
        Func<IDictionary<string, object>?, string> keyExtractor,
        IReadOnlyDictionary<string, string>? explicitShardMap,
        IServiceRegistry? registry = null,
        TimeSpan? cacheTtl = null,
        IAspectRuntime? aspectRuntime = null)
    {
        _keyExtractor = keyExtractor ?? throw new ArgumentNullException(nameof(keyExtractor));
        _explicitShardMap = explicitShardMap;
        _cache = new SelectionCache<TService>(cacheTtl);
        _registry = registry;
        _aspectRuntime = aspectRuntime ?? NoOpAspectRuntime.Instance;

        // Subscribe to provider changes for cache invalidation
        if (_registry != null)
        {
            _registry.ProviderChanged += OnProviderChanged;
        }
    }

    /// <inheritdoc />
    public SelectionStrategyType StrategyType => SelectionStrategyType.Sharded;

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

        // Extract shard key
        var shardKey = _keyExtractor(context.Metadata);
        if (string.IsNullOrEmpty(shardKey))
        {
            throw new InvalidOperationException("Shard key extraction returned null or empty string.");
        }

        // Record shard key
        _aspectRuntime.RecordMetric("strategy.sharded.shard_key", 1,
            ("service", typeof(TService).Name),
            ("shard_key", shardKey),
            ("strategy", "Sharded"));

        // Try cache first (including shard key in cache context)
        var cachedResult = _cache.TryGet(context);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        // Apply filtering and shard-based selection
        var selected = SelectProviderByShard(context, shardKey, _explicitShardMap);
        var selectedProvider = (TService)selected.Provider;

        // Record target provider
        _aspectRuntime.RecordMetric("strategy.sharded.target", 1,
            ("service", typeof(TService).Name),
            ("shard_key", shardKey),
            ("target_provider", selected.Capabilities.ProviderId));

        var result = SelectionResult<TService>.Single(
            selectedProvider,
            SelectionStrategyType.Sharded,
            new Dictionary<string, object>
            {
                ["ShardKey"] = shardKey,
                ["Priority"] = selected.Capabilities.Priority,
                ["RegisteredAt"] = selected.Capabilities.RegisteredAt,
                ["ProviderId"] = selected.Capabilities.ProviderId,
                ["Platform"] = selected.Capabilities.Platform,
                ["Tags"] = selected.Capabilities.Tags.ToList(),
                ["SelectionMethod"] = "Sharded routing"
            });

        // Cache the result
        _cache.Set(context, result);

        return result;
    }

    /// <inheritdoc />
    public bool CanHandle(ISelectionContext<TService> context)
    {
        // Sharded strategy can handle any context with at least one registration
        // and where key extraction doesn't fail
        if (context?.Registrations?.Count == 0)
            return false;

        try
        {
            var shardKey = _keyExtractor(context?.Metadata);
            return !string.IsNullOrEmpty(shardKey);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Selects a provider based on shard key routing with explicit mapping support and consistent hashing fallback.
    /// </summary>
    private static IProviderRegistration SelectProviderByShard(ISelectionContext<TService> context, string shardKey, IReadOnlyDictionary<string, string>? explicitShardMap)
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

        // Step 3: Try explicit shard mapping first, then fallback to consistent hashing
        return SelectByShardKey(candidateList, shardKey, explicitShardMap);
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
    /// Selects provider using explicit shard mapping first, then falls back to consistent hashing.
    /// </summary>
    private static IProviderRegistration SelectByShardKey(IList<IProviderRegistration> candidates, string shardKey, IReadOnlyDictionary<string, string>? explicitShardMap)
    {
        // First, try explicit shard mapping if available
        if (explicitShardMap != null && explicitShardMap.TryGetValue(shardKey, out var targetProviderId))
        {
            // Find the provider with the matching ProviderId
            var explicitProvider = candidates.FirstOrDefault(r => r.Capabilities.ProviderId == targetProviderId);
            if (explicitProvider != null)
            {
                return explicitProvider;
            }
            
            // If explicit provider not found in candidates, fall through to consistent hashing
            // This could happen if the provider is not available/compatible
        }

        // Fallback to consistent hashing when no explicit mapping or provider not found
        return SelectByConsistentHashing(candidates, shardKey);
    }

    /// <summary>
    /// Selects provider using consistent hashing based on shard key.
    /// </summary>
    private static IProviderRegistration SelectByConsistentHashing(IList<IProviderRegistration> candidates, string shardKey)
    {
        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        // Sort providers by provider ID for consistent ordering
        var sortedCandidates = candidates
            .OrderBy(r => r.Capabilities.ProviderId, StringComparer.Ordinal)
            .ToList();

        // Use consistent hashing: hash(shardKey + providerId) to determine best match
        var bestMatch = sortedCandidates[0];
        var bestHash = ComputeConsistentHash(shardKey, bestMatch.Capabilities.ProviderId);

        foreach (var candidate in sortedCandidates.Skip(1))
        {
            var candidateHash = ComputeConsistentHash(shardKey, candidate.Capabilities.ProviderId);
            if (candidateHash > bestHash)
            {
                bestHash = candidateHash;
                bestMatch = candidate;
            }
        }

        return bestMatch;
    }

    /// <summary>
    /// Computes a consistent hash for shard key and provider ID combination.
    /// </summary>
    private static uint ComputeConsistentHash(string shardKey, string providerId)
    {
        var combined = $"{shardKey}:{providerId}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return BitConverter.ToUInt32(hashBytes, 0);
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

            // Dispose the cache if it implements IDisposable
            if (_cache is IDisposable disposableCache)
            {
                disposableCache.Dispose();
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
    /// <param name="aspectRuntime">Optional aspect runtime for telemetry. Defaults to no-op.</param>
    /// <returns>A PickOne selection strategy instance.</returns>
    public static ISelectionStrategy<TService> CreatePickOne<TService>(
        IServiceRegistry? registry = null,
        TimeSpan? cacheTtl = null,
        IAspectRuntime? aspectRuntime = null)
        where TService : class
    {
        return new PickOneSelectionStrategy<TService>(registry, cacheTtl, aspectRuntime);
    }

    /// <summary>
    /// Creates a default FanOut strategy for the specified service type.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <param name="registry">Optional service registry for provider change monitoring.</param>
    /// <param name="resilienceExecutor">Optional resilience executor for fault handling.</param>
    /// <param name="aspectRuntime">Optional aspect runtime for telemetry. Defaults to no-op.</param>
    /// <param name="errorPolicy">Error handling policy for aggregation. Defaults to Continue.</param>
    /// <returns>A FanOut selection strategy instance.</returns>
    public static ISelectionStrategy<TService> CreateFanOut<TService>(
        IServiceRegistry? registry = null,
        IResilienceExecutor? resilienceExecutor = null,
        IAspectRuntime? aspectRuntime = null,
        FanOutErrorPolicy errorPolicy = FanOutErrorPolicy.Continue)
        where TService : class
    {
        return new FanOutSelectionStrategy<TService>(registry, resilienceExecutor, aspectRuntime, errorPolicy);
    }
    /// <summary>
    /// Creates a default Sharded strategy for the specified service type with analytics shard key extraction.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <param name="registry">Optional service registry for cache invalidation on provider changes.</param>
    /// <param name="cacheTtl">Optional TTL for cached selections. Defaults to 5 minutes.</param>
    /// <param name="aspectRuntime">Optional aspect runtime for telemetry. Defaults to no-op.</param>
    /// <returns>A Sharded selection strategy instance with analytics key extraction.</returns>
    public static ISelectionStrategy<TService> CreateAnalyticsSharded<TService>(
        IServiceRegistry? registry = null,
        TimeSpan? cacheTtl = null,
        IAspectRuntime? aspectRuntime = null)
        where TService : class
    {
        return new ShardedSelectionStrategy<TService>(
            ExtractAnalyticsShardKey,
            registry,
            cacheTtl,
            aspectRuntime);
    }

    /// <summary>
    /// Creates a Sharded strategy for the specified service type with custom key extraction.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <param name="keyExtractor">Function to extract shard key from selection metadata.</param>
    /// <param name="registry">Optional service registry for cache invalidation on provider changes.</param>
    /// <param name="cacheTtl">Optional TTL for cached selections. Defaults to 5 minutes.</param>
    /// <param name="aspectRuntime">Optional aspect runtime for telemetry. Defaults to no-op.</param>
    /// <returns>A Sharded selection strategy instance with custom key extraction.</returns>
    public static ISelectionStrategy<TService> CreateSharded<TService>(
        Func<IDictionary<string, object>?, string> keyExtractor,
        IServiceRegistry? registry = null,
        TimeSpan? cacheTtl = null,
        IAspectRuntime? aspectRuntime = null)
        where TService : class
    {
        return new ShardedSelectionStrategy<TService>(keyExtractor, registry, cacheTtl, aspectRuntime);
    }

    /// <summary>
    /// Creates a Sharded strategy for analytics with explicit shard mapping support.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <param name="explicitShardMap">Explicit mapping from shard keys to provider IDs. Takes precedence over consistent hashing.</param>
    /// <param name="registry">Optional service registry for cache invalidation on provider changes.</param>
    /// <param name="cacheTtl">Optional TTL for cached selections. Defaults to 5 minutes.</param>
    /// <param name="aspectRuntime">Optional aspect runtime for telemetry. Defaults to no-op.</param>
    /// <returns>A Sharded selection strategy instance with explicit mapping and analytics key extraction.</returns>
    public static ISelectionStrategy<TService> CreateAnalyticsShardedWithMap<TService>(
        IReadOnlyDictionary<string, string> explicitShardMap,
        IServiceRegistry? registry = null,
        TimeSpan? cacheTtl = null,
        IAspectRuntime? aspectRuntime = null)
        where TService : class
    {
        return new ShardedSelectionStrategy<TService>(
            ExtractAnalyticsShardKey,
            explicitShardMap,
            registry,
            cacheTtl,
            aspectRuntime);
    }

    /// <summary>
    /// Creates a Sharded strategy with custom key extraction and explicit shard mapping support.
    /// </summary>
    /// <typeparam name="TService">The service contract type.</typeparam>
    /// <param name="keyExtractor">Function to extract shard key from selection metadata.</param>
    /// <param name="explicitShardMap">Explicit mapping from shard keys to provider IDs. Takes precedence over consistent hashing.</param>
    /// <param name="registry">Optional service registry for cache invalidation on provider changes.</param>
    /// <param name="cacheTtl">Optional TTL for cached selections. Defaults to 5 minutes.</param>
    /// <param name="aspectRuntime">Optional aspect runtime for telemetry. Defaults to no-op.</param>
    /// <returns>A Sharded selection strategy instance with custom key extraction and explicit mapping.</returns>
    public static ISelectionStrategy<TService> CreateShardedWithMap<TService>(
        Func<IDictionary<string, object>?, string> keyExtractor,
        IReadOnlyDictionary<string, string> explicitShardMap,
        IServiceRegistry? registry = null,
        TimeSpan? cacheTtl = null,
        IAspectRuntime? aspectRuntime = null)
        where TService : class
    {
        return new ShardedSelectionStrategy<TService>(keyExtractor, explicitShardMap, registry, cacheTtl, aspectRuntime);
    }

    /// <summary>
    /// Default analytics shard key extractor that extracts event name prefix before the first dot.
    /// Example: "player.level.complete" → "player"
    /// </summary>
    /// <param name="metadata">Selection metadata containing the event name or shard key.</param>
    /// <returns>The extracted shard key.</returns>
    public static string ExtractAnalyticsShardKey(IDictionary<string, object>? metadata)
    {
        if (metadata == null)
        {
            throw new ArgumentException("Analytics sharding requires metadata with EventName or ShardKey.");
        }

        // First, check for explicit shard key
        if (metadata.TryGetValue("ShardKey", out var explicitKey) && explicitKey is string shardKey && !string.IsNullOrEmpty(shardKey))
        {
            return shardKey;
        }

        // Fall back to extracting from event name
        if (metadata.TryGetValue("EventName", out var eventNameObj) && eventNameObj is string eventName && !string.IsNullOrEmpty(eventName))
        {
            var dotIndex = eventName.IndexOf('.');
            return dotIndex > 0 ? eventName.Substring(0, dotIndex) : eventName;
        }

        throw new ArgumentException("Analytics sharding requires metadata with EventName or ShardKey.");
    }
}

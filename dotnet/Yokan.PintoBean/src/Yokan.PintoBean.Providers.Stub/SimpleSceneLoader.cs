using System;
using System.Threading;
using System.Threading.Tasks;
using Yokan.PintoBean.Abstractions;

namespace Yokan.PintoBean.Providers.Stub;

/// <summary>
/// Tier-4: Simple scene loader provider with policy-driven behavior.
/// Demonstrates deterministic scene transition logic with configurable logging policies.
/// </summary>
public class SimpleSceneLoader : ISceneFlow
{
    private readonly string _providerId;
    private readonly SceneLoaderPolicy _policy;
    private readonly Random _loadTimeGenerator;

    /// <summary>
    /// Initializes a new instance of the SimpleSceneLoader class.
    /// </summary>
    /// <param name="providerId">Unique identifier for this provider instance.</param>
    /// <param name="policy">Policy configuration for scene loading behavior.</param>
    public SimpleSceneLoader(string providerId, SceneLoaderPolicy policy)
    {
        _providerId = providerId ?? throw new ArgumentNullException(nameof(providerId));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _loadTimeGenerator = new Random(providerId.GetHashCode()); // Deterministic seed
    }

    /// <inheritdoc />
    public async Task LoadAsync(string scene, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scene))
            throw new ArgumentException("Scene name cannot be null or empty.", nameof(scene));

        cancellationToken.ThrowIfCancellationRequested();

        // Policy-driven pre-load logging
        if (_policy.LogOrder == LogOrder.Pre || _policy.LogOrder == LogOrder.Both)
        {
            Console.WriteLine($"🎬 [{_providerId}] PRE-LOAD: Preparing to load scene '{scene}' with policy '{_policy.Name}'");
        }

        // Simulate deterministic load time based on scene name and provider
        var loadTimeMs = CalculateLoadTime(scene);
        await Task.Delay(loadTimeMs, cancellationToken);

        // Policy-driven post-load logging
        if (_policy.LogOrder == LogOrder.Post || _policy.LogOrder == LogOrder.Both)
        {
            Console.WriteLine($"✅ [{_providerId}] POST-LOAD: Successfully loaded scene '{scene}' in {loadTimeMs}ms (Policy: {_policy.Name})");
        }

        // Policy-driven additional metadata logging
        if (_policy.IncludeMetadata)
        {
            Console.WriteLine($"📊 [{_providerId}] METADATA: Scene='{scene}', LoadTime={loadTimeMs}ms, Provider={_providerId}, Policy={_policy.Name}");
        }
    }

    /// <summary>
    /// Calculates a deterministic load time based on scene name and provider ID.
    /// This ensures consistent behavior for the same inputs across runs.
    /// </summary>
    private int CalculateLoadTime(string scene)
    {
        // Create deterministic "load time" based on scene name and provider
        var sceneHash = scene.GetHashCode();
        var providerHash = _providerId.GetHashCode();
        var combinedSeed = sceneHash ^ providerHash;
        
        // Use deterministic random based on combined seed
        var random = new Random(Math.Abs(combinedSeed));
        return _policy.BaseLoadTimeMs + random.Next(0, _policy.LoadTimeVarianceMs);
    }
}

/// <summary>
/// Configuration policy for scene loader behavior.
/// Enables policy-driven logging and timing behavior for demonstration purposes.
/// </summary>
public class SceneLoaderPolicy
{
    /// <summary>
    /// Gets or sets the name of this policy.
    /// </summary>
    public string Name { get; set; } = "Default";

    /// <summary>
    /// Gets or sets when to log scene loading operations.
    /// </summary>
    public LogOrder LogOrder { get; set; } = LogOrder.Both;

    /// <summary>
    /// Gets or sets whether to include additional metadata in logs.
    /// </summary>
    public bool IncludeMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets the base load time in milliseconds.
    /// </summary>
    public int BaseLoadTimeMs { get; set; } = 100;

    /// <summary>
    /// Gets or sets the variance range for load time simulation.
    /// </summary>
    public int LoadTimeVarianceMs { get; set; } = 50;

    /// <summary>
    /// Creates a policy optimized for development/debugging.
    /// </summary>
    public static SceneLoaderPolicy Development => new()
    {
        Name = "Development",
        LogOrder = LogOrder.Both,
        IncludeMetadata = true,
        BaseLoadTimeMs = 50,
        LoadTimeVarianceMs = 25
    };

    /// <summary>
    /// Creates a policy optimized for production with minimal logging.
    /// </summary>
    public static SceneLoaderPolicy Production => new()
    {
        Name = "Production",
        LogOrder = LogOrder.Post,
        IncludeMetadata = false,
        BaseLoadTimeMs = 200,
        LoadTimeVarianceMs = 100
    };

    /// <summary>
    /// Creates a policy for performance testing with no logging.
    /// </summary>
    public static SceneLoaderPolicy Performance => new()
    {
        Name = "Performance",
        LogOrder = LogOrder.None,
        IncludeMetadata = false,
        BaseLoadTimeMs = 10,
        LoadTimeVarianceMs = 5
    };
}

/// <summary>
/// Defines when to log scene loading operations.
/// </summary>
public enum LogOrder
{
    /// <summary>
    /// No logging.
    /// </summary>
    None,

    /// <summary>
    /// Log before scene loading begins.
    /// </summary>
    Pre,

    /// <summary>
    /// Log after scene loading completes.
    /// </summary>
    Post,

    /// <summary>
    /// Log both before and after scene loading.
    /// </summary>
    Both
}
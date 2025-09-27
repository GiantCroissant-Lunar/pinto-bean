// Tier-3: Intelligent router provider for AI text services with QoS-based selection

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Yokan.PintoBean.Abstractions;

namespace Yokan.PintoBean.Runtime.AI;

/// <summary>
/// Provider that selects a backend AI text service based on QoS metrics (latency/cost) and constraints.
/// Implements the IAIText interface and routes calls to the most appropriate backend provider
/// registered in the service registry based on RouterOptions policy.
/// </summary>
public sealed class IntelligentRouter : IAIText
{
    private readonly IServiceRegistry _serviceRegistry;
    private readonly RouterOptions _options;

    /// <summary>
    /// Initializes a new instance of the IntelligentRouter class.
    /// </summary>
    /// <param name="serviceRegistry">The service registry to query providers from.</param>
    /// <param name="options">The routing policy configuration.</param>
    public IntelligentRouter(IServiceRegistry serviceRegistry, RouterOptions? options = null)
    {
        _serviceRegistry = serviceRegistry ?? throw new ArgumentNullException(nameof(serviceRegistry));
        _options = options ?? RouterOptions.Default;
    }

    /// <inheritdoc />
    public async Task<AITextResponse> GenerateTextAsync(AITextRequest request, CancellationToken cancellationToken = default)
    {
        var selectedProvider = SelectBestProvider(request);
        return await selectedProvider.GenerateTextAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AITextResponse> GenerateTextStreamAsync(AITextRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var selectedProvider = SelectBestProvider(request);
        await foreach (var response in selectedProvider.GenerateTextStreamAsync(request, cancellationToken))
        {
            yield return response;
        }
    }

    /// <inheritdoc />
    public async Task<AITextResponse> ContinueConversationAsync(AITextRequest request, CancellationToken cancellationToken = default)
    {
        var selectedProvider = SelectBestProvider(request);
        return await selectedProvider.ContinueConversationAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AITextResponse> ContinueConversationStreamAsync(AITextRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var selectedProvider = SelectBestProvider(request);
        await foreach (var response in selectedProvider.ContinueConversationStreamAsync(request, cancellationToken))
        {
            yield return response;
        }
    }

    /// <inheritdoc />
    public async Task<AITextResponse> CompleteTextAsync(AITextRequest request, CancellationToken cancellationToken = default)
    {
        var selectedProvider = SelectBestProvider(request);
        return await selectedProvider.CompleteTextAsync(request, cancellationToken);
    }

    /// <summary>
    /// Selects the best provider based on the router options and provider capabilities.
    /// </summary>
    /// <param name="request">The AI text request for context.</param>
    /// <returns>The selected AI text provider.</returns>
    private IAIText SelectBestProvider(AITextRequest request)
    {
        var registrations = _serviceRegistry.GetRegistrations<IAIText>().ToList();
        
        if (registrations.Count == 0)
        {
            throw new InvalidOperationException("No AI text providers are registered in the service registry.");
        }

        // Step 1: Apply constraints and filters
        var candidates = ApplyConstraints(registrations);
        
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("No AI text providers match the specified router constraints.");
        }

        // Step 2: Select best provider based on QoS preferences
        var bestProvider = SelectByQoSPreferences(candidates);
        
        return (IAIText)bestProvider.Provider;
    }

    /// <summary>
    /// Applies the router constraints to filter eligible providers.
    /// </summary>
    /// <param name="registrations">All available provider registrations.</param>
    /// <returns>Filtered list of eligible providers.</returns>
    private List<IProviderRegistration> ApplyConstraints(List<IProviderRegistration> registrations)
    {
        var candidates = registrations.Where(r => r.IsActive).ToList();

        // Apply AllowExternal constraint
        if (!_options.AllowExternal)
        {
            candidates = candidates.Where(r => 
                !r.Capabilities.Tags.Contains("external") && 
                !r.Capabilities.Tags.Contains("third-party")).ToList();
        }

        // Apply Region constraint
        if (!string.IsNullOrEmpty(_options.Region))
        {
            candidates = candidates.Where(r => 
                r.Capabilities.Metadata.TryGetValue("region", out var region) &&
                string.Equals(region?.ToString(), _options.Region, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Apply Budget constraint
        if (_options.BudgetPer1KTokens.HasValue)
        {
            candidates = candidates.Where(r =>
            {
                if (r.Capabilities.Metadata.TryGetValue("costPer1KTokens", out var cost) &&
                    decimal.TryParse(cost?.ToString(), out var costValue))
                {
                    return costValue <= _options.BudgetPer1KTokens.Value;
                }
                // If no cost information, assume it meets budget (conservative approach)
                return true;
            }).ToList();
        }

        return candidates;
    }

    /// <summary>
    /// Selects the provider that best matches QoS preferences.
    /// </summary>
    /// <param name="candidates">List of eligible providers.</param>
    /// <returns>The best provider based on QoS metrics.</returns>
    private IProviderRegistration SelectByQoSPreferences(List<IProviderRegistration> candidates)
    {
        // If only one candidate, return it
        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        // Priority 1: If budget is specified, prefer cheapest
        if (_options.BudgetPer1KTokens.HasValue)
        {
            var cheapest = candidates
                .Where(r => r.Capabilities.Metadata.TryGetValue("costPer1KTokens", out var cost) &&
                           decimal.TryParse(cost?.ToString(), out _))
                .OrderBy(r => decimal.Parse(r.Capabilities.Metadata["costPer1KTokens"].ToString()!))
                .FirstOrDefault();
                
            if (cheapest != null)
            {
                return cheapest;
            }
        }

        // Priority 2: If latency target is specified, prefer lowest latency
        if (_options.TargetLatencyMs.HasValue)
        {
            var fastest = candidates
                .Where(r => r.Capabilities.Metadata.TryGetValue("avgLatencyMs", out var latency) &&
                           int.TryParse(latency?.ToString(), out _))
                .OrderBy(r => int.Parse(r.Capabilities.Metadata["avgLatencyMs"].ToString()!))
                .FirstOrDefault();
                
            if (fastest != null)
            {
                return fastest;
            }
        }

        // Fallback: Select by priority, then by registration order (deterministic)
        return candidates
            .OrderByDescending(r => r.Capabilities.Priority)
            .ThenBy(r => r.Capabilities.RegisteredAt)
            .First();
    }
}
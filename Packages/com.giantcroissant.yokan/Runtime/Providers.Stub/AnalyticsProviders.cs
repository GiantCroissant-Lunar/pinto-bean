// Tier-4: Analytics provider stubs for Yokan PintoBean service platform

using System;
using System.Threading;
using System.Threading.Tasks;
using Yokan.PintoBean.Abstractions;

namespace Yokan.PintoBean.Providers.Stub;

/// <summary>
/// Stub analytics provider simulating Unity Analytics integration.
/// Provides distinct logging for strategy testing and demonstration.
/// </summary>
public class UnityAnalyticsProvider : IAnalytics
{
    /// <summary>
    /// Gets the unique identifier for this provider instance.
    /// </summary>
    public string ProviderId { get; }

    /// <summary>
    /// Initializes a new instance of the UnityAnalyticsProvider class.
    /// </summary>
    /// <param name="providerId">Optional provider identifier. Defaults to "unity-analytics".</param>
    public UnityAnalyticsProvider(string? providerId = null)
    {
        ProviderId = providerId ?? "unity-analytics";
    }

    /// <summary>
    /// Tracks an analytics event with Unity Analytics-style logging.
    /// </summary>
    /// <param name="analyticsEvent">The analytics event to track.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous tracking operation.</returns>
    public Task Track(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default)
    {
        // Simulate Unity Analytics tracking with distinct logging
        var properties = analyticsEvent.Properties != null && analyticsEvent.Properties.Count > 0
            ? $" with {analyticsEvent.Properties.Count} properties"
            : "";

        Console.WriteLine($"[Unity Analytics] Tracking event '{analyticsEvent.EventName}'{properties}" +
                         (analyticsEvent.UserId != null ? $" for user {analyticsEvent.UserId}" : "") +
                         (analyticsEvent.SessionId != null ? $" in session {analyticsEvent.SessionId}" : ""));

        // Simulate async work
        return Task.CompletedTask;
    }
}

/// <summary>
/// Stub analytics provider simulating Firebase Analytics integration.
/// Provides distinct logging for strategy testing and demonstration.
/// </summary>
public class FirebaseAnalyticsProvider : IAnalytics
{
    /// <summary>
    /// Gets the unique identifier for this provider instance.
    /// </summary>
    public string ProviderId { get; }

    /// <summary>
    /// Initializes a new instance of the FirebaseAnalyticsProvider class.
    /// </summary>
    /// <param name="providerId">Optional provider identifier. Defaults to "firebase-analytics".</param>
    public FirebaseAnalyticsProvider(string? providerId = null)
    {
        ProviderId = providerId ?? "firebase-analytics";
    }

    /// <summary>
    /// Tracks an analytics event with Firebase Analytics-style logging.
    /// </summary>
    /// <param name="analyticsEvent">The analytics event to track.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous tracking operation.</returns>
    public Task Track(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default)
    {
        // Simulate Firebase Analytics tracking with distinct logging
        var properties = analyticsEvent.Properties != null && analyticsEvent.Properties.Count > 0
            ? $" [{string.Join(", ", analyticsEvent.Properties.Keys)}]"
            : "";

        Console.WriteLine($"[Firebase Analytics] Event: {analyticsEvent.EventName}{properties}" +
                         (analyticsEvent.UserId != null ? $" | User: {analyticsEvent.UserId}" : "") +
                         (analyticsEvent.SessionId != null ? $" | Session: {analyticsEvent.SessionId}" : "") +
                         $" | Timestamp: {analyticsEvent.Timestamp:yyyy-MM-dd HH:mm:ss}");

        // Simulate async work
        return Task.CompletedTask;
    }
}
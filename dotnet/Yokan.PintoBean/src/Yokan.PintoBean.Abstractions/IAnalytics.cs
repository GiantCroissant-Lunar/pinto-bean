// Tier-1: Analytics service contract for Yokan PintoBean service platform

using System.Threading;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Abstractions;

/// <summary>
/// Tier-1 contract for analytics tracking functionality.
/// Engine-free interface following the 4-tier architecture pattern.
/// Supports event tracking with routing strategies (FanOut, Sharded).
/// </summary>
public interface IAnalytics
{
    /// <summary>
    /// Tracks an analytics event asynchronously.
    /// </summary>
    /// <param name="analyticsEvent">The analytics event to track.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous tracking operation.</returns>
    Task Track(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default);
}
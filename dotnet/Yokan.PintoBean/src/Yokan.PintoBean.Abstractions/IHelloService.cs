// Tier-1: Hello service contract for Yokan PintoBean service platform

using System.Threading;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Abstractions;

/// <summary>
/// Tier-1 seed contract for hello/greeting functionality.
/// Engine-free interface following the 4-tier architecture pattern.
/// </summary>
public interface IHelloService
{
    /// <summary>
    /// Returns a greeting message for the specified name.
    /// </summary>
    /// <param name="request">The hello request containing the name and optional parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A hello response with the greeting message.</returns>
    Task<HelloResponse> SayHelloAsync(HelloRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a farewell message for the specified name.
    /// </summary>
    /// <param name="request">The goodbye request containing the name and optional parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A hello response with the farewell message.</returns>
    Task<HelloResponse> SayGoodbyeAsync(HelloRequest request, CancellationToken cancellationToken = default);
}
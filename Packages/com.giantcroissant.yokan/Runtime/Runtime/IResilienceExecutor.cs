// Tier-3: Resilience executor interface for cross-cutting concerns

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// Cross-cutting adapter for resilience patterns (circuit breaker, retry, timeout, etc.).
/// Integrates with Polly or similar resilience frameworks to provide consistent
/// fault handling across all service invocations.
/// </summary>
public interface IResilienceExecutor
{
    /// <summary>
    /// Executes a function with resilience policies applied.
    /// </summary>
    /// <typeparam name="TResult">The return type of the function.</typeparam>
    /// <param name="func">The function to execute with resilience.</param>
    /// <returns>The result from the function execution.</returns>
    TResult Execute<TResult>(Func<TResult> func);

    /// <summary>
    /// Executes an async function with resilience policies applied.
    /// </summary>
    /// <typeparam name="TResult">The return type of the async function.</typeparam>
    /// <param name="func">The async function to execute with resilience.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the result from the function execution.</returns>
    Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> func, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an async action with resilience policies applied.
    /// </summary>
    /// <param name="func">The async action to execute with resilience.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the operation.</returns>
    Task ExecuteAsync(Func<CancellationToken, Task> func, CancellationToken cancellationToken = default);
}

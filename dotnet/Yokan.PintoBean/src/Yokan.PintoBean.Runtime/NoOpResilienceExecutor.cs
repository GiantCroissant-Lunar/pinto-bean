// Tier-3: No-operation resilience executor implementation

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Runtime;

/// <summary>
/// No-operation implementation of IResilienceExecutor that performs pass-through execution.
/// This is the default implementation that executes functions directly without any resilience policies.
/// Useful for scenarios where resilience features are disabled or not needed.
/// </summary>
public sealed class NoOpResilienceExecutor : IResilienceExecutor
{
    /// <summary>
    /// Singleton instance of the no-op resilience executor.
    /// </summary>
    public static readonly NoOpResilienceExecutor Instance = new();

    private NoOpResilienceExecutor() { }

    /// <inheritdoc />
    public TResult Execute<TResult>(Func<TResult> func)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));
        
        return func();
    }

    /// <inheritdoc />
    public Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> func, CancellationToken cancellationToken = default)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));
        
        return func(cancellationToken);
    }

    /// <inheritdoc />
    public Task ExecuteAsync(Func<CancellationToken, Task> func, CancellationToken cancellationToken = default)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));
        
        return func(cancellationToken);
    }
}
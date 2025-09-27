using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Runtime.Unity;

/// <summary>
/// Default implementation of IUnityScheduler that provides Unity main-thread synchronization.
/// This implementation uses a simple queue-based approach that can be integrated with Unity's update loop.
/// </summary>
public class DefaultUnityScheduler : IUnityScheduler, IDisposable
{
    private readonly ConcurrentQueue<Action> _actionQueue = new();
    private readonly int _mainThreadId;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultUnityScheduler"/> class.
    /// </summary>
    /// <param name="mainThreadId">The ID of the Unity main thread. If not provided, uses the current thread.</param>
    public DefaultUnityScheduler(int? mainThreadId = null)
    {
        _mainThreadId = mainThreadId ?? Thread.CurrentThread.ManagedThreadId;
    }

    /// <inheritdoc />
    public bool IsMainThread => Thread.CurrentThread.ManagedThreadId == _mainThreadId;

    /// <inheritdoc />
    public void Post(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        ThrowIfDisposed();

        if (IsMainThread)
        {
            // If we're already on the main thread, execute immediately
            action();
        }
        else
        {
            // Queue for execution on the main thread
            _actionQueue.Enqueue(action);
        }
    }

    /// <inheritdoc />
    public Task PostAsync(Func<Task> func)
    {
        if (func == null) throw new ArgumentNullException(nameof(func));
        ThrowIfDisposed();

        if (IsMainThread)
        {
            // If we're already on the main thread, execute immediately
            return func();
        }
        else
        {
            // Queue for execution on the main thread and return a task that completes when done
            var tcs = new TaskCompletionSource<object?>();
            
            _actionQueue.Enqueue(async () =>
            {
                try
                {
                    await func();
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }
    }

    /// <summary>
    /// Processes all queued actions. This method should be called from the Unity main thread,
    /// typically in a MonoBehaviour's Update method or similar Unity callback.
    /// </summary>
    public void ProcessQueue()
    {
        ThrowIfDisposed();

        // Process all currently queued actions
        while (_actionQueue.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                // In Unity, this would typically be logged with Debug.LogException
                // For now, we'll re-throw to maintain error visibility
                throw new InvalidOperationException("Error executing queued action on Unity main thread", ex);
            }
        }
    }

    /// <summary>
    /// Gets the number of actions currently queued for execution.
    /// </summary>
    public int QueueCount => _actionQueue.Count;

    /// <summary>
    /// Disposes the scheduler and clears any queued actions.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        // Clear the queue
        while (_actionQueue.TryDequeue(out _)) { }
        
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DefaultUnityScheduler));
    }
}
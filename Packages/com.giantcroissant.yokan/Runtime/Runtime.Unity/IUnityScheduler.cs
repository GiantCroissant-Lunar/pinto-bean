using System;
using System.Threading.Tasks;

namespace Yokan.PintoBean.Runtime.Unity;

/// <summary>
/// Scheduler interface for ensuring operations run on the Unity main thread.
/// Provides synchronization adapter for provider calls that must execute on the main thread.
/// </summary>
public interface IUnityScheduler
{
    /// <summary>
    /// Posts an action to be executed on the Unity main thread.
    /// </summary>
    /// <param name="action">The action to execute on the main thread.</param>
    void Post(Action action);

    /// <summary>
    /// Posts an async function to be executed on the Unity main thread and returns a task that completes when the operation is done.
    /// </summary>
    /// <param name="func">The async function to execute on the main thread.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PostAsync(Func<Task> func);

    /// <summary>
    /// Gets a value indicating whether the current thread is the Unity main thread.
    /// </summary>
    bool IsMainThread { get; }
}
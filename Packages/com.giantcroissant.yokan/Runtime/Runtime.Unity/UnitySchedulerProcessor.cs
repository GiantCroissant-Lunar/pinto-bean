using System;

namespace Yokan.PintoBean.Runtime.Unity;

/// <summary>
/// Component responsible for processing the Unity scheduler queue.
/// This should be attached to a GameObject in Unity to process scheduled actions on the main thread.
/// 
/// Note: This class is designed to compile without Unity references for testing,
/// but should inherit from MonoBehaviour when used in Unity.
/// </summary>
public class UnitySchedulerProcessor // In Unity: : MonoBehaviour
{
    private IUnityScheduler? _scheduler;

    /// <summary>
    /// Gets or sets the scheduler to process. If null, attempts to resolve from the Unity bridge.
    /// </summary>
    public IUnityScheduler? Scheduler
    {
        get => _scheduler ?? (UnityServiceProviderBridge.IsInitialized ? UnityServiceProviderBridge.Current.Scheduler : null);
        set => _scheduler = value;
    }

    // In Unity: void Start()
    /// <summary>
    /// Initializes the processor by attempting to resolve the scheduler from the Unity bridge.
    /// </summary>
    public void Initialize()
    {
        if (_scheduler == null && UnityServiceProviderBridge.IsInitialized)
        {
            _scheduler = UnityServiceProviderBridge.Current.Scheduler;
        }

        if (_scheduler == null)
        {
            // In Unity: Debug.LogWarning("UnitySchedulerProcessor: No scheduler found. Ensure AddUnityScheduler() is called during service configuration.");
            System.Console.WriteLine("UnitySchedulerProcessor: No scheduler found. Ensure AddUnityScheduler() is called during service configuration.");
        }
    }

    // In Unity: void Update()
    /// <summary>
    /// Processes the scheduler queue. This should be called from Unity's Update loop.
    /// </summary>
    public void ProcessSchedulerQueue()
    {
        var scheduler = Scheduler;
        if (scheduler is DefaultUnityScheduler defaultScheduler)
        {
            try
            {
                defaultScheduler.ProcessQueue();
            }
            catch (Exception ex)
            {
                // In Unity: Debug.LogException(ex);
                System.Console.WriteLine($"Error processing scheduler queue: {ex}");
            }
        }
    }

    // In Unity: void OnDestroy()
    /// <summary>
    /// Cleans up resources when the processor is destroyed.
    /// </summary>
    public void Cleanup()
    {
        if (_scheduler is IDisposable disposableScheduler)
        {
            disposableScheduler.Dispose();
        }
        _scheduler = null;
    }
}
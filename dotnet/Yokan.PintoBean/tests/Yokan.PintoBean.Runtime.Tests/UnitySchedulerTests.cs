using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Yokan.PintoBean.Runtime.Unity;

namespace Yokan.PintoBean.Runtime.Tests;

public class UnitySchedulerTests
{
    [Fact]
    public void DefaultUnityScheduler_Constructor_SetsMainThreadId()
    {
        // Arrange
        var expectedThreadId = Thread.CurrentThread.ManagedThreadId;

        // Act
        var scheduler = new DefaultUnityScheduler();

        // Assert
        Assert.True(scheduler.IsMainThread);
    }

    [Fact]
    public void DefaultUnityScheduler_Constructor_WithCustomThreadId_SetsCorrectThreadId()
    {
        // Arrange
        var customThreadId = 12345;

        // Act
        var scheduler = new DefaultUnityScheduler(customThreadId);

        // Assert
        Assert.False(scheduler.IsMainThread); // Current thread is not the custom thread ID
    }

    [Fact]
    public void Post_WhenCalledFromMainThread_ExecutesImmediately()
    {
        // Arrange
        var scheduler = new DefaultUnityScheduler();
        var executed = false;

        // Act
        scheduler.Post(() => executed = true);

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public void Post_WhenCalledFromDifferentThread_QueuesAction()
    {
        // Arrange
        var mainThreadId = Thread.CurrentThread.ManagedThreadId;
        var scheduler = new DefaultUnityScheduler(mainThreadId + 1); // Different thread ID
        var executed = false;

        // Act
        scheduler.Post(() => executed = true);

        // Assert - Action should be queued, not executed immediately
        Assert.False(executed);
        Assert.Equal(1, scheduler.QueueCount);
    }

    [Fact]
    public void ProcessQueue_ExecutesQueuedActions()
    {
        // Arrange
        var mainThreadId = Thread.CurrentThread.ManagedThreadId;
        var scheduler = new DefaultUnityScheduler(mainThreadId + 1); // Different thread ID
        var executed = false;

        scheduler.Post(() => executed = true);

        // Act
        scheduler.ProcessQueue();

        // Assert
        Assert.True(executed);
        Assert.Equal(0, scheduler.QueueCount);
    }

    [Fact]
    public async Task PostAsync_WhenCalledFromMainThread_ExecutesImmediately()
    {
        // Arrange
        var scheduler = new DefaultUnityScheduler();
        var executed = false;

        // Act
        await scheduler.PostAsync(async () =>
        {
            await Task.Delay(1); // Simulate async work
            executed = true;
        });

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task PostAsync_WhenCalledFromDifferentThread_ReturnsTaskThatCompletesAfterProcessing()
    {
        // Arrange
        var mainThreadId = Thread.CurrentThread.ManagedThreadId;
        var scheduler = new DefaultUnityScheduler(mainThreadId + 1); // Different thread ID
        var executed = false;

        // Act
        var task = scheduler.PostAsync(async () =>
        {
            await Task.Delay(1); // Simulate async work
            executed = true;
        });

        // Assert - Task should not be completed yet
        Assert.False(task.IsCompleted);
        Assert.False(executed);

        // Process the queue to complete the task
        scheduler.ProcessQueue();
        await task;

        Assert.True(executed);
    }

    [Fact]
    public void Post_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var scheduler = new DefaultUnityScheduler();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => scheduler.Post(null!));
    }

    [Fact]
    public async Task PostAsync_WithNullFunc_ThrowsArgumentNullException()
    {
        // Arrange
        var scheduler = new DefaultUnityScheduler();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => scheduler.PostAsync(null!));
    }

    [Fact]
    public void Dispose_ClearsQueue()
    {
        // Arrange
        var mainThreadId = Thread.CurrentThread.ManagedThreadId;
        var scheduler = new DefaultUnityScheduler(mainThreadId + 1); // Different thread ID

        scheduler.Post(() => { });
        Assert.Equal(1, scheduler.QueueCount);

        // Act
        scheduler.Dispose();

        // Assert
        Assert.Equal(0, scheduler.QueueCount);
    }

    [Fact]
    public void Post_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var scheduler = new DefaultUnityScheduler();
        scheduler.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => scheduler.Post(() => { }));
    }

    [Fact]
    public void AddUnityScheduler_RegistersSchedulerInServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddUnityScheduler();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var scheduler = serviceProvider.GetService<IUnityScheduler>();
        Assert.NotNull(scheduler);
        Assert.IsType<DefaultUnityScheduler>(scheduler);
    }

    [Fact]
    public void AddUnityScheduler_WithCustomScheduler_RegistersCustomScheduler()
    {
        // Arrange
        var services = new ServiceCollection();
        var customScheduler = new DefaultUnityScheduler();

        // Act
        services.AddUnityScheduler(customScheduler);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var scheduler = serviceProvider.GetService<IUnityScheduler>();
        Assert.Same(customScheduler, scheduler);
    }

    [Fact]
    public void UnityServiceProviderBridge_WithScheduler_ProvidesSchedulerAccess()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnityScheduler();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var bridge = UnityServiceProviderBridge.Initialize(serviceProvider);

        // Assert
        Assert.NotNull(bridge.Scheduler);
        Assert.IsType<DefaultUnityScheduler>(bridge.Scheduler);

        // Cleanup
        UnityServiceProviderBridge.Reset();
    }

    [Fact]
    public void UnityServiceProviderBridge_PostToMainThread_CallsSchedulerPost()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnityScheduler();
        var serviceProvider = services.BuildServiceProvider();
        var bridge = UnityServiceProviderBridge.Initialize(serviceProvider);
        var executed = false;

        // Act
        bridge.PostToMainThread(() => executed = true);

        // Assert
        Assert.True(executed); // Should execute immediately since we're on the main thread

        // Cleanup
        UnityServiceProviderBridge.Reset();
    }

    [Fact]
    public async Task UnityServiceProviderBridge_PostToMainThreadAsync_CallsSchedulerPostAsync()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddUnityScheduler();
        var serviceProvider = services.BuildServiceProvider();
        var bridge = UnityServiceProviderBridge.Initialize(serviceProvider);
        var executed = false;

        // Act
        await bridge.PostToMainThreadAsync(async () =>
        {
            await Task.Delay(1);
            executed = true;
        });

        // Assert
        Assert.True(executed);

        // Cleanup
        UnityServiceProviderBridge.Reset();
    }
}
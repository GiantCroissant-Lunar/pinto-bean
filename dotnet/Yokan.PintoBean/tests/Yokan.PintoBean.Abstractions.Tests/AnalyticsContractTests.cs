// Tests for IAnalytics contract and AnalyticsEvent model

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Yokan.PintoBean.Abstractions;

namespace Yokan.PintoBean.Abstractions.Tests;

/// <summary>
/// Tests for the IAnalytics service contract and related models.
/// </summary>
public class AnalyticsContractTests
{
    [Fact]
    public void AnalyticsEvent_WithRequiredProperties_CreatesValidInstance()
    {
        // Arrange & Act
        var analyticsEvent = new AnalyticsEvent
        {
            EventName = "player.level.complete"
        };

        // Assert
        Assert.Equal("player.level.complete", analyticsEvent.EventName);
        Assert.Null(analyticsEvent.Properties);
        Assert.Null(analyticsEvent.UserId);
        Assert.Null(analyticsEvent.SessionId);
        Assert.True(analyticsEvent.Timestamp <= DateTime.UtcNow);
        Assert.True(analyticsEvent.Timestamp > DateTime.UtcNow.AddSeconds(-5)); // Should be recent
    }

    [Fact]
    public void AnalyticsEvent_WithAllProperties_CreatesValidInstance()
    {
        // Arrange
        var properties = new Dictionary<string, object>
        {
            ["level"] = 5,
            ["score"] = 1250
        };
        var timestamp = DateTime.UtcNow.AddMinutes(-1);

        // Act
        var analyticsEvent = new AnalyticsEvent
        {
            EventName = "player.achievement.unlocked",
            Properties = properties,
            UserId = "user123",
            SessionId = "session456",
            Timestamp = timestamp
        };

        // Assert
        Assert.Equal("player.achievement.unlocked", analyticsEvent.EventName);
        Assert.Same(properties, analyticsEvent.Properties);
        Assert.Equal("user123", analyticsEvent.UserId);
        Assert.Equal("session456", analyticsEvent.SessionId);
        Assert.Equal(timestamp, analyticsEvent.Timestamp);
    }

    [Fact]
    public void AnalyticsEvent_DefaultTimestamp_IsCurrentUtcTime()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var analyticsEvent = new AnalyticsEvent
        {
            EventName = "test.event"
        };

        // Assert
        var after = DateTime.UtcNow;
        Assert.True(analyticsEvent.Timestamp >= before);
        Assert.True(analyticsEvent.Timestamp <= after);
    }

    [Fact]
    public async Task IAnalytics_TrackMethod_HasCorrectSignature()
    {
        // Arrange
        var mockAnalytics = new MockAnalyticsProvider();
        var analyticsEvent = new AnalyticsEvent { EventName = "test.event" };
        var cancellationToken = new CancellationToken();

        // Act & Assert - This test verifies the method signature compiles correctly
        await mockAnalytics.Track(analyticsEvent, cancellationToken);
        Assert.True(mockAnalytics.TrackWasCalled);
    }

    /// <summary>
    /// Mock implementation of IAnalytics for testing.
    /// </summary>
    private class MockAnalyticsProvider : IAnalytics
    {
        public bool TrackWasCalled { get; private set; }

        public Task Track(AnalyticsEvent analyticsEvent, CancellationToken cancellationToken = default)
        {
            TrackWasCalled = true;
            return Task.CompletedTask;
        }
    }
}
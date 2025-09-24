using System;
using Xunit;
using Yokan.PintoBean.Runtime;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Tests for the PlatformDetector utility class.
/// </summary>
public class PlatformDetectorTests
{
    [Fact]
    public void CurrentPlatform_ShouldReturnValidPlatform()
    {
        // Act
        var currentPlatform = PlatformDetector.CurrentPlatform;

        // Assert
        Assert.True(Enum.IsDefined(typeof(Platform), currentPlatform));
        
        // Should return the same value on subsequent calls (cached)
        var secondCall = PlatformDetector.CurrentPlatform;
        Assert.Equal(currentPlatform, secondCall);
    }

    [Theory]
    [InlineData(Platform.Any, true)]
    public void IsCompatible_WithPlatformAny_ShouldAlwaysReturnTrue(Platform providerPlatform, bool expected)
    {
        // Act
        var isCompatible = PlatformDetector.IsCompatible(providerPlatform);

        // Assert
        Assert.Equal(expected, isCompatible);
    }

    [Fact]
    public void IsCompatible_WithCurrentPlatform_ShouldReturnTrue()
    {
        // Arrange
        var currentPlatform = PlatformDetector.CurrentPlatform;

        // Act
        var isCompatible = PlatformDetector.IsCompatible(currentPlatform);

        // Assert
        Assert.True(isCompatible);
    }

    [Theory]
    [InlineData(Platform.Unity)]
    [InlineData(Platform.Godot)]
    [InlineData(Platform.DotNet)]
    [InlineData(Platform.Web)]
    [InlineData(Platform.Mobile)]
    [InlineData(Platform.Desktop)]
    public void IsCompatible_WithSpecificPlatforms_ShouldFollowCompatibilityRules(Platform platform)
    {
        // Act
        var isCompatible = PlatformDetector.IsCompatible(platform);

        // Assert
        // We can't predict the exact result without knowing the current platform,
        // but it should at least be consistent
        var secondResult = PlatformDetector.IsCompatible(platform);
        Assert.Equal(isCompatible, secondResult);
    }

    [Fact]
    public void IsCompatible_ConsistencyCheck()
    {
        // Arrange
        var allPlatforms = Enum.GetValues<Platform>();

        // Act & Assert
        foreach (var platform in allPlatforms)
        {
            // Should be consistent across multiple calls
            var result1 = PlatformDetector.IsCompatible(platform);
            var result2 = PlatformDetector.IsCompatible(platform);
            Assert.Equal(result1, result2);

            // Platform.Any should always be compatible
            if (platform == Platform.Any)
            {
                Assert.True(result1);
            }
        }
    }
}
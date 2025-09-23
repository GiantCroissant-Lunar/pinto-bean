using Xunit;

namespace Yokan.PintoBean.Runtime.Tests;

/// <summary>
/// Basic test class for Runtime functionality.
/// </summary>
public class RuntimeTests
{
    /// <summary>
    /// Tests that the Runtime version is accessible.
    /// </summary>
    [Fact]
    public void Version_ShouldBeAccessible()
    {
        // Arrange & Act
        var version = PintoBeanRuntime.Version;

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
    }
}
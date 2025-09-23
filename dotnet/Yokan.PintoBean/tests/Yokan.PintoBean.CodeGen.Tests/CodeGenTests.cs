using Xunit;

namespace Yokan.PintoBean.CodeGen.Tests;

/// <summary>
/// Basic test class for CodeGen functionality.
/// </summary>
public class CodeGenTests
{
    /// <summary>
    /// Tests that the CodeGen version is accessible.
    /// </summary>
    [Fact]
    public void Version_ShouldBeAccessible()
    {
        // Arrange & Act
        var version = PintoBeanCodeGen.Version;

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
    }
}
using Xunit;
using Yokan.PintoBean.Runtime;
using Yokan.PintoBean.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;

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
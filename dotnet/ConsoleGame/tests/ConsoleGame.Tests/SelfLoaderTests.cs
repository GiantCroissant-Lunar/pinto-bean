using ConsoleGame.App;
using Xunit;

namespace ConsoleGame.Tests;

public class SelfLoaderTests
{
    [Fact]
    public void LoadSelfAndInvoke_ReturnsCompositeMessage()
    {
        var message = SelfLoader.LoadSelfAndInvoke();
        Assert.Contains("Primary:", message);
        Assert.Contains("Loaded:", message);
        Assert.Contains("Contexts:", message);
        Assert.Contains("SelfCopyContext", message);
    }
}

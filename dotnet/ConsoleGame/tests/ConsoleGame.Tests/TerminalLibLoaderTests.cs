using System;
using System.IO;
using ConsoleGame.App;
using Xunit;

namespace ConsoleGame.Tests;

public class TerminalLibLoaderTests
{
    [Fact]
    public void LoadTerminalLibAndGetInfo_ReturnsMessage()
    {
        // Resolve the path where the test assembly is running from
        var baseDir = AppContext.BaseDirectory; // tests bin path
        var tfmDir = new DirectoryInfo(baseDir).Name; // e.g., net9.0
        var configDir = new DirectoryInfo(baseDir).Parent?.Name ?? "Debug";
        // Solution-relative path to the library output
        var candidate = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "src", "ConsoleGame.TerminalLib", "bin", configDir, tfmDir, "ConsoleGame.TerminalLib.dll"));
        if (!File.Exists(candidate))
        {
            // If not built yet, skip gracefully
            return;
        }

        var msg = SelfLoader.LoadTerminalLibAndGetInfo(candidate);
        Assert.Contains("Terminal.Gui loaded:", msg);
        Assert.Contains("Context=TerminalLibContext", msg);
    }
}

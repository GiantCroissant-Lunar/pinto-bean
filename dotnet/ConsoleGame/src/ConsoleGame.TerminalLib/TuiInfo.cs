using System.Reflection;
using ConsoleGame.Contracts;
using Terminal.Gui;

namespace ConsoleGame.TerminalLib;

public static class TuiInfo
{
    public static string GetInfo()
    {
        var asm = typeof(Application).Assembly;
        var name = asm.GetName();
        return $"Terminal.Gui loaded: {name.Name} v{name.Version}";
    }
}

[Plugin("consolegame.terminalgui", "Terminal.Gui Plugin", "1.0.0", "Demo Terminal.Gui-based plugin", "ConsoleGame Team")]
public sealed class TuiPlugin : IPlugin
{
    public string Name => "Terminal.Gui Plugin";
    public string Describe()
    {
        return $"{Name} => {TuiInfo.GetInfo()}";
    }
}

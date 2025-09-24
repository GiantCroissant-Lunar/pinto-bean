using System.Reflection;
using Terminal.Gui;

namespace ConsoleGame.TerminalLib;

public static class TuiInfo
{
    public static string GetInfo()
    {
        // Touch a type to ensure Terminal.Gui is loaded and resolvable
        var asm = typeof(Application).Assembly;
        var name = asm.GetName();
        return $"Terminal.Gui loaded: {name.Name} v{name.Version}";
    }
}

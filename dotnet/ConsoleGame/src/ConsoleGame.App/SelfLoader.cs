using System.Reflection;
using System.Runtime.Loader;

namespace ConsoleGame.App;

public static class SelfLoader
{
    // Contract: load a second copy of this assembly into a collectible ALC and call Messages.Hello()
    // Returns a composite message that shows both contexts.
    public static string LoadSelfAndInvoke()
    {
        var currentAsm = Assembly.GetExecutingAssembly();
        var asmPath = currentAsm.Location;

        // Create a collectible ALC to demonstrate isolation/unload.
        var alc = new AssemblyLoadContext("SelfCopyContext", isCollectible: true);
        try
        {
            var asmCopy = alc.LoadFromAssemblyPath(asmPath);
            var type = asmCopy.GetType("ConsoleGame.App.Messages");
            if (type == null)
                throw new InvalidOperationException("Type ConsoleGame.App.Messages not found in loaded copy.");

            var method = type.GetMethod("Hello", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException("Method Hello not found on Messages.");

            var result = method.Invoke(null, null) as string ?? string.Empty;

            return $"Primary: {Messages.Hello()} | Loaded: {result} | Contexts: primary={AssemblyLoadContext.GetLoadContext(currentAsm)?.Name ?? "default"}, copy={alc.Name}";
        }
        finally
        {
            alc.Unload();
        }
    }

    // Load a specified assembly file into a collectible ALC and invoke ConsoleGame.TerminalLib.TuiInfo.GetInfo().
    public static string LoadTerminalLibAndGetInfo(string assemblyPath)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath)) throw new ArgumentException("assemblyPath is required", nameof(assemblyPath));
        var alc = new PluginLoadContext(assemblyPath);
        try
        {
            var asm = alc.LoadFromAssemblyPath(assemblyPath);
            var type = asm.GetType("ConsoleGame.TerminalLib.TuiInfo");
            if (type == null) throw new InvalidOperationException("TuiInfo type not found in TerminalLib.");
            var method = type.GetMethod("GetInfo", BindingFlags.Public | BindingFlags.Static);
            if (method == null) throw new InvalidOperationException("GetInfo method not found on TuiInfo.");
            var result = method.Invoke(null, null) as string ?? string.Empty;
            return $"Plugin: {result} | Context={alc.Name}";
        }
        finally
        {
            alc.Unload();
        }
    }
}

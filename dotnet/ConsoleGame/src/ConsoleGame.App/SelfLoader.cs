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
}

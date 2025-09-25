using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using ConsoleGame.Contracts;
using System.Linq;

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

    public static IPlugin LoadPlugin(string pluginAssemblyPath)
    {
        if (string.IsNullOrWhiteSpace(pluginAssemblyPath)) throw new ArgumentException("pluginAssemblyPath is required", nameof(pluginAssemblyPath));
        var alc = new PluginLoadContext(pluginAssemblyPath);
        try
        {
            var asm = alc.LoadFromAssemblyPath(pluginAssemblyPath);
            var pluginType = asm.GetType("ConsoleGame.TerminalLib.TuiPlugin");
            if (pluginType == null) throw new InvalidOperationException("Plugin type not found in assembly.");
            if (!typeof(IPlugin).IsAssignableFrom(pluginType)) throw new InvalidOperationException("Type does not implement IPlugin.");
            // Create instance in the plugin context; return as IPlugin (contract lives in default context)
            var instance = Activator.CreateInstance(pluginType);
            return (IPlugin)instance!;
        }
        catch
        {
            // Ensure unload on failure too
            alc.Unload();
            throw;
        }
    }

    /// <summary>
    /// Load a plugin assembly in a collectible context, set up its plugin context, call optional ConfigureAsync,
    /// and if a RunAsync method exists, await it until completion or cancellation. Returns details about unload verification.
    /// </summary>
    public static async Task<PluginRunResult> RunPluginAsync(string pluginAssemblyPath, IServiceProvider services, CancellationToken shutdownToken = default)
    {
        if (string.IsNullOrWhiteSpace(pluginAssemblyPath)) throw new ArgumentException("pluginAssemblyPath is required", nameof(pluginAssemblyPath));

        PluginLoadContext? alc = new PluginLoadContext(pluginAssemblyPath);
        var alcWeakRef = new WeakReference(alc, trackResurrection: false);
        Assembly? asm = null;
        Type? pluginType = null;
        object? instance = null;
        try
        {
            asm = alc.LoadFromAssemblyPath(pluginAssemblyPath);

            // Find first concrete type implementing ConsoleGame.Contracts.IPlugin from the default context
            var pluginContract = typeof(IPlugin);
            pluginType = asm
                .GetTypes()
                .FirstOrDefault(t => t.IsClass && !t.IsAbstract && pluginContract.IsAssignableFrom(t));
            if (pluginType == null)
            {
                throw new InvalidOperationException($"No type implementing {pluginContract.FullName} was found in {Path.GetFileName(pluginAssemblyPath)}.");
            }

            instance = Activator.CreateInstance(pluginType) ?? throw new InvalidOperationException($"Failed to create instance of {pluginType.FullName}");

            // Create and assign plugin context if supported
            var pluginDir = Path.GetDirectoryName(pluginAssemblyPath) ?? AppContext.BaseDirectory;
            var hostCtx = new HostPluginContext(services, pluginDir, properties: null, shutdownToken: shutdownToken);

            // Prefer direct assignment if property exists on the interface, otherwise set via reflection
            var contextProp = pluginType.GetProperty("Context", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (contextProp != null && contextProp.CanWrite && typeof(IPluginContext).IsAssignableFrom(contextProp.PropertyType))
            {
                contextProp.SetValue(instance, hostCtx);
            }

            // Call optional ConfigureAsync(CancellationToken) or ConfigureAsync()
            await InvokeOptionalAsync(instance, pluginType, methodName: "ConfigureAsync", shutdownToken).ConfigureAwait(false);

            // If the plugin also implements IRuntimePlugin, call RunAsync until cancellation; otherwise, try a duck-typed RunAsync.
            var runtimeInterface = typeof(IRuntimePlugin);
            if (runtimeInterface.IsAssignableFrom(pluginType))
            {
                // Strongly-typed path
                var runAsync = pluginType.GetMethod("RunAsync", BindingFlags.Instance | BindingFlags.Public);
                if (runAsync == null)
                {
                    throw new InvalidOperationException($"{pluginType.FullName} implements IRuntimePlugin but does not have RunAsync().");
                }
                await InvokeAsyncMethod(instance, runAsync, shutdownToken).ConfigureAwait(false);
            }
            else
            {
                // Duck-typed fallback: invoke RunAsync if present
                var runAsync = pluginType.GetMethod("RunAsync", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (runAsync != null)
                {
                    await InvokeAsyncMethod(instance, runAsync, shutdownToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            if (instance is IDisposable disposable)
            {
                try { disposable.Dispose(); } catch { }
            }
            instance = null;
            pluginType = null;
            asm = null;

            alc?.Unload();
        }

        alc = null;

        if (!WaitForUnload(alcWeakRef, out var elapsed, attempts: 40, delay: TimeSpan.FromMilliseconds(250)))
        {
            var debugEnv = Environment.GetEnvironmentVariable("CONSOLEGAME_DEBUG_UNLOAD");
            if (!string.IsNullOrWhiteSpace(debugEnv) && debugEnv.Trim() == "1")
            {
                Console.WriteLine("[Unload] Collectible AssemblyLoadContexts still alive:");
                var trackedCtx = alcWeakRef.Target as AssemblyLoadContext;
                if (trackedCtx != null)
                {
                    Console.WriteLine($"  - (tracked) {trackedCtx.Name ?? "<unnamed>"}, Assemblies={trackedCtx.Assemblies.Count()}");
                    foreach (var assembly in trackedCtx.Assemblies.OrderBy(a => a.FullName, StringComparer.Ordinal))
                    {
                        Console.WriteLine($"      * {assembly.FullName}");
                    }
                }
                foreach (var ctx in AssemblyLoadContext.All.Where(c => c.IsCollectible))
                {
                    Console.WriteLine($"  - {ctx.Name ?? "<unnamed>"}, IsAlive={alcWeakRef.IsAlive}");
                    foreach (var assembly in ctx.Assemblies.OrderBy(a => a.FullName, StringComparer.Ordinal))
                    {
                        Console.WriteLine($"      * {assembly.FullName}");
                    }
                }
            }
            return new PluginRunResult(false, elapsed);
        }

        return new PluginRunResult(true, elapsed);
    }

    private static async Task InvokeOptionalAsync(object instance, Type type, string methodName, CancellationToken token)
    {
        var mi = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (mi == null) return;
        await InvokeAsyncMethod(instance, mi, token).ConfigureAwait(false);
    }

    private static Task InvokeAsyncMethod(object instance, MethodInfo mi, CancellationToken token)
    {
        // Prefer overload accepting CancellationToken if available
        var parameters = mi.GetParameters();
        object? result;
        if (parameters.Length == 1 && parameters[0].ParameterType == typeof(CancellationToken))
        {
            result = mi.Invoke(instance, new object[] { token });
        }
        else if (parameters.Length == 0)
        {
            result = mi.Invoke(instance, null);
        }
        else
        {
            // Mismatched signature; don't call
            return Task.CompletedTask;
        }

        if (result is Task task)
        {
            return task;
        }
        else
        {
            return Task.CompletedTask;
        }
    }

    private static bool WaitForUnload(WeakReference alcReference, out TimeSpan elapsed, int attempts, TimeSpan delay)
    {
        var sw = Stopwatch.StartNew();
        var unloaded = false;
        try
        {
            for (int i = 0; i < attempts; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                if (!alcReference.IsAlive)
                {
                    unloaded = true;
                    break;
                }

                if (delay > TimeSpan.Zero)
                {
                    Thread.Sleep(delay);
                }
            }

            return unloaded || !alcReference.IsAlive;
        }
        finally
        {
            sw.Stop();
            elapsed = sw.Elapsed;
        }
    }
}

public readonly record struct PluginRunResult(bool Unloaded, TimeSpan Duration);

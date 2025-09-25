using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace ConsoleGame.App;

internal sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _pluginDir;

    public PluginLoadContext(string mainAssemblyPath)
        : base("TerminalLibContext", isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
        _pluginDir = Path.GetDirectoryName(mainAssemblyPath) ?? AppContext.BaseDirectory;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Ensure the shared contracts assembly resolves to the default context to avoid type identity split
        if (string.Equals(assemblyName.Name, "ConsoleGame.Contracts", StringComparison.Ordinal))
        {
            return null; // fallback to default context resolution
        }

        if (IsSharedAssembly(assemblyName))
        {
            var existing = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName.Name, StringComparison.Ordinal));
            if (existing != null)
            {
                return existing;
            }

            var sharedPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (!string.IsNullOrWhiteSpace(sharedPath))
            {
                return Assembly.LoadFrom(sharedPath);
            }

            return Assembly.Load(assemblyName);
        }
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }
        // Fallback: probe next to the plugin assembly
        var candidate = Path.Combine(_pluginDir, assemblyName.Name + ".dll");
        if (File.Exists(candidate))
        {
            return LoadFromAssemblyPath(candidate);
        }
        return null; // fallback to default context
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }
        return IntPtr.Zero;
    }

    private static bool IsSharedAssembly(AssemblyName assemblyName)
    {
        var name = assemblyName.Name;
        return string.Equals(name, "LibVLCSharp", StringComparison.Ordinal)
            || string.Equals(name, "LibVLCSharp.Shared", StringComparison.Ordinal)
            || string.Equals(name, "Terminal.Gui", StringComparison.Ordinal)
            || string.Equals(name, "ReactiveUI", StringComparison.Ordinal)
            || string.Equals(name, "Splat", StringComparison.Ordinal)
            || string.Equals(name, "System.Reactive", StringComparison.Ordinal);
    }
}

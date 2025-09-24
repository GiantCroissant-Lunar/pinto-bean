using ConsoleGame.App;
using ConsoleGame.Contracts;

Console.WriteLine("ConsoleGame.App – AssemblyLoadContext demo");
var message = SelfLoader.LoadSelfAndInvoke();
Console.WriteLine(message);

// Try loading Terminal.Gui library via separate class library dynamically
var baseDir = AppContext.BaseDirectory; // points to bin/<config>/netX.Y/
var terminalLibPath = Path.Combine(baseDir, "ConsoleGame.TerminalLib.dll");
if (!File.Exists(terminalLibPath))
{
	// Fallback to sibling project's bin output (assuming solution-relative layout)
	// ../../ConsoleGame.TerminalLib/bin/<config>/<tfm>/ConsoleGame.TerminalLib.dll
	var configDir = new DirectoryInfo(baseDir).Parent?.Name ?? "Debug";
	var tfmDir = new DirectoryInfo(baseDir).Name; // e.g., net9.0
	var candidate = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "ConsoleGame.TerminalLib", "bin", configDir, tfmDir, "ConsoleGame.TerminalLib.dll"));
	if (File.Exists(candidate))
	{
		terminalLibPath = candidate;
	}
}
// Runtime-loaded plugin path via env var or fallback to Dungeon plugin
var envPlugin = Environment.GetEnvironmentVariable("CONSOLEGAME_PLUGIN_PATH");
var dungeonPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "ConsoleGame.Dungeon.Plugin", "bin", new DirectoryInfo(baseDir).Parent?.Name ?? "Debug", new DirectoryInfo(baseDir).Name, "ConsoleGame.Dungeon.Plugin.dll"));
string pluginPath;
if (!string.IsNullOrWhiteSpace(envPlugin) && File.Exists(envPlugin))
{
	pluginPath = envPlugin;
	Console.WriteLine($"Using plugin from env: {pluginPath}");
}
else
{
	pluginPath = File.Exists(dungeonPath) ? dungeonPath : terminalLibPath;
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

try
{
	if (File.Exists(pluginPath))
	{
		await SelfLoader.RunPluginAsync(pluginPath, services: new DefaultServices(), shutdownToken: cts.Token);
	}
	else
	{
		Console.WriteLine($"No plugin found. Checked: {pluginPath}");
	}
}
catch (Exception ex)
{
	Console.WriteLine($"Runtime plugin error: {ex.GetType().Name}: {ex.Message}");
}

// dummy services for sample
public sealed class DefaultServices : IServiceProvider
{
	public object? GetService(Type serviceType) => null;
}

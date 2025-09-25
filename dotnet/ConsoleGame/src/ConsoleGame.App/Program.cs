using ConsoleGame.App;
using ConsoleGame.Contracts;
using System.Globalization;

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
var autoCancelEnv = Environment.GetEnvironmentVariable("CONSOLEGAME_AUTOCANCEL_MS");
if (!string.IsNullOrWhiteSpace(autoCancelEnv) &&
	int.TryParse(autoCancelEnv, NumberStyles.Integer, CultureInfo.InvariantCulture, out var autoCancelMs) &&
	autoCancelMs > 0)
{
	cts.CancelAfter(TimeSpan.FromMilliseconds(autoCancelMs));
	Console.WriteLine($"Auto-cancel enabled after {autoCancelMs} ms.");
}
Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };
var services = new DefaultServices();

try
{
	if (File.Exists(pluginPath))
	{
		var result = await SelfLoader.RunPluginAsync(pluginPath, services: services, shutdownToken: cts.Token);
		Console.WriteLine($"Plugin unloaded: {result.Unloaded} after {result.Duration.TotalMilliseconds:N0} ms");
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

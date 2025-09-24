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
try
{
	if (File.Exists(terminalLibPath))
	{
		var pluginMsg = SelfLoader.LoadTerminalLibAndGetInfo(terminalLibPath);
		Console.WriteLine(pluginMsg);

		// Also load via IPlugin contract
		try
		{
			IPlugin plugin = SelfLoader.LoadPlugin(terminalLibPath);
			Console.WriteLine($"IPlugin: {plugin.Name} => {plugin.Describe()}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"IPlugin load failed: {ex.GetType().Name}: {ex.Message}");
		}
	}
	else
	{
		Console.WriteLine($"Plugin not found at {terminalLibPath}. Build the solution so the dll is copied alongside the app.");
	}
}
catch (Exception ex)
{
	Console.WriteLine($"Plugin load failed: {ex.GetType().Name}: {ex.Message}");
}

using ConsoleGame.App;
using ConsoleGame.App.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine;
using System.Threading;
using System.IO;
using System.Linq;

var rootCommand = BuildRootCommand();
await rootCommand.InvokeAsync(args);

static RootCommand BuildRootCommand()
{
	var pluginOption = new Option<FileInfo?>("--plugin", description: "Path to the plugin assembly to load.");
	var probeOption = new Option<DirectoryInfo[]>("--probe", description: "Additional directories to probe for plugin assemblies.")
	{
		AllowMultipleArgumentsPerToken = true
	};
	probeOption.SetDefaultValue(Array.Empty<DirectoryInfo>());

	var autoCancelOption = new Option<int?>("--auto-cancel", description: "Automatically cancel the plugin after the specified number of milliseconds.")
	{
		Arity = ArgumentArity.ZeroOrOne
	};

	var debugUnloadOption = CreateBoolOption("--debug-unload", "Enable collectible AssemblyLoadContext diagnostics.");
	var headlessOption = CreateBoolOption("--headless", "Run the plugin UI in headless mode (no Terminal.Gui loop).");
	var disableAudioOption = CreateBoolOption("--disable-audio", "Disable LibVLC audio playback inside the plugin.");
	var disableNativeCaptureOption = CreateBoolOption("--disable-native-capture", "Disable native stderr capture inside the plugin.");
	var debugUnloadWaitOption = CreateBoolOption("--debug-unload-wait", "Wait for input before exiting to inspect unload diagnostics.");

	var root = new RootCommand("ConsoleGame host and plugin runner");
	root.AddOption(pluginOption);
	root.AddOption(probeOption);
	root.AddOption(autoCancelOption);
	root.AddOption(debugUnloadOption);
	root.AddOption(headlessOption);
	root.AddOption(disableAudioOption);
	root.AddOption(disableNativeCaptureOption);
	root.AddOption(debugUnloadWaitOption);

	root.SetHandler(async (FileInfo? plugin, DirectoryInfo[] probes, int? autoCancel, bool? debugUnload, bool? headless, bool? disableAudio, bool? disableNativeCapture, bool? debugUnloadWait) =>
	{
		var probePaths = probes.Select(p => p.FullName).ToArray();
		await RunAsync(plugin?.FullName, probePaths, autoCancel, debugUnload, headless, disableAudio, disableNativeCapture, debugUnloadWait).ConfigureAwait(false);
	}, pluginOption, probeOption, autoCancelOption, debugUnloadOption, headlessOption, disableAudioOption, disableNativeCaptureOption, debugUnloadWaitOption);

	return root;
}

static Option<bool?> CreateBoolOption(string alias, string description)
{
	return new Option<bool?>(alias, description)
	{
		Arity = ArgumentArity.ZeroOrOne
	};
}

static async Task RunAsync(
	string? pluginPath,
	string[] probePaths,
	int? autoCancel,
	bool? debugUnload,
	bool? headless,
	bool? disableAudio,
	bool? disableNativeCapture,
	bool? debugUnloadWait,
	CancellationToken cancellationToken = default)
{
	Console.WriteLine("ConsoleGame.App – AssemblyLoadContext demo");
	Console.WriteLine(SelfLoader.LoadSelfAndInvoke());

	ApplyBooleanEnvironment("CONSOLEGAME_TUI_HEADLESS", headless);
	ApplyBooleanEnvironment("CONSOLEGAME_DISABLE_AUDIO", disableAudio);
	ApplyBooleanEnvironment("CONSOLEGAME_DISABLE_NATIVE_CAPTURE", disableNativeCapture);
	ApplyBooleanEnvironment("CONSOLEGAME_DEBUG_UNLOAD_WAIT", debugUnloadWait);

	var builder = Host.CreateApplicationBuilder(Array.Empty<string>());
	builder.Services.Configure<PluginHostOptions>(builder.Configuration.GetSection(PluginHostOptions.SectionName));
	builder.Services.AddHostedService<PluginRuntimeHostedService>();
	builder.Services.Configure<PluginHostOptions>(options =>
	{
		if (!string.IsNullOrWhiteSpace(pluginPath))
		{
			options.PluginPath = Path.GetFullPath(pluginPath);
		}

		if (probePaths.Length > 0)
		{
			options.ProbePaths.Clear();
			foreach (var probe in probePaths)
			{
				options.ProbePaths.Add(Path.GetFullPath(probe));
			}
		}

		if (autoCancel.HasValue)
		{
			options.AutoCancelMilliseconds = autoCancel > 0 ? autoCancel : null;
		}

		if (debugUnload.HasValue)
		{
			options.DebugUnload = debugUnload.Value;
		}
	});

	using var host = builder.Build();
	await host.RunAsync(cancellationToken).ConfigureAwait(false);
}

static void ApplyBooleanEnvironment(string variable, bool? value)
{
	if (!value.HasValue)
	{
		return;
	}

	Environment.SetEnvironmentVariable(variable, value.Value ? "1" : null);
}

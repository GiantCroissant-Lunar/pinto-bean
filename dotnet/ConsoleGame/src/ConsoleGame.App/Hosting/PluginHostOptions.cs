using System.Collections.Generic;

namespace ConsoleGame.App.Hosting;

public sealed class PluginHostOptions
{
    public const string SectionName = "PluginHost";

    public string? PluginPath { get; set; }

    public List<string> ProbePaths { get; } = new();

    public int? AutoCancelMilliseconds { get; set; }

    public bool DebugUnload { get; set; }
}

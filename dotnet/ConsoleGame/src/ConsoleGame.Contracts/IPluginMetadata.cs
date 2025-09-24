namespace ConsoleGame.Contracts;

public interface IPluginMetadata
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    string? Description { get; }
    string? Author { get; }
    IReadOnlyDictionary<string, string>? Dependencies { get; }
    IReadOnlyDictionary<string, object>? Properties { get; }
}

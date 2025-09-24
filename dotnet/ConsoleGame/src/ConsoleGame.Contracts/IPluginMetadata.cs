namespace ConsoleGame.Contracts;

public interface IPluginMetadata
{
    string Id { get; }
    string Name { get; }
    Version Version { get; }
    string Description { get; }
    string Author { get; }
    IReadOnlyList<string> Dependencies { get; }
    IReadOnlyDictionary<string, object> Properties { get; }
}

namespace ConsoleGame.Contracts;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class PluginAttribute : Attribute
{
    public PluginAttribute(string id, string name, string version, string description, string author)
    {
        Id = id;
        Name = name;
        Version = version;
        Description = description;
        Author = author;
    }

    public string Id { get; }
    public string Name { get; }
    public string Version { get; }
    public string Description { get; }
    public string Author { get; }
    public string[] Dependencies { get; set; } = Array.Empty<string>();
    public string? MinimumHostVersion { get; set; }
    public bool CanUnload { get; set; } = true;
    public string[] Tags { get; set; } = Array.Empty<string>();
}

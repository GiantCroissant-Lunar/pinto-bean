namespace ConsoleGame.Contracts;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class PluginAttribute : Attribute
{
    public PluginAttribute(string id, string name, string version)
    {
        Id = id; Name = name; Version = version;
    }
    public string Id { get; }
    public string Name { get; }
    public string Version { get; }
    public string? Description { get; set; }
    public string? Author { get; set; }
}

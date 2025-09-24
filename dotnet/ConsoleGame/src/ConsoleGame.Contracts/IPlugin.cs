namespace ConsoleGame.Contracts;

public interface IPlugin
{
    string Name { get; }
    string Describe();
}

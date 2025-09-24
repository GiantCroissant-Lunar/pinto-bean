using ConsoleGame.App;

Console.WriteLine("ConsoleGame.App – AssemblyLoadContext demo");
var message = SelfLoader.LoadSelfAndInvoke();
Console.WriteLine(message);

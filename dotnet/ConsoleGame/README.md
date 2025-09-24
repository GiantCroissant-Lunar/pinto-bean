# ConsoleGame – AssemblyLoadContext demo

This sample shows how to load a copy of the running assembly into a collectible AssemblyLoadContext and invoke a method from it.

## Structure
- src/ConsoleGame.App: Console app that calls `SelfLoader.LoadSelfAndInvoke()`
- tests/ConsoleGame.Tests: xUnit tests covering the basic behavior

## Run
```pwsh
# Build solution
dotnet build ./ConsoleGame.sln -warnaserror

# Run the app
dotnet run --project ./src/ConsoleGame.App/ConsoleGame.App.csproj

# Run tests
dotnet test ./ConsoleGame.sln --no-build
```

Expected output contains a composite message like:
```
Primary: Hello from primary load context | Loaded: Hello from primary load context | Contexts: primary=default, copy=SelfCopyContext
```

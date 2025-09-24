param(
    [string]$Configuration = "Debug",
    [string]$Framework = "net9.0",
    [string]$PluginPath
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$solutionDir = Join-Path $repoRoot 'dotnet/ConsoleGame'
$appProj = Join-Path $solutionDir 'src/ConsoleGame.App/ConsoleGame.App.csproj'

if (-not $PluginPath) {
    $dungeon = Join-Path $solutionDir "src/ConsoleGame.Dungeon.Plugin/bin/$Configuration/$Framework/ConsoleGame.Dungeon.Plugin.dll"
    $terminal = Join-Path $solutionDir "src/ConsoleGame.TerminalLib/bin/$Configuration/$Framework/ConsoleGame.TerminalLib.dll"
    if (Test-Path $dungeon) {
        $PluginPath = $dungeon
    } elseif (Test-Path $terminal) {
        $PluginPath = $terminal
    } else {
        Write-Error "No plugin found. Build the solution or pass -PluginPath."
    }
}

if (-not (Test-Path $PluginPath)) {
    Write-Error "Plugin path not found: $PluginPath"
}

Write-Host "Using plugin: $PluginPath"
$env:CONSOLEGAME_PLUGIN_PATH = $PluginPath

# Ensure build first
& dotnet build $appProj -c $Configuration | Write-Host

# Run the app
& dotnet run --project $appProj -c $Configuration

param(
  [string]$PluginPath
)

$ErrorActionPreference = 'Stop'

if (-not $PluginPath) {
  $tfm = 'net9.0'
  $config = 'Debug'
  $root = Split-Path -Parent $PSScriptRoot
  $published = Join-Path $root "artifacts/plugins/ConsoleGame.Dungeon.Plugin/ConsoleGame.Dungeon.Plugin.dll"
  $dungeon = Join-Path $root "src/ConsoleGame.Dungeon.Plugin/bin/$config/$tfm/ConsoleGame.Dungeon.Plugin.dll"
  $term = Join-Path $root "src/ConsoleGame.TerminalLib/bin/$config/$tfm/ConsoleGame.TerminalLib.dll"
  if (Test-Path $published) { $PluginPath = $published }
  elseif (Test-Path $dungeon) { $PluginPath = $dungeon }
  elseif (Test-Path $term) { $PluginPath = $term }
}

if (-not (Test-Path $PluginPath)) { throw "Plugin not found: $PluginPath" }

$env:CONSOLEGAME_PLUGIN_PATH = (Resolve-Path $PluginPath).Path

dotnet build "$PSScriptRoot/../src/ConsoleGame.App/ConsoleGame.App.csproj" -warnaserror
dotnet run --project "$PSScriptRoot/../src/ConsoleGame.App/ConsoleGame.App.csproj"

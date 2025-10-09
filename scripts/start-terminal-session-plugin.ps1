param(
    [ValidateSet("start", "stop")]
    [string]$Action = "start"
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$consoleGameDir = Join-Path $repoRoot "dotnet\ConsoleGame"
$nodeDir = Join-Path $repoRoot "scripts\nodejs"
$pluginPath = Join-Path $consoleGameDir "src\ConsoleGame.Terminal.Plugin\bin\Debug\net8.0\ConsoleGame.Terminal.Plugin.dll"

function Stop-TerminalSession {
    Write-Host "Stopping ConsoleGame terminal processes..." -ForegroundColor Yellow

    $processNames = @(
        "ConsoleGame.App",
        "ConsoleGame.TerminalHost",
        "ConsoleGame.Terminal.Client.Cli",
        "ConsoleGame.Terminal.Client.Tui",
        "dotnet",
        "node"
    )

    foreach ($name in $processNames) {
        $procs = Get-Process -Name $name -ErrorAction SilentlyContinue
        if ($procs) {
            foreach ($proc in $procs) {
                try {
                    Write-Host "Stopping $($proc.ProcessName) (PID: $($proc.Id))" -ForegroundColor DarkGray
                    $proc | Stop-Process -Force -ErrorAction SilentlyContinue
                }
                catch {
                    Write-Warning "Failed to stop process $($proc.ProcessName) (PID: $($proc.Id)): $_"
                }
            }
        }
    }

    if (Test-Path Env:NODE_OPTIONS) {
        Remove-Item Env:NODE_OPTIONS -ErrorAction SilentlyContinue
        Write-Host "Cleared NODE_OPTIONS environment variable." -ForegroundColor DarkGray
    }

    Write-Host "All terminal processes stopped." -ForegroundColor Green
}

if ($Action -eq "stop") {
    Stop-TerminalSession
    return
}

Write-Host "Preparing ConsoleGame plugin-based terminal session startup..." -ForegroundColor Cyan

# Stop any lingering processes before launching fresh ones.
Stop-TerminalSession

# Ensure cross-env dependency is installed for the web workspace.
if (-not (Test-Path $nodeDir)) {
    throw "Node workspace directory not found: $nodeDir"
}

Write-Host "Ensuring web workspace dependencies are installed (this may take a moment)..." -ForegroundColor Cyan
Start-Process pnpm.cmd -ArgumentList "install","--filter","@pinto-bean/main-site..." -WorkingDirectory $nodeDir -NoNewWindow -Wait

# Launch web dev server
Write-Host "Starting web dev server (Next.js, port 3002)..." -ForegroundColor Cyan
$webCommand = "Set-Location `"$nodeDir`"; `$env:NODE_OPTIONS='--openssl-legacy-provider'; pnpm.cmd --filter main-site dev"
Start-Process powershell -ArgumentList '-NoProfile','-Command', $webCommand

Start-Sleep -Seconds 2

# Launch transport plugin (includes WebSocket server and named pipe server)
Write-Host "Starting ConsoleGame.App with Terminal Transport Plugin..." -ForegroundColor Cyan
$transportCommand = "Set-Location `"$consoleGameDir`"; dotnet run --project src\ConsoleGame.App -- --plugin `"$pluginPath`""
Start-Process powershell -ArgumentList '-NoProfile','-Command', $transportCommand

Start-Sleep -Seconds 2

# Optional: Launch CLI client plugin in separate window
Write-Host "Starting CLI client (optional - use for debugging)..." -ForegroundColor DarkCyan
$cliCommand = "Set-Location `"$consoleGameDir`"; dotnet run --project src\ConsoleGame.App -- --plugin `"$pluginPath`" --mode cli"
Start-Process powershell -ArgumentList '-NoProfile','-Command', $cliCommand

Start-Sleep -Seconds 1

# Optional: Launch TUI client plugin in separate window
Write-Host "Starting TUI client (optional - use for debugging)..." -ForegroundColor DarkCyan
$tuiCommand = "Set-Location `"$consoleGameDir`"; dotnet run --project src\ConsoleGame.App -- --plugin `"$pluginPath`" --mode tui"
Start-Process powershell -ArgumentList '-NoProfile','-Command', $tuiCommand

Start-Sleep -Seconds 1

Write-Host "All processes launched." -ForegroundColor Green
Write-Host "Web UI:     http://localhost:3002/terminal" -ForegroundColor Green
Write-Host "WebSocket:  ws://localhost:4040" -ForegroundColor Green
Write-Host "Named Pipe: console-game-terminal" -ForegroundColor Green
Write-Host "Use 'powershell -File scripts\start-terminal-session-plugin.ps1 stop' to cleanly shut down."

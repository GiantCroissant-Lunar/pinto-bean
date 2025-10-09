param(
    [ValidateSet("start", "stop")]
    [string]$Action = "start"
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$consoleGameDir = Join-Path $repoRoot "dotnet\ConsoleGame"
$nodeDir = Join-Path $repoRoot "scripts\nodejs"

function Stop-TerminalSession {
    Write-Host "Stopping ConsoleGame terminal processes..." -ForegroundColor Yellow

    $processNames = @(
        "ConsoleGame.TerminalHost",
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

Write-Host "Preparing ConsoleGame terminal session startup..." -ForegroundColor Cyan

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

# Launch terminal host
Write-Host "Starting ConsoleGame.TerminalHost..." -ForegroundColor Cyan
$hostCommand = "Set-Location `"$consoleGameDir`"; dotnet run --project src\ConsoleGame.TerminalHost\ConsoleGame.TerminalHost.csproj"
Start-Process powershell -ArgumentList '-NoProfile','-Command', $hostCommand

Start-Sleep -Seconds 1

# Launch CLI client
Write-Host "Starting ConsoleGame.Terminal.Client.Cli..." -ForegroundColor Cyan
$cliCommand = "Set-Location `"$consoleGameDir`"; dotnet run --project src\ConsoleGame.Terminal.Client.Cli\ConsoleGame.Terminal.Client.Cli.csproj"
Start-Process powershell -ArgumentList '-NoProfile','-Command', $cliCommand

Start-Sleep -Seconds 1

# Launch TUI client
Write-Host "Starting ConsoleGame.Terminal.Client.Tui..." -ForegroundColor Cyan
$tuiCommand = "Set-Location `"$consoleGameDir`"; dotnet run --project src\ConsoleGame.Terminal.Client.Tui\ConsoleGame.Terminal.Client.Tui.csproj"
Start-Process powershell -ArgumentList '-NoProfile','-Command', $tuiCommand

Start-Sleep -Seconds 1

Write-Host "All processes launched."
Write-Host "Web UI:     http://localhost:3002/terminal" -ForegroundColor Green
Write-Host "Use 'powershell -File scripts\start-terminal-session.ps1 stop' to cleanly shut down."

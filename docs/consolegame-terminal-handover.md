# ConsoleGame Terminal Handover

## Purpose

This document summarizes the current state of the ConsoleGame live-terminal integration and provides the steps required for the next engineer (focused on `ConsoleGame.App`) to continue the workstream without guesswork.

## Current Architecture Overview

* **Backend bridge:** `dotnet/ConsoleGame/ConsoleGame.TerminalHost/` (new .NET 8 console project using SuperSocket) exposes a WebSocket endpoint. Each client session launches a dedicated `ConsoleGame.App` process and streams stdin/stdout over the socket.
* **Frontend terminal:** `scripts/nodejs/apps/main-site/src/pages/terminal.astro` renders an `xterm.js` console via `src/scripts/terminal-client.ts`. Users connect manually with Connect/Disconnect buttons. Styling lives in `src/styles/terminal-page.css`.
* **Default WebSocket URL:** `ws://localhost:4040` (overridable with `PUBLIC_TERMINAL_WS_URL` at build/runtime).

## Backend Responsibilities (`ConsoleGame.App` owner)

1. **Maintain executable discoverability**
   * Terminal host resolves the executable via `CONSOLE_GAME_EXE` env var, otherwise probes `src/ConsoleGame.App/bin/Debug/net8.0/ConsoleGame.App(.exe)` relative to the repository root.
   * Ensure `ConsoleGame.App` builds cleanly with `dotnet build ConsoleGame.sln` so the host finds the binary.
2. **Process lifecycle hooks**
   * Any graceful shutdown or cleanup logic in `ConsoleGame.App` should run on STDIN EOF or cancellation; the WebSocket host kills the process when the session ends.
   * If plugins/DLLs are hot-loaded, verify they still resolve when the process runs under the host’s working directory (`ConsoleGame.TerminalHost` process directory or override via env vars).
3. **Logging & diagnostics**
   * Console output is streamed directly into the browser terminal. Keep log verbosity manageable. Consider routing verbose logs to files instead of STDOUT if it becomes noisy.
4. **UX adjustments**
   * Provide clear prompts/instructions on launch so web users know how to start interacting (e.g., main menu, keyboard shortcuts).
   * If specific keybindings are essential, document them in the UI copy or expose `/help` command.

## Running the Bridge Locally

```powershell
# From repository root
cd dotnet/ConsoleGame

# Optional: override defaults
$env:CONSOLE_GAME_WS_PORT = "4040"   # change port if needed
$env:CONSOLE_GAME_EXE = "<absolute path to ConsoleGame.App.exe>"
$env:CONSOLE_GAME_ARGS = ""          # pass CLI args if necessary

# Start the WebSocket host
dotnet run --project ConsoleGame.TerminalHost
```

* Default port: `4040`. Change via `CONSOLE_GAME_WS_PORT` if port conflicts occur.
* Each new browser session spawns its own child process; monitor resource usage if multiple sessions connect.
* Named pipe transport is exposed via `CONSOLE_GAME_PIPE_NAME` (defaults to `console-game-terminal`). Local clients can attach without interfering with browser sessions.

### Local CLI client

```powershell
# With the host already running
cd dotnet/ConsoleGame
$env:CONSOLE_GAME_PIPE_NAME = "console-game-terminal" # optional override

dotnet run --project src/ConsoleGame.Terminal.Client.Cli
```

* Streams STDOUT/STDERR/control frames in the console.
* Any input you type (press Enter to send) is forwarded to the shared session.

### Terminal.Gui TUI client

```powershell
# In a new terminal while the host is running
cd dotnet/ConsoleGame
$env:CONSOLE_GAME_PIPE_NAME = "console-game-terminal" # optional override

dotnet run --project src/ConsoleGame.Terminal.Client.Tui
```

* Presents an `Application.Init` window that mirrors the same output the browser sees.
* Type into the bottom input field or use `_Send`; keystrokes are sent over the named pipe to the shared process.

### End-to-end validation

1. Start the host (`ConsoleGame.TerminalHost`) as above.
2. Launch the CLI and/or TUI clients to verify local control works.
3. Open the browser front-end (`scripts/nodejs/apps/main-site`, run `pnpm --filter main-site dev`, navigate to `/terminal`).
4. Confirm all clients see the same output and can issue commands concurrently.

### Automated transport tests

```powershell
cd dotnet/ConsoleGame
dotnet test tests/ConsoleGame.Terminal.Transport.Tests/ConsoleGame.Terminal.Transport.Tests.csproj
```

* Covers frame codec round-trips and multiplexer broadcast/STDIN forwarding.

## Frontend Touchpoints

* Terminal page route: `http://localhost:3002/terminal` when running `pnpm --filter main-site dev`.
* The terminal client sends raw keystrokes; ensure `ConsoleGame.App` handles individual characters as expected (e.g., Enter = `\n`).
* Browser fetches WebSocket URL using `PUBLIC_TERMINAL_WS_URL` env var. To point at a remote host, export that variable before building or use `.env` in Astro.

## Coordination Notes / Next Steps

* **For the ConsoleGame engineer:**
  * Verify the game boots correctly via the WebSocket host.
  * Implement any gameplay/menu adjustments needed for web sessions.
  * Harden plugin loading logic so runtime DLL changes work when launched from the host.
* **For the frontend/infrastructure engineer (already notified):**
  * Add authentication/rate limiting if the terminal will be public.
  * Write deployment docs (see TODO `todo-49` and `todo-53`).

## Reference

* Backend entrypoint: `dotnet/ConsoleGame/ConsoleGame.TerminalHost/Program.cs`
* Terminal client script: `scripts/nodejs/apps/main-site/src/scripts/terminal-client.ts`
* Astro terminal page: `scripts/nodejs/apps/main-site/src/pages/terminal.astro`
* Stylesheet: `scripts/nodejs/apps/main-site/src/styles/terminal-page.css`

Keep this document updated as further integration steps are completed.

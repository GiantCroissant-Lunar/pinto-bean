# Adopt named-pipe mediated multi-client terminal architecture for ConsoleGame

* Status: proposed
* Date: 2025-09-28
* Decision-makers: @GiantCroissant-Lunar, @Cascade
* Technical Story: Enable ConsoleGame to serve multiple terminal clients (browser, local CLI/TUI, future MCP integrations) concurrently through a transport-agnostic mediator.

## Context and Problem Statement

`ConsoleGame.TerminalHost` currently exposes a single WebSocket bridge that spawns a headless `ConsoleGame.App` process and streams STDIN/STDOUT to a browser `xterm.js` client. This design limits us to one transport at a time and prevents local tooling (e.g., CLI/TUI debugging or automation agents) from attaching concurrently. The `TerminalEchoSample` proof-of-concept highlighted the need to separate UI clients from the core process while allowing shared control.

We need an architecture that:

1. Allows multiple simultaneous clients (browser, local console, future integrations) to observe and control the same game session.
2. Keeps the game executable headless so transports can plug in without rendering conflicts.
3. Uses a cross-platform IPC mechanism for on-box clients without requiring the game process to own the console handle.
4. Supports future extensibility (e.g., MCP server, replay recorder) without rewriting transport plumbing.

## Decision Drivers

* Maintainability and testability of the terminal stack.
* Ability to add/remove client types with minimal coupling.
* Cross-platform support (Windows, macOS, Linux) for local tooling.
* Observability and debugging ergonomics during development.
* Security: single process retains execution rights while mediating textual streams.

## Considered Options

1. **Status quo**: Keep a single WebSocket bridge tied directly to the child process.
2. **Direct Terminal.Gui hybrid**: Run the console app with Terminal.Gui and bolt on websocket mirroring via hooks inside the UI.
3. **Named-pipe mediated hub** (**chosen**): Introduce a dedicated mediator that owns the process pipes, exposes WebSocket and named-pipe connectors, and fans out I/O to clients.

## Decision Outcome

Chosen option: **Named-pipe mediated hub**.

### Rationale

* Dedicated mediator cleanly separates transport concerns from game logic.
* `System.IO.Pipes.NamedPipeServerStream` provides cross-platform, low-latency IPC for local tooling and future automation agents.
* The mediator can track client presence, provide logging/tracing, and enforce backpressure or auth policies consistently across transports.
* Migrating ConsoleGame to this architecture now reduces risk when adding additional client types (e.g., MCP server, terminal record/replay) later.

### Positive Consequences

* Multiple clients (browser `xterm.js`, local CLI, future TUI) can attach simultaneously and share the same session.
* Transport-agnostic design: adding a new client means implementing a connector instead of modifying the game or mediator core.
* Improved developer ergonomics: local troubleshooting via CLI/TUI while observing remote sessions, with the Terminal.Gui TUI remaining the primary experience.
* Easier integration testing by scripting named-pipe clients.

### Negative Consequences

* Additional complexity in the mediator (client registry, fan-out, synchronization).
* Need to manage lifecycle of named pipes and WebSocket sessions carefully to avoid resource leaks.
* Higher testing surface area (simultaneous client scenarios, race conditions on STDIN writes).
* More moving pieces to secure (authn/z for each connector, potential elevation of local IPC endpoints).

## Implementation Plan

1. **Extract headless terminal core**
   * Move reusable game/terminal logic into `ConsoleGame.TerminalLib` (or a new `ConsoleGame.TerminalCore`).
   * Provide a headless entrypoint that reads/write plain text via STDIN/STDOUT (no `Terminal.Gui`).

2. **Refactor existing executables**
   * Update `ConsoleGame.App` to delegate to the headless core.
   * Keep CLI/TUI entrypoints (if desired) as thin wrappers that connect through the mediator rather than owning the process pipes directly.

3. **Enhance mediator host**
   * Extend `dotnet/ConsoleGame/src/ConsoleGame.TerminalHost/` to expose:
     * WebSocket connector (reuse existing `SuperSocket` implementation).
     * Named pipe server connector with multiple client support.
   * Implement a shared session manager that:
     * Launches the headless process with redirected STDIN/STDOUT/STDERR.
     * Broadcasts output to all connected clients (WebSocket sessions and pipe clients).
     * Serializes inbound input and writes to the process using a guarded queue.
     * Handles client join/leave, idle timeout, and graceful shutdown (`Ctrl+C`, EOF).

4. **Create primary and auxiliary clients**
   * Adapt the existing Terminal.Gui presentation in `ConsoleGame.TerminalLib` to operate as a named-pipe client (the canonical first-class UI).
   * Provide a simple CLI client (e.g., `ConsoleGame.TerminalCli`) that connects to the named pipe for developers and automated tooling.

5. **Observability & diagnostics**
   * Add structured logging around session creation, client connections, and IO events.
   * Consider tracing hooks for future replay or metrics exporters.

6. **Security & configuration**
   * Define environment variables / config for named pipe names, authentication strategy, and client limits.
   * Document operational procedures and update `docs/consolegame-terminal-handover.md`.

7. **Testing & rollout**
   * Write automated tests for the mediator (simulated clients, fan-out behavior, backpressure).
   * Validate cross-platform behavior of named pipes (Windows named pipes vs Unix domain sockets).
   * Stage rollout: terminal echo prototype → ConsoleGame dev builds → production.

## Consequences for Documentation & Tooling

* Update ADR index / Log4Brains catalog to include this decision.
* Refresh developer docs with new local development flows (attach via CLI or TUI pipe client).
* Ensure CI builds new client binaries and runs mediator tests.

## Status and Follow-up Actions

* This ADR must be accepted before refactoring work begins.
* Once accepted, execute the implementation plan and capture lessons learned in subsequent ADRs or RFCs if adjustments are needed.

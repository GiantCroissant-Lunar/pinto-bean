# Enforce JS and .NET formatting in pre-commit

- Status: accepted
- Date: 2025-09-26
- Tags: tooling, linting, quality, pre-commit

Technical Story: pre-commit hook configuration for Biome, dotnet-format, ReSharper cleanupcode

## Context and Problem Statement

We recently expanded our codebase with multiple JavaScript/TypeScript workspaces and several .NET solutions. The existing pre-commit configuration only covered Python and repository hygiene checks. Developers were introducing inconsistent formatting in frontend and C# projects because there was no automated enforcement before commits. How do we guarantee consistent linting and formatting across Node.js and .NET code without relying on manual vigilance?

## Decision Drivers

- Reduce reviewer time spent on style-related feedback for JS/TS and C# changes.
- Keep the pre-commit experience within acceptable run times for contributors on Windows.
- Reuse existing toolchains (pnpm workspace, dotnet SDK, ReSharper CLI) instead of introducing new language-specific formatters.
- Provide graceful fallbacks when tooling is unavailable so hooks do not block unrelated work.

## Considered Options

- Do nothing and continue relying on reviewers or CI to catch formatting drift.
- Apply prettier/eslint and dotnet-format manually but do not wire them into pre-commit.
- Integrate Biome, dotnet format, and ReSharper cleanupcode into pre-commit with custom wrappers.

## Decision Outcome

Chosen option: "Integrate Biome, dotnet format, and ReSharper cleanupcode into pre-commit with custom wrappers", because this is the only option that consistently enforces formatting across both ecosystems while leveraging the tools we already adopted. The custom wrappers allow the hooks to short-circuit when prerequisites are missing, keeping the developer experience predictable.

### Positive Consequences

- JS/TS/JSON, .csproj, and C# source formatting issues are corrected before commits, reducing churn in code reviews.
- A single command (`pre-commit run --all-files`) now validates Python, Node, and .NET style expectations, improving local parity with CI.
- Wrappers centralise environment detection (pnpm availability, solution grouping, ReSharper CLI lookup), so future hook adjustments are confined to one place.

### Negative Consequences

- Pre-commit runtime increases slightly, especially when dotnet-format or ReSharper needs to scan large solutions.
- Contributors must install pnpm workspace dependencies and the JetBrains CLI locally; otherwise the hooks skip silently, which could mask configuration drift if teams forget to install them.
- We must maintain the Python wrapper scripts when tool CLIs or command-line arguments evolve.

## Pros and Cons of the Options

### Do nothing

- Good, because no additional setup or hook runtime is introduced.
- Bad, because formatting issues keep showing up in reviews and CI, creating avoidable rework.

### Manual formatting only

- Good, because developers can opt-in to formatting when needed.
- Bad, because compliance depends on individual discipline and inevitable misses lead to inconsistent style.

### Pre-commit integration (selected)

- Good, because formatting is enforced automatically for every staged change.
- Good, because wrappers provide resilience on Windows (path normalisation, pnpm resolution) and respect staged scopes.
- Bad, because hook logic is more complex and requires ongoing maintenance.

## Links

- Implemented in commit `9eff4cf` (chore: expand lint/format hooks).
- Relates to the Biome introduction in `scripts/nodejs/package.json` and pre-commit configuration updates (`.pre-commit-config.yaml`).

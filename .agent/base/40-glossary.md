# Glossary

Edge test: Test covering null/empty, boundary numeric values, or permission‑denied outcomes.
Hot path: Code executed in >5% of typical request cycles (instrumentation pending).
Modified lines: Sum of additions and deletions from `git diff --numstat` for a PR.
Cross‑cutting refactor: Changes spanning ≥ 3 modules/packages or affecting shared public APIs.
Public API surface: Any public types/methods/events that downstream code can consume.
Quarantine test: Failing or flaky test marked to skip in CI while tracked by an issue.
Pre‑approved dependency: Dev‑only tool listed in `/docs/preapproved-deps.md` exempt from R‑PRC‑030 prompt.

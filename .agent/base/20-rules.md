# Normative Rules (Canon)

(Use: Adapters cite “R-CODE-010” etc.)

## Process
R-PRC-010: A single PR should target one logical concern.
R-PRC-020: Must ask user before large/ cross‑cutting changes. Trigger if **any**:
  - (a) `added + deleted > 200` lines (from `git diff --numstat`) **and** more than 5 files changed; or
  - (b) edits span ≥ 3 top‑level modules/packages; or
  - (c) rename/move of public packages or shared libraries; or
  - (d) any behavior‑changing refactor without tests.
R-PRC-030: Do not introduce new dependencies without explicit user confirmation. (Pre‑approved dev‑only deps may be listed in `/docs/preapproved-deps.md`.)
R-PRC-040: Do not auto‑upgrade major versions.
R-PRC-050: Must request confirmation before deleting > 50 lines net.
R-PRC-060: Must request confirmation before altering CI pipeline logic. PR description MUST include: risk note, rollout plan, and rollback steps.
R-PRC-070: Do not duplicate full rule text in adapters; adapters cite rule IDs only.
R-PRC-080: Never renumber or reuse a retired rule ID; create a new ID for semantic changes.
R-PRC-090: On semantic change to a rule, mark old rule **DEPRECATED** verbatim and add a new rule with a new ID; bump VERSION accordingly.
R-PRC-100: Policy changes happen only in base canon files (not solely in adapters).
R-PRC-110: Keep speculative/roadmap ideas out of normative rules; place them in non‑normative docs.

## Code & Architecture
R-CODE-010: New functions require at least one happy‑path and one edge test.
R-CODE-020: Avoid creating new top‑level modules if a suitable existing module exists.
R-CODE-030: No large style‑only rewrites; piggyback formatting only on functional edits.
R-CODE-040: Keep public API backward‑compatible unless user approves the break.
R-CODE-050: Do not fabricate file contents; if unsure, request clarification.

## Security
R-SEC-010: Never log, echo, or invent secrets; use `<REDACTED>` placeholder.
R-SEC-020: Do not embed credentials/tokens in examples.
R-SEC-030: External call stubs must be mocked in tests (no real network unless explicitly allowed).
R-SEC-040: Do not log PII; redact sensitive fields by default.
R-SEC-050: External calls must define timeout and retry/backoff policy in code; tests must stub them.
R-SEC-060: PRs touching authentication, authorization, or billing must not add live integration tests by default; use fixtures/sandboxes.

## Testing
R-TST-010: Flaky tests are quarantined (mark and raise issue) not deleted.
R-TST-020: New code paths altering data shape require regression tests.
R-TST-030: Avoid live integration tests for auth or billing; use fixtures.
R-TST-040: Non‑trivial behavior changes must update or add tests that cover the changed behavior (even if no new functions were added).

## Performance
R-PERF-010: Flag O(n^2) operations if n could exceed 10k; propose alternatives.
R-PERF-020: Provide a before/after note if optimizing a known hot path.

## Documentation
R-DOC-010: Public API changes must update the CHANGELOG stub.
R-DOC-020: Do not inline large spec tables; link to existing docs.
R-DOC-030: Documents must follow naming conventions:
  - RFCs: `rfcs/rfc-xxxx-title.md` (lowercase, 4‑digit number).
  - ADRs: `adr/000x-title.md` (lowercase, 4‑digit number).
  - PRDs: `prd/YYYY-MM-DD-title.md`.
R-DOC-040: RFCs must include frontmatter with at least: `id` (4‑digit number), `title`, `status` (Draft/Accepted/Rejected/Postponed), `category` (one or more of: gameplay, infra, web, publishing, docs, tooling). Labels optional.

## Git
R-GIT-010: Commit bodies MUST be authored from a file and passed via `git commit -F <file>`.
  - Do not include literal backslash‑escaped newlines (e.g., `
`) in `-m` arguments.
  - Subject line ≤ 72 chars; then a blank line; then Markdown body.
  - For non‑trivial changes, the commit body SHOULD live at `changes/unreleased/<yyyy-mm-dd>-<slug>.md` and be added to the commit.

## Deprecated Rules
(None yet)

# Pre-Commit Framework Overview

This document describes the pre-commit setup used in the `pinto-bean` repository: tooling goals, hook inventory, how to run/maintain it, and troubleshooting strategies.

## Goals

1. Catch quality, security, and style issues before they reach CI.
2. Provide consistent cross-platform developer experience (Windows / macOS / Linux).
3. Avoid duplicated secret scanning or redundant tool executions.
4. Keep configuration declarative and easily upgradable.

## High-Level Architecture

```
.git/hooks/pre-commit (Git config points to .githooks/) --> custom shell launcher
  |-- runs PowerShell helper (if available) or Python validator fallback
  |-- then chains into pre-commit framework (.pre-commit-config.yaml)
```

Secret scanning is **layered**:
- Custom validator (`scripts/python/validate_secrets.py`): heuristics (entropy, template enforcement, age key safety). Does not run gitleaks/detect-secrets unless `LEGACY_FULL_SCAN=1`.
- Framework hooks: `gitleaks`, `detect-secrets` (baseline enforced).
- CI: Executes `pre-commit run --all-files` plus the validator; publishes SARIF (secret scan) if configured.

## Installation

Initial local setup:

```bash
pip install -r scripts/python/requirements-dev.txt
pre-commit install --hook-type pre-commit --hook-type commit-msg
git config core.hooksPath .githooks
```

Upgrade all hooks to latest allowed versions:

```bash
pre-commit autoupdate
```

(Commit the updated `.pre-commit-config.yaml` after review.)

## Hook Inventory

| Category | Hook | Purpose | Notes |
|----------|------|---------|-------|
| Core Hygiene | check-merge-conflict | Prevent unresolved conflicts | `pre-commit-hooks` |
| Core Hygiene | end-of-file-fixer | Ensure newline EOF |  |
| Core Hygiene | trailing-whitespace | Trim trailing spaces |  |
| Core Hygiene | mixed-line-ending | Normalize line endings | Helps cross-platform |
| Secrets | detect-private-key | Catch accidentally added private keys |  |
| Hygiene | check-added-large-files | Block very large blobs | 10 MB threshold |
| Repo Safety | forbid-new-submodules | Disallow submodule additions |  |
| YAML | check-yaml | Basic YAML parse validation |  |
| YAML | yamllint | Style/consistency for YAML | Line length disabled |
| Secrets | gitleaks | Signature-based secret scanning | Redact mode; staging focus |
| Secrets | detect-secrets | Baseline diff scanning | Requires `.secrets.baseline` |
| Commit Convention | commitizen | Enforce commit message format | `commit-msg` stage |
| .NET | dotnet-format | Ensure C# formatting | No changes allowed |
| Unity | unity-meta-check | Ensure `.meta` files exist | Custom local Python hook |
| Unity | unity-packages-lock-check | Manifest vs lock sync | Custom local Python hook |
| Unity / Assets | lfs-enforce | Enforce Git LFS for large binaries | Custom local Python hook |
| Unity | block-library-temp-obj-build | Block generated folders | Shell one-liner |
| Binary Safety | binary-blocker | Prevent compiled binaries | Shell one-liner |
| Python | ruff | Lint + autofix (selected categories) | Runs before formatting |
| Python | ruff-format | Code formatting (PEP 8 + style) | Idempotent |
| Python | mypy | Static type checking | Targets `scripts/python` |

## Secret Scanning Details

1. `.secrets.baseline` manages accepted findings for `detect-secrets`.
2. Gitleaks runs with redact to minimize exposure of candidate secrets.
3. Custom validator prevents false positives explosion by filtering tokens that look like paths, placeholders, or trivial repetitions.
4. To run a legacy full scan alone (bypassing framework) via QA script:

```bash
pwsh scripts/qa.ps1 -Step secrets -LegacyFullScan
```

## Running Manually

Run all hooks against every file:

```bash
pre-commit run --all-files
```

Run a single hook (example):

```bash
pre-commit run ruff --all-files
```

Bypass (temporary – discouraged):

```bash
git commit -m "msg" --no-verify
```

(Use only for emergency fixes; follow with a normal commit that passes.)

## CI Integration

The workflow job named `Pre-commit Quality Gate` runs:
1. Dependency install (`scripts/python/requirements-dev.txt`).
2. `pre-commit run --all-files`.
3. Redundant explicit `ruff check` (defense-in-depth) – optional.
4. Caches pre-commit environments and tool caches.

The `tests` job executes pytest with coverage, leveraging cached `.pytest_cache`, `.mypy_cache`, and `.ruff_cache`.

The `terraform` job depends on both quality and test jobs, ensuring policy gates before infrastructure changes.

## Maintenance Tasks

| Task | Command | Notes |
|------|---------|-------|
| Update hook versions | `pre-commit autoupdate` | Review diffs for new rules |
| Refresh detect-secrets baseline | `detect-secrets scan > .secrets.baseline` | Commit the updated baseline |
| Add new Python dependency for hooks | Add to `scripts/python/requirements-dev.txt` | Keep versions pinned |
| Increase coverage threshold | Edit `[tool.coverage.report] fail_under` in `pyproject.toml` | Adjust gradually |
| Add new hook | Modify `.pre-commit-config.yaml` + run `pre-commit run --all-files` | Ensure ordering makes sense |

## Ordering Rationale

1. Fast structural & hygiene hooks run early (cheap failures first).
2. Secrets scanning occurs before expensive static analysis to surface critical security issues promptly.
3. Formatting/linting (Ruff) before mypy to reduce noise from style problems.
4. Type checking (mypy) runs last to catch deeper issues after code style/format is clean.

## Performance Tips

- Enable caching: already done in CI; locally rely on pre-commit's environment reuse.
- Run selective hooks during iterative development: `pre-commit run ruff`.
- Use `scripts/qa.ps1 -Step ruff,mypy,tests` for a focused loop.

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| Hook not running | Not installed or wrong hooksPath | Re-run install commands |
| detect-secrets always fails | Baseline missing or outdated | Regenerate baseline |
| gitleaks false positives | Signature collision | (Optionally) add regex allowlist or refine config (reintroduce `.gitleaks.toml`) |
| Ruff format changes on CI | Local env outdated | Reinstall deps; run `ruff format .` |
| Mypy cannot find module | Missing dependency types | Add to requirements or `types-` stub package |

## When to Re-Introduce `.gitleaks.toml`

If encrypted template files or known benign tokens begin generating noise, create a minimal `.gitleaks.toml` with only required allowlist paths rather than broad patterns.

## Future Enhancements

- Add Bandit or integrate security subset of Ruff rules (many already covered).
- Introduce conventional commits enforcement in PR titles (optional).
- Add a nightly job to run `pre-commit autoupdate` and open a PR automatically.

## Quick Reference

```bash
# Run everything (all hooks)
pre-commit run --all-files

# Update hooks
pre-commit autoupdate

# QA script full suite
pwsh scripts/qa.ps1

# Focused test + type + lint
pwsh scripts/qa.ps1 -Step ruff,mypy,tests
```

---
Maintained by Infrastructure & Tooling owners. Keep this document updated when hook set changes.

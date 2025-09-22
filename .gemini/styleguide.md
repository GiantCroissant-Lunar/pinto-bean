# Pinto-Bean Repository Review Guide

This style guide augments Gemini's default review focus areas with project-specific expectations.

## Languages & Stacks
- Python (tooling, hooks, validation)
- PowerShell (infrastructure & QA scripts)
- Terraform (infrastructure as code)
- Unity / C# (game and engine-related code; formatting handled externally for now)

## General Principles
1. Security & secret hygiene first.
2. Fail fast: small, actionable findings over broad refactors.
3. Prefer configuration over custom ad-hoc scripts (keep logic centralized).
4. Consistency across scripting languages (naming, error handling, explicit exits).
5. Minimize noisy / low-value comments.

## Python Guidelines
- Use Ruff & Mypy baselines; only flag issues not already enforced unless they materially affect correctness or security.
- Avoid suggesting style changes already covered by Ruff configuration (line length 100, E203/E501 ignored, formatting delegated to ruff format).
- Prefer explicit errors over silent pass-by exceptions.
- Secret scanning tooling (`validate_secrets.py`, `validator_core.py`) should keep deterministic output; avoid recommendations that add non-deterministic entropy thresholds without justification.

## PowerShell Guidelines
- Scripts should use `Set-StrictMode -Version Latest` and `$ErrorActionPreference = 'Stop'` when complexity grows.
- Functions should have `param()` blocks; flag missing parameter validation only if it creates real risk.
- Encourage named parameter invocation for clarity (already adopted in `qa.ps1`).

## Terraform Guidelines
- Validate logical drift risks (missing validation steps, unpinned providers, absent `required_version`).
- Do not request formatting changes; rely on `terraform fmt`.
- Suggest additional validation (e.g., input variable validation blocks) if missing for critical variables.

## Unity / C# Guidelines
- Skip stylistic C# recommendations unless correctness, performance, or safety is impacted.
- Do not flag generated or engine folders (`Library/`, `Temp/`, `Obj/`, `Build/`).

## Secret Hygiene
- Do not duplicate findings already reported by `gitleaks` or `detect-secrets` unless severity escalates (e.g., exposed private key).
- Treat placeholder tokens (`<...>`, `CHANGEME`, `example`) as benign unless they escape intended templates.

## Commit & Documentation
- Only comment on commit message formatting if it breaks conventional commits (enforced by commitizen) or obscures intent.
- Flag outdated README statements if diverging from CI workflows or tooling behavior.

## Noise Reduction
Gemini should avoid:
- Recommending enabling hooks deliberately removed (e.g., `dotnet-format` before a solution is committed).
- Redundant security warnings about excluded build artifacts.
- Reformatting suggestions already auto-managed.

## Review Priorities (Descending)
1. Security / secrets exposure
2. Functional correctness / reliability
3. Broken or misleading documentation / configs
4. Maintainability (dead code, duplication, missing validation)
5. Performance (only if non-trivial or likely impact)
6. Style (only if causes ambiguity or error risk)

## When Unsure
Prefer asking a clarifying question rather than proposing speculative changes.

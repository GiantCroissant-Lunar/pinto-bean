#!/usr/bin/env pwsh
<#!
.SYNOPSIS
  Consolidated quality gate runner (cross-platform PowerShell).
.DESCRIPTION
  Runs selected or all quality checks: pre-commit hooks, ruff lint/format check, mypy, secret validator, terraform fmt/validate.
.PARAMETER Step
  Comma-separated subset of steps to run (default: all). Recognized keys:
    precommit, ruff, mypy, secrets, terraform, format
.PARAMETER Fix
  If set, allows in-place formatting (ruff format, terraform fmt without -check).
.PARAMETER LegacyFullScan
  If set, enables legacy full secret scan (detect-secrets & gitleaks) via validator env.
.EXAMPLES
  pwsh scripts/qa.ps1
  pwsh scripts/qa.ps1 -Step precommit,ruff,mypy
  pwsh scripts/qa.ps1 -Step secrets -LegacyFullScan
  pwsh scripts/qa.ps1 -Fix
.NOTES
  Requires: pre-commit, ruff, mypy, terraform, python, pwsh.
#>
param(
  [string]$Step = "",
  [switch]$Fix,
  [switch]$LegacyFullScan
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Resolve-Path "$PSScriptRoot/.." ).Path
Set-Location $RepoRoot

function Write-Section($Title) {
  Write-Host "`n=== $Title ===" -ForegroundColor Cyan
}

$allSteps = @('precommit','ruff','mypy','secrets','terraform','format','tests')
$requested = if ([string]::IsNullOrWhiteSpace($Step)) { $allSteps } else { $Step.Split(',') | ForEach-Object { $_.Trim().ToLower() } }
$invalid = $requested | Where-Object { $_ -notin $allSteps }
if ($invalid) { Write-Error "Unknown step(s): $($invalid -join ', ')"; exit 2 }

function Test-CommandAvailable {
  param(
    [Parameter(Mandatory)][string]$Name
  )
  if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) { Write-Error "$Name not found in PATH"; exit 3 }
}

# Pre-flight: only check commands actually needed
if ($requested -contains 'precommit') { Test-CommandAvailable -Name 'pre-commit' }
if ($requested -contains 'ruff') { Test-CommandAvailable -Name 'ruff' }
if ($requested -contains 'mypy') { Test-CommandAvailable -Name 'mypy' }
if ($requested -contains 'terraform') { Test-CommandAvailable -Name 'terraform' }
if ($requested -contains 'secrets') { Test-CommandAvailable -Name 'python' }
if ($requested -contains 'tests') { Test-CommandAvailable -Name 'python' }

$global:failures = @()
function Invoke-QAStep {
  param(
    [Parameter(Mandatory)][string]$Name,
    [Parameter(Mandatory)][scriptblock]$Block
  )
  if ($requested -contains $Name) {
    Write-Section $Name
    try { & $Block } catch { $global:failures += $Name; Write-Host "[$Name] FAILED: $($_.Exception.Message)" -ForegroundColor Red }
  }
}

Invoke-QAStep -Name 'precommit' -Block { pre-commit run --all-files }

Invoke-QAStep -Name 'ruff' -Block {
  if ($Fix) {
    ruff check --fix .
    ruff format .
  } else {
    ruff check .
    # format is idempotent: run in diff mode by checking for changes
    $before = git diff --name-only
    ruff format .
    if ((git diff --name-only) -ne $before) { Write-Error 'Formatting changes required (re-run with -Fix)' }
  }
}

Invoke-QAStep -Name 'mypy' -Block { mypy scripts/python }

Invoke-QAStep -Name 'secrets' -Block {
  if ($LegacyFullScan) { $env:LEGACY_FULL_SCAN = '1' }
  $env:VALIDATOR_JSON = 'secret-scan.local.json'
  python scripts/python/validate_secrets.py
}

Invoke-QAStep -Name 'terraform' -Block {
  Push-Location infra/terraform/github
  try {
    if ($Fix) { terraform fmt -recursive } else { terraform fmt -check -diff }
    terraform validate
  } finally { Pop-Location }
}

Invoke-QAStep -Name 'format' -Block {
  if (-not $Fix) { Write-Host 'Format step requires -Fix to apply changes' -ForegroundColor Yellow; return }
  ruff format .
  Push-Location infra/terraform/github
  try { terraform fmt -recursive } finally { Pop-Location }
}

Invoke-QAStep -Name 'tests' -Block {
  # Run pytest with coverage (xml + terminal summary); requires pytest + pytest-cov installed.
  if (-not (Get-Command pytest -ErrorAction SilentlyContinue)) { Write-Error 'pytest not installed (pip install pytest pytest-cov)'; return }
  pytest --cov=scripts/python --cov-report=term-missing --cov-report=xml:coverage.xml
  if (-not (Test-Path coverage.xml)) { Write-Error 'coverage.xml not produced' }
}

if ($failures.Count -gt 0) {
  Write-Host "`nFailures: $($failures -join ', ')" -ForegroundColor Red
  exit 1
}
Write-Host "`nAll requested QA steps passed." -ForegroundColor Green

Param()
$ErrorActionPreference = 'Stop'
try { $repoRoot = (git rev-parse --show-toplevel).Trim() } catch { Write-Error "Unable to determine repository root: $($_.Exception.Message)"; exit 1 }
$validator = Join-Path $repoRoot 'scripts' 'python' 'validate_secrets.py'
if (-not (Test-Path $validator)) { Write-Error "Validator script missing at $validator"; exit 1 }
$pythonCmd = $null; foreach ($cand in @('python','python3')) { $cmd = Get-Command $cand -ErrorAction SilentlyContinue; if ($cmd) { $pythonCmd = $cmd.Source; break } }
if (-not $pythonCmd) { Write-Error "Python interpreter not found (python/python3)"; exit 1 }
Write-Host "[pre-commit] Running secrets validator via: $pythonCmd"
& $pythonCmd $validator
$rc = $LASTEXITCODE
if ($rc -ne 0) { Write-Error "[pre-commit] Secrets validation failed (exit $rc)."; exit $rc }
Write-Host "[pre-commit] Secrets validation passed."
exit 0

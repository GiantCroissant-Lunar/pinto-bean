[CmdletBinding()]
param(
  [string]$Source = (Join-Path $PSScriptRoot '..' 'github' 'secrets' 'github-vars.json'),
  [string]$Target = (Join-Path $PSScriptRoot '..' 'github' 'secrets' 'github-vars.json.encrypted'),
  [switch]$TerraformJson,
  [switch]$Force
)
if ($TerraformJson) {
  $Source = (Join-Path $PSScriptRoot '..' 'secrets' 'terraform.json')
  $Target = (Join-Path $PSScriptRoot '..' 'secrets' 'terraform.json.encrypted')
}
if (-not (Get-Command sops -ErrorAction SilentlyContinue)) { throw 'sops CLI not found. Install from https://github.com/mozilla/sops/releases or package manager.' }
if (-not (Test-Path $Source)) { throw "Source file not found: $Source" }
if ((Test-Path $Target) -and -not $Force) { throw "Target already exists: $Target (use -Force to overwrite)" }
& sops --encrypt $Source | Out-File -Encoding utf8 $Target
Write-Host "Encrypted -> $Target" -ForegroundColor Green

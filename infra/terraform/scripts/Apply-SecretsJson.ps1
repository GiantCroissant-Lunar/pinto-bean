<#!
.SYNOPSIS
  Apply Terraform GitHub related variables from .secrets\terraform_github_vars.json into a Terraform Cloud workspace.
.DESCRIPTION
  Reads a JSON object (key->value). For each entry:
   - If key is GITHUB_TOKEN it is stored as env category, sensitive.
   - Keys ending with _SECRET, _TOKEN, _KEY, _PASS, _PASSWORD become sensitive terraform vars.
   - Simple booleans ("true"/"false" case-insensitive), numbers, HCL-looking lists [..] or maps {..} are flagged as HCL.
   - Other values treated as plain strings.

.PARAMETER Organization
  Terraform Cloud organization name.
.PARAMETER Workspace
  Workspace name.
.PARAMETER Path
  Optional path to JSON file (default: ../../.secrets/terraform_github_vars.json; falls back to secret.json for backward compatibility).

.EXAMPLE
  ./Apply-SecretsJson.ps1 -Organization my-org -Workspace pinto-github

.NOTES
  Requires TFC_TOKEN environment variable.
!#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)][string]$Organization,
  [Parameter(Mandatory)][string]$Workspace,
  [string]$Path
)

if (-not $Path) {
  $candidates = @(
    (Join-Path $PSScriptRoot '..' 'secrets' 'github-vars.json.encrypted'),
    (Join-Path $PSScriptRoot '..' 'secrets' 'terraform.json.encrypted'),
    (Join-Path $PSScriptRoot '..' 'secrets' 'terraform.json'),
    (Join-Path $PSScriptRoot '..' 'github' 'secrets' 'github-vars.json.encrypted'),
    (Join-Path $PSScriptRoot '..' 'github' 'secrets' 'terraform-github-vars.json'),
    (Join-Path $PSScriptRoot '..' 'github' 'secrets' 'terraform.json.encrypted'),
    (Join-Path $PSScriptRoot '..' 'github' 'secrets' 'terraform.json'),
    (Join-Path $PSScriptRoot '..' '..' '..' '.secrets' 'terraform-github-vars.json.encrypted'),
    (Join-Path $PSScriptRoot '..' '..' '..' '.secrets' 'terraform-github-vars.json'),
    (Join-Path $PSScriptRoot '..' '..' '..' '.secrets' 'secret.json')
  )
  $Path = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
  if (-not $Path) { throw 'No secrets file found in new or legacy locations.' }
}

if (-not $env:TFC_TOKEN) { throw 'TFC_TOKEN environment variable not set.' }
if (-not (Test-Path $Path)) { throw "Secrets file not found: $Path" }

$rawJson = ''
if ($Path -match '\\.(sops\.json|encrypted(\.json)?)$') {
  if (-not (Get-Command sops -ErrorAction SilentlyContinue)) { throw 'sops CLI required to decrypt encrypted secrets file.' }
  Write-Host "Decrypting SOPS file: $([System.IO.Path]::GetFileName($Path))" -ForegroundColor DarkYellow
  $rawJson = sops -d $Path
} else {
  $rawJson = Get-Content -Raw -Path $Path
}

$secrets = $rawJson | ConvertFrom-Json
$baseUri = 'https://app.terraform.io/api/v2'

$wsResp = Invoke-RestMethod -Method GET -Uri "$baseUri/organizations/$Organization/workspaces/$Workspace" -Headers @{ Authorization = "Bearer $($env:TFC_TOKEN)" }
$workspaceId = $wsResp.data.id
$existing = (Invoke-RestMethod -Method GET -Uri "$baseUri/workspaces/$workspaceId/vars" -Headers @{ Authorization = "Bearer $($env:TFC_TOKEN)" }).data

function UpsertVar {
  param(
    [string]$Key,
    [string]$Value,
    [string]$Category,
    [bool]$Sensitive=$false,
    [bool]$Hcl=$false
  )
  $match = $existing | Where-Object { $_.attributes.key -eq $Key -and $_.attributes.category -eq $Category }
  $body = @{ data = @{ type='vars'; attributes = @{ key=$Key; value=$Value; category=$Category; hcl=$Hcl; sensitive=$Sensitive } } } | ConvertTo-Json -Depth 5
  if ($match) {
    Write-Host "Updating $Category variable '$Key' (hcl=$Hcl sensitive=$Sensitive)" -ForegroundColor Cyan
    Invoke-RestMethod -Method PATCH -Uri "$baseUri/workspaces/$workspaceId/vars/$($match.id)" -Headers @{ Authorization = "Bearer $($env:TFC_TOKEN)"; 'Content-Type'='application/vnd.api+json' } -Body $body | Out-Null
  } else {
    Write-Host "Creating $Category variable '$Key' (hcl=$Hcl sensitive=$Sensitive)" -ForegroundColor Green
    Invoke-RestMethod -Method POST -Uri "$baseUri/workspaces/$workspaceId/vars" -Headers @{ Authorization = "Bearer $($env:TFC_TOKEN)"; 'Content-Type'='application/vnd.api+json' } -Body $body | Out-Null
  }
}

foreach ($prop in ($secrets | Get-Member -MemberType NoteProperty)) {
  if ($prop.Name -in @('sops','TFC_TOKEN')) { continue }
  $key = $prop.Name
  $raw = $secrets.$key
  $value = [string]$raw
  $isSensitive = $false
  $category = 'terraform'
  $isHcl = $false
  if ($key -eq 'GITHUB_TOKEN') { $category = 'env'; $isSensitive = $true }
  elseif ($key -match '(_SECRET|_TOKEN|_KEY|_PASS|_PASSWORD|token)$') { $isSensitive = $true }
  if ($value -match '^(true|false)$' -or $value -match '^[-+]?[0-9]+(\.[0-9]+)?$' -or ($value.Trim().StartsWith('[') -and $value.Trim().EndsWith(']')) -or ($value.Trim().StartsWith('{') -and $value.Trim().EndsWith('}'))) { $isHcl = $true }
  UpsertVar -Key $key -Value $value -Category $category -Sensitive $isSensitive -Hcl $isHcl
}

Write-Host 'Secrets JSON applied.' -ForegroundColor Yellow

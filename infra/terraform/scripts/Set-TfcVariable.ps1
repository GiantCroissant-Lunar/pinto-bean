<#!
.SYNOPSIS
  Create or update a Terraform Cloud workspace variable.

.DESCRIPTION
  Upserts a variable (env or terraform type) in a specified Terraform Cloud workspace using the v2 API.

.PARAMETER Organization
  Terraform Cloud organization name.

.PARAMETER Workspace
  Workspace name.

.PARAMETER Name
  Variable key/name.

.PARAMETER Value
  Variable value.

.PARAMETER Category
  One of: env, terraform. Default: terraform.

.PARAMETER Hcl
  Treat value as HCL (boolean).

.PARAMETER Sensitive
  Treat variable as sensitive (boolean). Sensitive values are write-only.

.EXAMPLE
  ./Set-TfcVariable.ps1 -Organization my-org -Workspace repo-github -Name github_owner -Value GiantCroissant-Lunar

.EXAMPLE
  ./Set-TfcVariable.ps1 -Organization my-org -Workspace repo-github -Name GITHUB_TOKEN -Value abc123 -Category env -Sensitive

.NOTES
  Requires environment variable TFC_TOKEN (user/team/org API token with workspace:read/write).
!#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)][string]$Organization,
  [Parameter(Mandatory)][string]$Workspace,
  [Parameter(Mandatory)][string]$Name,
  [Parameter(Mandatory)][string]$Value,
  [ValidateSet('terraform','env')][string]$Category = 'terraform',
  [switch]$Hcl,
  [switch]$Sensitive
)

if (-not $env:TFC_TOKEN) { throw 'TFC_TOKEN environment variable not set.' }

$baseUri = 'https://app.terraform.io/api/v2'

# Resolve workspace ID
$wsResp = Invoke-RestMethod -Method GET -Uri "$baseUri/organizations/$Organization/workspaces/$Workspace" -Headers @{ Authorization = "Bearer $($env:TFC_TOKEN)" }
$workspaceId = $wsResp.data.id

# List existing variables to check for match
$varsResp = Invoke-RestMethod -Method GET -Uri "$baseUri/workspaces/$workspaceId/vars" -Headers @{ Authorization = "Bearer $($env:TFC_TOKEN)" }
$existing = $varsResp.data | Where-Object { $_.attributes.key -eq $Name }

$body = @{ data = @{ type = 'vars'; attributes = @{ key = $Name; value = $Value; category = $Category; hcl = [bool]$Hcl; sensitive = [bool]$Sensitive } } } | ConvertTo-Json -Depth 6

if ($existing) {
  $varId = $existing.id
  Write-Host "Updating variable '$Name' ($varId)" -ForegroundColor Cyan
  Invoke-RestMethod -Method PATCH -Uri "$baseUri/workspaces/$workspaceId/vars/$varId" -Headers @{ Authorization = "Bearer $($env:TFC_TOKEN)"; 'Content-Type'='application/vnd.api+json' } -Body $body | Out-Null
} else {
  Write-Host "Creating variable '$Name'" -ForegroundColor Green
  Invoke-RestMethod -Method POST -Uri "$baseUri/workspaces/$workspaceId/vars" -Headers @{ Authorization = "Bearer $($env:TFC_TOKEN)"; 'Content-Type'='application/vnd.api+json' } -Body $body | Out-Null
}

Write-Host "Done." -ForegroundColor Yellow

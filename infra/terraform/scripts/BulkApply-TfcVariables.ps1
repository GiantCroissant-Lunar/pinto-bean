<#!
.SYNOPSIS
  Bulk create/update Terraform Cloud variables from JSON descriptor.
.DESCRIPTION
  Reads a JSON file containing arrays of terraform and env variables and upserts them.
  Automatically skips unchanged sensitive values (value still required in file though).

.PARAMETER Organization
  Terraform Cloud organization name.
.PARAMETER Workspace
  Workspace name.
.PARAMETER File
  Path to JSON file describing variables.

.JSON Structure
{
  "terraform": [
    { "key": "github_owner", "value": "GiantCroissant-Lunar" },
    { "key": "repository_name", "value": "pinto-bean" },
    { "key": "required_status_checks", "value": "[\"build\",\"test\"]", "hcl": true }
  ],
  "env": [
    { "key": "GITHUB_TOKEN", "value": "<token>", "sensitive": true }
  ]
}

.EXAMPLE
  ./BulkApply-TfcVariables.ps1 -Organization my-org -Workspace repo-github -File ./variables.json

.NOTES
  Requires env var TFC_TOKEN.
!#>
[CmdletBinding()]
param(
  [Parameter(Mandatory)][string]$Organization,
  [Parameter(Mandatory)][string]$Workspace,
  [Parameter(Mandatory)][string]$File
)

if (-not $env:TFC_TOKEN) { throw 'TFC_TOKEN environment variable not set.' }
if (-not (Test-Path $File)) { throw "File not found: $File" }

$baseUri = 'https://app.terraform.io/api/v2'
$descriptor = Get-Content -Raw -Path $File | ConvertFrom-Json

$wsResp = Invoke-RestMethod -Method GET -Uri "$baseUri/organizations/$Organization/workspaces/$Workspace" -Headers @{ Authorization = "Bearer $($env:TFC_TOKEN)" }
$workspaceId = $wsResp.data.id

$existing = (Invoke-RestMethod -Method GET -Uri "$baseUri/workspaces/$workspaceId/vars" -Headers @{ Authorization = "Bearer $($env:TFC_TOKEN)" }).data

function UpsertVar {
  param(
    [string]$Key,
    [string]$Value,
    [string]$Category,
    [bool]$Hcl=$false,
    [bool]$Sensitive=$false
  )
  $match = $existing | Where-Object { $_.attributes.key -eq $Key -and $_.attributes.category -eq $Category }
  $body = @{ data = @{ type='vars'; attributes = @{ key=$Key; value=$Value; category=$Category; hcl=$Hcl; sensitive=$Sensitive } } } | ConvertTo-Json -Depth 5
  if ($match) {
    Write-Host "Updating $Category variable '$Key'" -ForegroundColor Cyan
    Invoke-RestMethod -Method PATCH -Uri "$baseUri/workspaces/$workspaceId/vars/$($match.id)" -Headers @{ Authorization = "Bearer $($env:TFC_TOKEN)"; 'Content-Type'='application/vnd.api+json' } -Body $body | Out-Null
  } else {
    Write-Host "Creating $Category variable '$Key'" -ForegroundColor Green
    Invoke-RestMethod -Method POST -Uri "$baseUri/workspaces/$workspaceId/vars" -Headers @{ Authorization = "Bearer $($env:TFC_TOKEN)"; 'Content-Type'='application/vnd.api+json' } -Body $body | Out-Null
  }
}

foreach ($v in @($descriptor.terraform)) {
  if ($null -ne $v) { UpsertVar -Key $v.key -Value $v.value -Category 'terraform' -Hcl ([bool]$v.hcl) -Sensitive ([bool]$v.sensitive) }
}
foreach ($v in @($descriptor.env)) {
  if ($null -ne $v) { UpsertVar -Key $v.key -Value $v.value -Category 'env' -Hcl ([bool]$v.hcl) -Sensitive ([bool]$v.sensitive) }
}

Write-Host 'Bulk apply complete.' -ForegroundColor Yellow

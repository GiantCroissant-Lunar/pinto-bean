[CmdletBinding()]
param(
  [Parameter(Mandatory)][string]$Organization,
  [Parameter(Mandatory)][string]$Workspace,
  [string]$Message = "Queued via API script",
  [switch]$IsDestroy
)
if (-not $env:TFC_TOKEN) { throw 'TFC_TOKEN not set.' }
$base = 'https://app.terraform.io/api/v2'
$ws = Invoke-RestMethod -Method GET -Uri "$base/organizations/$Organization/workspaces/$Workspace" -Headers @{ Authorization = "Bearer $($env:TFC_TOKEN)" }
$wsId = $ws.data.id
$body = @{ data = @{ type = 'runs'; attributes = @{ message = $Message; 'is-destroy' = [bool]$IsDestroy }; relationships = @{ workspace = @{ data = @{ type = 'workspaces'; id = $wsId } } } } } | ConvertTo-Json -Depth 6
Write-Host "Queuing run (destroy=$IsDestroy) on workspace $Workspace" -ForegroundColor Cyan
$run = Invoke-RestMethod -Method POST -Uri "$base/runs" -Headers @{ Authorization = "Bearer $($env:TFC_TOKEN)"; 'Content-Type'='application/vnd.api+json' } -Body $body
$run.data.id

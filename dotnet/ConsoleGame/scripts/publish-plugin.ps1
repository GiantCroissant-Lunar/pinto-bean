param(
  [Parameter(Mandatory=$true)][string]$Project,
  [string]$Configuration = 'Debug',
  [string]$Framework = 'net9.0',
  [string]$Output
)

$ErrorActionPreference = 'Stop'

if (-not $Output) { $Output = Join-Path (Split-Path -Parent $PSScriptRoot) "artifacts\plugins\$(Split-Path -Leaf (Split-Path -Parent $Project))" }
New-Item -ItemType Directory -Force -Path $Output | Out-Null

dotnet publish $Project -c $Configuration -f $Framework -o $Output /p:PublishSingleFile=false /p:IncludeNativeLibrariesForSelfExtract=false
Write-Host "Published plugin to: $Output"

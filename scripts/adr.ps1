#!/usr/bin/env pwsh
<#
.SYNOPSIS
    ADR (Architecture Decision Record) management script for pinto-bean project

.DESCRIPTION
    This script provides convenient commands to manage Architecture Decision Records
    using log4brains, including creating new ADRs, previewing the knowledge base,
    and building the static site.

.PARAMETER Command
    The command to execute: preview, build, new, list

.PARAMETER Title
    Title for new ADR (required when Command is 'new')

.EXAMPLE
    ./adr.ps1 preview
    Starts the log4brains preview server

.EXAMPLE
    ./adr.ps1 new "Use Redis for caching"
    Creates a new ADR with the given title

.EXAMPLE
    ./adr.ps1 build
    Builds the static ADR site
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("preview", "build", "new", "list", "help")]
    [string]$Command,

    [Parameter(Position = 1)]
    [string]$Title,

    [Parameter()]
    [int]$Port = 4004
)

$ErrorActionPreference = 'Stop'
$RepoRoot = $PWD.Path

# Ensure we're in the correct directory
Set-Location $RepoRoot

function Write-Info($Message) {
    Write-Host "🔷 $Message" -ForegroundColor Cyan
}

function Write-Success($Message) {
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error-Custom($Message) {
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Test-Log4brains {
    try {
        $null = Get-Command log4brains -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

function Show-Help {
    Write-Host @"
🏗️  ADR Management for pinto-bean

Available commands:
  preview    Start the log4brains preview server
  build      Build the static ADR knowledge base
  new        Create a new ADR (requires title)
  list       List all existing ADRs
  help       Show this help message

Examples:
  .\adr.ps1 preview
  .\adr.ps1 new "Use Redis for caching"
  .\adr.ps1 build
  .\adr.ps1 list

Requirements:
  - Node.js and npm installed
  - log4brains installed globally: npm install -g log4brains

Current ADR directory: docs/adr/
Knowledge base URL (when running): http://localhost:$Port
"@
}

function Start-Preview {
    Write-Info "Starting log4brains preview server on port $Port..."
    Write-Info "Access the knowledge base at: http://localhost:$Port"
    Write-Info "Press Ctrl+C to stop the server"

    try {
        & log4brains preview --port $Port
    }
    catch {
        Write-Error-Custom "Failed to start preview server: $_"
        Write-Info "Make sure log4brains is installed: npm install -g log4brains"
    }
}

function Build-Site {
    Write-Info "Building static ADR knowledge base..."

    try {
        & log4brains-web build
        Write-Success "Static site built successfully in ./dist/"
    }
    catch {
        Write-Error-Custom "Failed to build site: $_"
    }
}

function New-ADR {
    if (-not $Title) {
        Write-Error-Custom "Title is required for new ADR. Use: .\adr.ps1 new 'Your ADR Title'"
        return
    }

    Write-Info "Creating new ADR: $Title"

    try {
        & log4brains adr new
        Write-Success "New ADR created successfully!"
    }
    catch {
        Write-Error-Custom "Failed to create new ADR: $_"
    }
}

function List-ADRs {
    Write-Info "Architecture Decision Records in docs/adr/:"
    Write-Host ""

    $adrPath = Join-Path $RepoRoot "docs" "adr" "*.md"

    if (-not (Test-Path (Join-Path $RepoRoot "docs" "adr"))) {
        Write-Error-Custom "ADR directory does not exist: $(Join-Path $RepoRoot "docs" "adr")"
        return
    }

    $adrFiles = Get-ChildItem -Path $adrPath | Where-Object { $_.Name -ne "index.md" -and $_.Name -ne "README.md" -and $_.Name -ne "template.md" }

    if ($adrFiles) {
        foreach ($file in $adrFiles | Sort-Object Name) {
            $title = (Get-Content $file.FullName -First 10 | Where-Object { $_ -match "^#\s+(.+)" } | Select-Object -First 1) -replace "^#\s+", ""
            Write-Host "📋 $($file.Name) - $title"
        }
    }
    else {
        Write-Host "No ADR files found."
    }
}

# Main execution
if (-not (Test-Log4brains)) {
    Write-Error-Custom "log4brains is not installed or not in PATH"
    Write-Info "Install it with: npm install -g log4brains"
    exit 1
}

switch ($Command) {
    "preview" { Start-Preview }
    "build" { Build-Site }
    "new" { New-ADR }
    "list" { List-ADRs }
    "help" { Show-Help }
    default { Show-Help }
}

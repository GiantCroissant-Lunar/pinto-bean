#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds and runs all PintoBean console samples.

.DESCRIPTION
    This script builds the PintoBean solution and runs the Analytics, Resources, and SceneFlow demo samples.
    It's designed to work both locally and in CI environments.

.PARAMETER Sample
    Specific sample to run. If not specified, runs all samples.
    Valid values: Analytics, Resources, SceneFlow, All

.PARAMETER Configuration
    Build configuration to use. Defaults to Release.

.PARAMETER SkipBuild
    Skip the build step and run samples directly.

.PARAMETER ContinueOnFailure
    Continue running other samples even if one fails.

.EXAMPLE
    pwsh scripts/run-samples.ps1
    Runs all samples with default settings.

.EXAMPLE
    pwsh scripts/run-samples.ps1 -Sample Analytics
    Runs only the Analytics sample.

.EXAMPLE
    pwsh scripts/run-samples.ps1 -Configuration Debug -ContinueOnFailure
    Runs all samples in Debug mode, continuing even if one fails.
#>

param(
    [ValidateSet("Analytics", "Resources", "SceneFlow", "All")]
    [string]$Sample = "All",
    
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [switch]$SkipBuild,
    [switch]$ContinueOnFailure
)

$ErrorActionPreference = if ($ContinueOnFailure) { 'Continue' } else { 'Stop' }
$RepoRoot = (Resolve-Path "$PSScriptRoot/..").Path
$SolutionDir = Join-Path $RepoRoot "dotnet/Yokan.PintoBean"

Write-Host "=== PintoBean Samples Runner ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Solution: $SolutionDir" -ForegroundColor Gray
Write-Host ""

# Define sample projects
$Samples = @{
    "Analytics" = @{
        Name = "PintoBean Analytics Demo"
        Project = "samples/PintoBean.Analytics.Demo.Console"
        Description = "Demonstrates FanOut and Sharded selection strategies with analytics providers"
    }
    "Resources" = @{
        Name = "PintoBean Resources Demo"
        Project = "samples/PintoBean.Resources.Demo.Console"
        Description = "Shows PickOne strategy with fallback via resilience patterns"
    }
    "SceneFlow" = @{
        Name = "PintoBean SceneFlow Demo"
        Project = "samples/PintoBean.SceneFlow.Demo.Console"
        Description = "Demonstrates deterministic PickOne with explicit policy configuration"
    }
}

function Write-Section($Title) {
    Write-Host "`n=== $Title ===" -ForegroundColor Yellow
}

function Write-Success($Message) {
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error($Message) {
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Write-Info($Message) {
    Write-Host "ℹ️  $Message" -ForegroundColor Blue
}

function Test-CommandAvailable($Command) {
    if (-not (Get-Command $Command -ErrorAction SilentlyContinue)) {
        Write-Error "$Command not found in PATH"
        exit 1
    }
}

# Pre-flight checks
Write-Section "Pre-flight Checks"
Test-CommandAvailable "dotnet"

Set-Location $SolutionDir
Write-Info "Working directory: $(Get-Location)"

# Build step
if (-not $SkipBuild) {
    Write-Section "Building Solution"
    Write-Info "Building with configuration: $Configuration"
    
    try {
        Write-Host "Restoring packages..." -ForegroundColor Gray
        & dotnet restore --nologo
        if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed" }
        
        Write-Host "Building solution..." -ForegroundColor Gray
        & dotnet build -c $Configuration --nologo --no-restore
        if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }
        
        Write-Success "Solution built successfully"
    }
    catch {
        Write-Error "Build failed: $_"
        if (-not $ContinueOnFailure) { exit 1 }
    }
} else {
    Write-Info "Skipping build step"
}

# Determine which samples to run
$SamplesToRun = if ($Sample -eq "All") { $Samples.Keys } else { @($Sample) }

Write-Section "Running Samples"
Write-Info "Samples to run: $($SamplesToRun -join ', ')"

$Results = @{}
$TotalSamples = 0
$SuccessfulSamples = 0

foreach ($SampleKey in $SamplesToRun) {
    $SampleInfo = $Samples[$SampleKey]
    $TotalSamples++
    
    Write-Host "`n--- $($SampleInfo.Name) ---" -ForegroundColor Magenta
    Write-Host "$($SampleInfo.Description)" -ForegroundColor Gray
    Write-Host "Project: $($SampleInfo.Project)" -ForegroundColor Gray
    
    try {
        $StartTime = Get-Date
        & dotnet run --project $SampleInfo.Project -c $Configuration --no-build
        $EndTime = Get-Date
        $Duration = $EndTime - $StartTime
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "$($SampleInfo.Name) completed successfully in $($Duration.TotalSeconds.ToString('F1'))s"
            $Results[$SampleKey] = @{ Status = "Success"; Duration = $Duration; Error = $null }
            $SuccessfulSamples++
        } else {
            throw "Sample exited with code $LASTEXITCODE"
        }
    }
    catch {
        $ErrorMessage = $_.Exception.Message
        Write-Error "$($SampleInfo.Name) failed: $ErrorMessage"
        $Results[$SampleKey] = @{ Status = "Failed"; Duration = $null; Error = $ErrorMessage }
        
        if (-not $ContinueOnFailure) {
            Write-Error "Stopping execution due to failure. Use -ContinueOnFailure to continue running other samples."
            exit 1
        }
    }
}

# Summary
Write-Section "Execution Summary"
Write-Host "Total samples: $TotalSamples" -ForegroundColor Gray
Write-Host "Successful: $SuccessfulSamples" -ForegroundColor Green
Write-Host "Failed: $($TotalSamples - $SuccessfulSamples)" -ForegroundColor Red

if ($Results.Count -gt 0) {
    Write-Host "`nDetailed Results:" -ForegroundColor Gray
    foreach ($SampleKey in $Results.Keys) {
        $Result = $Results[$SampleKey]
        $StatusColor = if ($Result.Status -eq "Success") { "Green" } else { "Red" }
        $Duration = if ($Result.Duration) { " ($($Result.Duration.TotalSeconds.ToString('F1'))s)" } else { "" }
        Write-Host "  $SampleKey`: " -NoNewline -ForegroundColor Gray
        Write-Host "$($Result.Status)$Duration" -ForegroundColor $StatusColor
        
        if ($Result.Error) {
            Write-Host "    Error: $($Result.Error)" -ForegroundColor Red
        }
    }
}

# Exit with appropriate code
$ExitCode = if ($SuccessfulSamples -eq $TotalSamples) { 0 } else { 1 }
Write-Host "`nScript completed with exit code: $ExitCode" -ForegroundColor Gray
exit $ExitCode
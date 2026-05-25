param(
    [string]$ProjectId = "",
    [string]$EnvironmentName = "",
    [switch]$Login,
    [switch]$DryRunOnly,
    [switch]$BuildOnly,
    [switch]$SkipDryRun,
    [switch]$NoPause
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "== $Message ==" -ForegroundColor Cyan
}

function Require-Command {
    param([string]$CommandName)

    if (Get-Command $CommandName -ErrorAction SilentlyContinue) {
        return
    }

    throw "Required command '$CommandName' was not found in PATH."
}

function Invoke-Checked {
    param(
        [string]$FilePath,
        [string[]]$Arguments
    )

    Write-Host "> $FilePath $($Arguments -join ' ')" -ForegroundColor DarkGray
    & $FilePath @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $LASTEXITCODE."
    }
}

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Resolve-Path (Join-Path $ScriptRoot "..")
$ModuleRoot = Join-Path $ProjectRoot "CloudCodeModules\EdgegapAllocator"
$SolutionPath = Join-Path $ModuleRoot "EdgegapAllocator.sln"
$AllocatorSourcePath = Join-Path $ModuleRoot "Project\EdgegapAllocator.cs"

if (!(Test-Path $SolutionPath)) {
    throw "Cloud Code solution not found: $SolutionPath"
}

if (!(Test-Path $AllocatorSourcePath)) {
    throw "Edgegap allocator source not found: $AllocatorSourcePath"
}

Require-Command "dotnet"
Require-Command "ugs"

$AllocatorSource = Get-Content -Path $AllocatorSourcePath -Raw
$VersionMatch = [regex]::Match($AllocatorSource, 'private\s+const\s+string\s+VersionName\s*=\s*"([^"]+)"')
$VersionName = if ($VersionMatch.Success) { $VersionMatch.Groups[1].Value } else { "<not found>" }

Write-Host "EdgegapAllocator Cloud Code deploy"
Write-Host "Project root : $ProjectRoot"
Write-Host "Module      : $SolutionPath"
Write-Host "VersionName : $VersionName"

if ($Login) {
    Write-Step "UGS login"
    Invoke-Checked "ugs" @("login")
}

if (![string]::IsNullOrWhiteSpace($ProjectId)) {
    Write-Step "UGS project"
    Invoke-Checked "ugs" @("config", "set", "project-id", $ProjectId)
}

if (![string]::IsNullOrWhiteSpace($EnvironmentName)) {
    Write-Step "UGS environment"
    Invoke-Checked "ugs" @("config", "set", "environment-name", $EnvironmentName)
}

Write-Step "Build Cloud Code module"
Invoke-Checked "dotnet" @("build", $SolutionPath)

if ($BuildOnly) {
    Write-Host ""
    Write-Host "Build completed. UGS deployment skipped." -ForegroundColor Yellow
    exit 0
}

if (!$SkipDryRun) {
    Write-Step "UGS deploy dry-run"
    Invoke-Checked "ugs" @("deploy", $SolutionPath, "--dry-run")
}

if ($DryRunOnly) {
    Write-Host ""
    Write-Host "Dry-run completed. Real deployment skipped." -ForegroundColor Yellow
    exit 0
}

Write-Step "UGS deploy"
Invoke-Checked "ugs" @("deploy", $SolutionPath)

Write-Host ""
Write-Host "EdgegapAllocator deployed successfully." -ForegroundColor Green

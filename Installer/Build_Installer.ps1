<#
.SYNOPSIS
    Builds and packages SmartDS Auto Launcher into an Inno Setup installer.
.DESCRIPTION
    1. Restores and publishes the .NET 8 WPF Release build.
    2. Compiles the Inno Setup script (SmartDS_Auto_Launcher.iss) to generate SmartDS_Auto_Launcher_Setup.exe.
#>

param (
    [switch]$SelfContained = $false
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$projectPath = Join-Path $rootDir "SmartDSAutoLauncher\SmartDSAutoLauncher.csproj"
$publishDir = Join-Path $rootDir "publish"
$outputDir = Join-Path $rootDir "Output"
$issScript = Join-Path $scriptDir "SmartDS_Auto_Launcher.iss"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "       SmartDS Auto Launcher - Build & Package Script     " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Clean previous publish and output
Write-Host "`n[1/3] Preparing build directories..." -ForegroundColor Yellow
if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
if (-not (Test-Path $outputDir)) { New-Item -ItemType Directory -Path $outputDir -Force | Out-Null }

# 2. Publish .NET 8 WPF Application
Write-Host "`n[2/3] Publishing Release build..." -ForegroundColor Yellow
$publishArgs = @("publish", $projectPath, "-c", "Release", "-o", $publishDir)
if ($SelfContained) {
    $publishArgs += @("-r", "win-x64", "--self-contained", "true")
}

& dotnet $publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}
Write-Host "Publish completed successfully -> $publishDir" -ForegroundColor Green

# 3. Compile Inno Setup Installer
Write-Host "`n[3/3] Locating Inno Setup Compiler (ISCC.exe)..." -ForegroundColor Yellow
$isccLocations = @(
    "iscc.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\iscc.exe",
    "${env:ProgramFiles}\Inno Setup 6\iscc.exe",
    "${env:LOCALAPPDATA}\Programs\Inno Setup 6\iscc.exe"
)

$isccPath = $null
foreach ($loc in $isccLocations) {
    if (Get-Command $loc -ErrorAction SilentlyContinue) {
        $isccPath = $loc
        break
    } elseif (Test-Path $loc) {
        $isccPath = $loc
        break
    }
}

if ($isccPath) {
    Write-Host "Compiling installer using: $isccPath" -ForegroundColor Cyan
    & $isccPath $issScript
    if ($LASTEXITCODE -eq 0) {
        $finalInstaller = Join-Path $outputDir "SmartDS_Auto_Launcher_Setup.exe"
        Write-Host "`n==========================================================" -ForegroundColor Green
        Write-Host " SUCCESS! Installer generated at:" -ForegroundColor Green
        Write-Host " $finalInstaller" -ForegroundColor Green
        Write-Host "==========================================================" -ForegroundColor Green
    } else {
        Write-Error "Inno Setup compilation failed with exit code $LASTEXITCODE"
    }
} else {
    Write-Host "`n[NOTE] Inno Setup 6 compiler (ISCC.exe) was not found in standard paths." -ForegroundColor Yellow
    Write-Host "To create the final installer:" -ForegroundColor White
    Write-Host " 1. Download Inno Setup 6 from: https://jrsoftware.org/isdl.php" -ForegroundColor White
    Write-Host " 2. Open '$issScript' in Inno Setup Compiler and click 'Compile' (F9)" -ForegroundColor White
    Write-Host "    OR run: 'iscc.exe ""$issScript""'" -ForegroundColor White
}

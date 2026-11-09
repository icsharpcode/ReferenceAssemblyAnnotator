# Builds each sample project under tests/ against the locally built package.
# Run after building the solution with the same configuration.
param(
    [string]$Configuration = 'Debug',
    [ValidateSet('dotnet', 'msbuild')]
    [string]$Engine = 'dotnet',
    # NuGet packages folder for the test restores. Default: tests/.isolatednugetcache, wiped before every case.
    # CI passes a short path outside the checkout instead (MAX_PATH under msbuild.exe); that is not wiped.
    [string]$PackagesPath
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$tests = Join-Path $root 'tests'

# tests/NuGet.Config resolves the local package source through this variable.
$env:CONFIGURATION = $Configuration

# Set NUGET_PACKAGES explicitly: an inherited value (e.g. a machine-wide cache) overrides globalPackagesFolder in
# tests/NuGet.Config, and would then serve a stale annotator package with the same version as the one just built.
if (-not $PackagesPath) { $PackagesPath = Join-Path $tests '.isolatednugetcache' }
$env:NUGET_PACKAGES = $PackagesPath

if ($Engine -eq 'msbuild') {
    $msbuild = (Get-Command msbuild.exe -ErrorAction SilentlyContinue).Source
    if (-not $msbuild) {
        $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
        $msbuild = & $vswhere -latest -prerelease -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    }
    if (-not $msbuild) { throw 'msbuild.exe not found' }
}

$singleTfms = 'net35', 'net48', 'netstandard1.6', 'netstandard2.0', 'netstandard2.1',
    'netcoreapp1.0', 'netcoreapp1.1', 'netcoreapp2.0', 'netcoreapp2.1', 'netcoreapp3.0', 'netcoreapp3.1', 'net10.0'

$cases = @(
    @{ Project = 'MultiTFM'; Args = @('-warnaserror') }
    @{ Project = 'RefVersion3.1.0'; Args = @('-warnaserror') }
    # -warnaserror ignores WarningsNotAsErrors, so this one uses the property instead.
    @{ Project = 'NoDisabledWarnings'; Args = @('-p:TreatWarningsAsErrors=true') }
)
foreach ($tfm in $singleTfms) {
    $cases += @{ Project = 'SingleTFM'; Args = @('-warnaserror', "-p:TestFramework=$tfm") }
}
foreach ($markup in 'false', 'true') {
    $cases += @{ Project = 'WPF'; Args = @('-warnaserror', "-p:IncludePackageReferencesDuringMarkupCompilation=$markup") }
}

$failed = @()
foreach ($case in $cases) {
    $name = "$($case.Project) $($case.Args -join ' ')"
    Write-Host "=== [$Engine] $name" -ForegroundColor Cyan

    # Start clean so the isolated package cache never serves a stale package.
    # longpaths: package cache paths exceed MAX_PATH on deep checkouts (e.g. GitHub runners).
    git -C $root -c core.longpaths=true clean -dxfq -- tests
    if ($LASTEXITCODE -ne 0) { throw 'git clean failed' }

    $buildArgs = @('-restore', (Join-Path $tests $case.Project), '-nr:false', '-v:m', "-p:Configuration=$Configuration") + $case.Args
    if ($Engine -eq 'dotnet') { & dotnet msbuild @buildArgs } else { & $msbuild @buildArgs }
    if ($LASTEXITCODE -ne 0) { $failed += $name }
}

if ($failed.Count -gt 0) {
    Write-Host "Failed ($($failed.Count)/$($cases.Count)):" -ForegroundColor Red
    $failed | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}

Write-Host "All $($cases.Count) test builds passed." -ForegroundColor Green

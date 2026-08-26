<#
.SYNOPSIS
    Builds PmxMcpPlugin.dll without Visual Studio, using the C# compiler that ships
    with the .NET Framework (or the Roslyn compiler if Visual Studio is installed).

.PARAMETER PmxEditorPath
    Folder that contains PmxEditor.exe. Defaults to the PMXEDITOR_PATH environment
    variable, then to a few common locations.

.PARAMETER Install
    Also copy the built DLL into <PmxEditorPath>\_plugin\User.

.EXAMPLE
    .\build.ps1 -PmxEditorPath C:\Tools\PmxEditor_0273 -Install
#>
[CmdletBinding()]
param(
    [string]$PmxEditorPath,
    [switch]$Install
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Definition

function Resolve-PmxEditorPath {
    param([string]$Explicit)

    $candidates = @()
    if ($Explicit) { $candidates += $Explicit }
    if ($env:PMXEDITOR_PATH) { $candidates += $env:PMXEDITOR_PATH }
    $candidates += @(
        "C:\PmxEditor",
        "$env:USERPROFILE\Downloads\PmxEditor_0273",
        "$env:USERPROFILE\Documents\PmxEditor"
    )

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path (Join-Path $candidate "Lib\PEPlugin\PEPlugin.dll"))) {
            return (Resolve-Path $candidate).Path
        }
    }
    throw "PMX Editor was not found. Pass -PmxEditorPath <folder containing PmxEditor.exe> or set PMXEDITOR_PATH."
}

function Resolve-Csc {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $vsPath = & $vswhere -latest -products * -property installationPath 2>$null
        if ($vsPath) {
            $roslyn = Join-Path $vsPath "MSBuild\Current\Bin\Roslyn\csc.exe"
            if (Test-Path $roslyn) { return $roslyn }
        }
    }
    $framework = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    if (Test-Path $framework) { return $framework }
    throw "No C# compiler found (looked for Roslyn via vswhere and the .NET Framework csc.exe)."
}

$PmxEditorPath = Resolve-PmxEditorPath -Explicit $PmxEditorPath
$csc = Resolve-Csc

$pePlugin = Join-Path $PmxEditorPath "Lib\PEPlugin\PEPlugin.dll"
$slimDx = Join-Path $PmxEditorPath "Lib\SlimDX\x64\SlimDX.dll"
if (-not (Test-Path $slimDx)) { $slimDx = Join-Path $PmxEditorPath "Lib\SlimDX\x86\SlimDX.dll" }
if (-not (Test-Path $slimDx)) { throw "SlimDX.dll was not found under $PmxEditorPath\Lib\SlimDX." }

$distDir = Join-Path $root "dist"
if (-not (Test-Path $distDir)) { New-Item -ItemType Directory -Path $distDir | Out-Null }
$output = Join-Path $distDir "PmxMcpPlugin.dll"

$sources = Get-ChildItem -Path (Join-Path $root "src") -Filter *.cs -Recurse | ForEach-Object { $_.FullName }
if (-not $sources) { throw "No source files found under $root\src." }

Write-Host "PMX Editor : $PmxEditorPath"
Write-Host "Compiler   : $csc"
Write-Host "Sources    : $($sources.Count) files"

$arguments = @(
    "/nologo",
    "/target:library",
    "/platform:anycpu",
    "/optimize+",
    "/codepage:65001",
    "/langversion:5",
    "/out:$output",
    "/r:$pePlugin",
    "/r:$slimDx",
    "/r:System.dll",
    "/r:System.Core.dll",
    "/r:System.Drawing.dll",
    "/r:System.Windows.Forms.dll",
    "/r:System.Web.Extensions.dll"
) + $sources

& $csc $arguments
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE." }

Write-Host "Built      : $output"

if ($Install) {
    $target = Join-Path $PmxEditorPath "_plugin\User"
    if (-not (Test-Path $target)) { New-Item -ItemType Directory -Path $target | Out-Null }
    Copy-Item $output $target -Force
    Write-Host "Installed  : $(Join-Path $target 'PmxMcpPlugin.dll')"
    Write-Host "Restart PMX Editor to load the new build."
}

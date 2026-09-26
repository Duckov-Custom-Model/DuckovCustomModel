#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$binRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot 'DuckovCustomModel/bin'))
$outputRoot = (Resolve-Path -LiteralPath $OutputDirectory).Path
if (-not $outputRoot.StartsWith($binRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Packaging output must be strictly inside DuckovCustomModel/bin.'
}
if (-not (Test-Path -LiteralPath (Join-Path $outputRoot 'DuckovCustomModel.GameModules.dll') -PathType Leaf)) {
    throw 'GameModules DLL missing from build output.'
}
$requiredFiles = @(
    'DuckovCustomModel.dll', 'DuckovCustomModel.Core.dll', 'DuckovCustomModel.GameModules.dll',
    'ModelRuntime.dll', 'ModelRuntime.Adapters.dll', 'ModelRuntime.Media.dll',
    'ModelRuntime.Ysm.dll', 'StbImageSharp.dll', 'ZstdSharp.dll',
    'System.Runtime.CompilerServices.Unsafe.dll', 'info.ini', 'preview.png',
    'Resources/ysm-rendering.bundle'
)
foreach ($relativePath in $requiredFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $outputRoot $relativePath) -PathType Leaf)) {
        throw "Required package file missing: $relativePath"
    }
}
$packageDirectory = Join-Path $binRoot ('PackageTemp-' + [Guid]::NewGuid().ToString('N'))
$configuration = Split-Path (Split-Path $outputRoot.TrimEnd('\', '/') -Parent) -Leaf
$zipDirectory = Join-Path $binRoot $configuration
New-Item -ItemType Directory -Path $zipDirectory -Force | Out-Null
$zipPath = Join-Path $zipDirectory 'DuckovCustomModel.zip'
New-Item -ItemType Directory -Path $packageDirectory | Out-Null
try {
    foreach ($relativePath in $requiredFiles) {
        $destination = Join-Path $packageDirectory $relativePath
        New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $outputRoot $relativePath) -Destination $destination
    }
    $localizations = Join-Path $outputRoot 'Localizations'
    if (Test-Path -LiteralPath $localizations) {
        $destination = Join-Path $packageDirectory 'Localizations'
        New-Item -ItemType Directory -Path $destination | Out-Null
        Get-ChildItem -LiteralPath $localizations -Filter '*.json' -File | Copy-Item -Destination $destination
    }
    $versionSource = [IO.File]::ReadAllText((Join-Path $repositoryRoot 'DuckovCustomModel/Constant.cs'))
    if ($versionSource -notmatch 'ModVersion\s*=\s*"([^"]+)"') { throw 'Could not read existing ModVersion.' }
    [IO.File]::WriteAllText((Join-Path $packageDirectory 'version.txt'), $Matches[1])
    Compress-Archive -Path (Join-Path $packageDirectory '*') -DestinationPath $zipPath -Force
    Write-Output "Local review package: $zipPath"
    Get-FileHash -LiteralPath $zipPath -Algorithm SHA256
}
finally {
    $resolvedTemporaryDirectory = [IO.Path]::GetFullPath($packageDirectory)
    if (-not $resolvedTemporaryDirectory.StartsWith($binRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing temporary directory cleanup outside project bin.'
    }
    [IO.Directory]::Delete($resolvedTemporaryDirectory, $true)
}

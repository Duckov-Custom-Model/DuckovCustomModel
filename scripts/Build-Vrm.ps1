#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$UnityEditor,
    [string]$WorkDirectory = (Join-Path $PSScriptRoot '../bin/VrmBuild')
)
$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$workspace = [IO.Path]::GetFullPath($WorkDirectory)
$project = Join-Path $workspace 'Project'
$output = Join-Path $workspace 'Output'
$archive = Join-Path $workspace 'VRM.unitypackage'
$extracted = Join-Path $workspace 'Package'
$editor = (Resolve-Path -LiteralPath $UnityEditor).Path
if ((Get-Item -LiteralPath $editor).VersionInfo.ProductVersion -notlike '2022.3.*') {
    throw 'VRM 资源需要使用 Unity 2022.3 编译，以兼容游戏的两个 Unity 版本。'
}
foreach ($directory in @($workspace, $output, $extracted, "$project/Assets/Editor", "$project/Packages", "$project/ProjectSettings")) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}
$hash = '4DE20729161C4CFCFDC4BA889645A0B0846D1CFD423BE4B5B44B5A4B95988D90'
if (-not (Test-Path -LiteralPath $archive)) {
    Invoke-WebRequest -Uri 'https://github.com/vrm-c/UniVRM/releases/download/v0.131.3/VRM-0.131.3_011b.unitypackage' -OutFile $archive
}
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $hash) { throw 'UniVRM 包校验失败。' }
& tar -xzf $archive -C $extracted
if ($LASTEXITCODE -ne 0) { throw 'UniVRM 包解压失败。' }
foreach ($entry in Get-ChildItem -LiteralPath $extracted -Directory) {
    $pathname = Join-Path $entry.FullName 'pathname'
    if (-not (Test-Path -LiteralPath $pathname)) { continue }
    $relative = [IO.File]::ReadAllText($pathname).Trim()
    if ($relative -notmatch '^Packages/com\.vrmc\.(gltf|vrm)/') { continue }
    $destination = [IO.Path]::GetFullPath((Join-Path $project $relative))
    if (-not $destination.StartsWith($project + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'UniVRM 包含无效资源路径。'
    }
    $asset = Join-Path $entry.FullName 'asset'
    if (-not (Test-Path -LiteralPath $asset -PathType Leaf)) { continue }
    New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $asset -Destination $destination
    $meta = Join-Path $entry.FullName 'asset.meta'
    if (Test-Path -LiteralPath $meta) { Copy-Item -LiteralPath $meta -Destination ($destination + '.meta') }
}
@'
{
  "dependencies": {
    "com.unity.collections": "1.4.0",
    "com.unity.render-pipelines.universal": "14.0.10",
    "com.unity.modules.assetbundle": "1.0.0",
    "com.unity.modules.physics": "1.0.0"
  }
}
'@ | Set-Content -LiteralPath "$project/Packages/manifest.json" -Encoding utf8NoBOM
'm_EditorVersion: 2022.3.22f1' | Set-Content -LiteralPath "$project/ProjectSettings/ProjectVersion.txt" -Encoding utf8NoBOM
Copy-Item -LiteralPath "$PSScriptRoot/Unity/BuildVrmResources.cs" -Destination "$project/Assets/Editor/BuildVrmResources.cs"
Expand-Archive -LiteralPath "$PSScriptRoot/Unity/Animations/UAL1-Standard.zip" -DestinationPath "$project/Assets/Animations" -Force
Expand-Archive -LiteralPath "$PSScriptRoot/Unity/Animations/KayKit-Movement.zip" -DestinationPath "$project/Assets/Animations/KayKit" -Force
@'
{
  "name": "VrmBuild.Editor",
  "includePlatforms": ["Editor"],
  "references": ["Unity.RenderPipelines.Universal.Runtime", "Unity.RenderPipelines.Core.Runtime"]
}
'@ | Set-Content -LiteralPath "$project/Assets/Editor/VrmBuild.Editor.asmdef" -Encoding utf8NoBOM
$previousOutput = $env:DCM_VRM_BUILD_OUTPUT
try {
    $env:DCM_VRM_BUILD_OUTPUT = $output
    $log = Join-Path $workspace 'Build.log'
    $arguments = @('-batchmode', '-quit', '-projectPath', ('"' + $project + '"'), '-executeMethod', 'BuildVrmResources.Build', '-logFile', ('"' + $log + '"'))
    $process = Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0 -or -not (Select-String -LiteralPath $log -Pattern 'VRM_RESOURCES_BUILT' -Quiet)) {
        throw "VRM 构建失败，请检查 $log"
    }
} finally {
    $env:DCM_VRM_BUILD_OUTPUT = $previousOutput
}
foreach ($assembly in @('UniGLTF.Utils', 'UniGLTF.UniUnlit', 'UniHumanoid', 'UniGLTF', 'SpringBoneJobs', 'FastSpringBone10', 'VrmLib', 'VRM10.MToon10', 'VRM10')) {
    Copy-Item -LiteralPath "$output/Assemblies/$assembly.dll" -Destination "$repository/lib/$assembly.dll"
}
Copy-Item -LiteralPath "$output/vrm-runtime.bundle" -Destination "$repository/DuckovCustomModel/Resources/vrm-runtime.bundle"
Copy-Item -LiteralPath "$project/Packages/com.vrmc.gltf/LICENSE.md" -Destination "$repository/DuckovCustomModel/Resources/UniVRM-LICENSE.txt"

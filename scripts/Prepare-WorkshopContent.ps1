$outputPath = "DuckovCustomModel/bin/Release/netstandard2.1"
$workshopDir = "DuckovCustomModel/bin/Release/WorkshopContent"

# 检查编译输出目录是否存在
if (-not (Test-Path $outputPath)) {
  Write-Error "编译输出目录不存在: $outputPath，请确保 Build 步骤已成功执行"
  exit 1
}

# 创建 Workshop 内容目录
if (Test-Path $workshopDir) {
  Remove-Item -Path $workshopDir -Recurse -Force
}
New-Item -ItemType Directory -Path $workshopDir -Force | Out-Null

# 从编译输出目录复制所有文件到 Workshop 目录
Get-ChildItem -Path $outputPath -Recurse -File | ForEach-Object {
  $relativePath = $_.FullName.Substring((Resolve-Path $outputPath).Path.Length).TrimStart('\', '/')
  $destPath = Join-Path $workshopDir $relativePath
  $destDir = Split-Path $destPath -Parent
  if (-not (Test-Path $destDir)) {
    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
  }
  Copy-Item $_.FullName -Destination $destPath -Force
}

Write-Host "✓ Workshop 内容已准备完成（从 $outputPath 复制）"

echo "workshop_dir=$workshopDir" >> $env:GITHUB_OUTPUT

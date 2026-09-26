$hasUsername = $env:STEAM_USERNAME -ne ""
$hasPassword = $env:STEAM_PASSWORD -ne ""
$hasAppId = $env:STEAM_APP_ID -ne ""

if ($hasUsername -and $hasPassword -and $hasAppId) {
  echo "steam_enabled=true" >> $env:GITHUB_OUTPUT
  Write-Host "✓ Steam Workshop secrets 已配置"
} else {
  echo "steam_enabled=false" >> $env:GITHUB_OUTPUT
  Write-Host "⚠ Steam Workshop secrets 未配置，将跳过上传"
}

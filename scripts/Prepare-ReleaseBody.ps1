$tagName = $env:GITHUB_REF_NAME
$version = $tagName -replace '^v', ''
$changelogFile = "CHANGELOG.md"
$manualChangelog = ""

if (Test-Path $changelogFile) {
  $content = Get-Content $changelogFile -Raw -Encoding UTF8
  $escapedVersion = [regex]::Escape($version)

  # 判断是否为 fix 版本
  $isFixVersion = $version -match '-fix\d+$'

  if ($isFixVersion) {
    # Fix 版本：提取从 fix 版本到下一个版本之间的所有内容
    # 例如：v1.9.5-fix1 应该包含 v1.9.5-fix1 和 v1.9.5 的内容
    # 提取基础版本号（去掉 -fix1 等后缀）
    $baseVersion = $version -replace '-fix\d+$', ''
    $escapedBaseVersion = [regex]::Escape($baseVersion)

    # 找到 fix 版本的起始位置
    $fixVersionPattern = "##\s+v?$escapedVersion\s*\r?\n"
    $fixVersionMatch = [regex]::Match($content, $fixVersionPattern)

    if ($fixVersionMatch.Success) {
      # 从 fix 版本开始，找到下一个不同版本号的位置
      # 需要跳过基础版本号（如 v1.9.5），找到下一个不同的版本号（如 v1.9.4）
      $startPos = $fixVersionMatch.Index + $fixVersionMatch.Length
      $remainingContent = $content.Substring($startPos)

      # 先匹配所有版本号标题
      $allVersionPattern = "(?m)^##\s+v?(\d+\.\d+\.\d+(?:-fix\d+)?)"
      $allVersionMatches = [regex]::Matches($remainingContent, $allVersionPattern)

      $nextVersionIndex = -1
      foreach ($match in $allVersionMatches) {
        $matchedVersion = $match.Groups[1].Value
        # 如果匹配到的版本号不是基础版本号的任何变体，则找到了下一个不同版本号
        if ($matchedVersion -notmatch "^$escapedBaseVersion(?:-fix\d+)?$") {
          $nextVersionIndex = $match.Index
          break
        }
      }

      if ($nextVersionIndex -ge 0) {
        # 提取到下一个不同版本号之前的所有内容
        $changelogContent = $remainingContent.Substring(0, $nextVersionIndex)
      } else {
        # 如果没有找到下一个不同版本号，提取到文件结尾
        $changelogContent = $remainingContent
      }

      # 分离 fix 内容和基础版本内容
      $baseVersionPattern = "##\s+v?$escapedBaseVersion\s*\r?\n"
      $baseVersionMatch = [regex]::Match($changelogContent, $baseVersionPattern)

      if ($baseVersionMatch.Success) {
        # 分离两部分内容
        $fixContent = $changelogContent.Substring(0, $baseVersionMatch.Index).Trim()
        $baseContent = $changelogContent.Substring($baseVersionMatch.Index + $baseVersionMatch.Length).Trim()

        # 处理 fix 内容：保留 fix 版本标识，但转换为更清晰的格式
        # 获取当前 fix 版本号
        $currentFixNumber = ""
        $fixMatch = [regex]::Match($version, '-fix(\d+)$')
        if ($fixMatch.Success) {
          $currentFixNumber = $fixMatch.Groups[1].Value
        }
        # 匹配所有 fix 版本标题行，转换为只带 fix 编号的格式（但当前 fix 版本不添加标题）
        $fixContent = [regex]::Replace($fixContent, "(?m)^##\s+v?$escapedBaseVersion-fix(\d+)\s*\r?\n", {
          param($match)
          $matchedFixNumber = $match.Groups[1].Value
          if ($matchedFixNumber -eq $currentFixNumber) {
            # 当前 fix 版本，不添加标题，只保留换行
            return "`n`n"
          } else {
            # 其他 fix 版本，添加标题
            return "`n`n**fix$matchedFixNumber 修复：**`n`n"
          }
        })
        # 如果当前 fix 版本内容在开头且没有标题，保持原样（不添加标题）
        # 清理多余的连续换行（超过2个换行的地方只保留2个）
        $fixContent = [regex]::Replace($fixContent, "(\r?\n){3,}", "`n`n")

        # 移除基础版本标题行（如果存在）
        $baseContent = [regex]::Replace($baseContent, "(?m)^##\s+v?$escapedBaseVersion(?:-fix\d+)?\s*\r?\n", "")

        # 清理基础版本内容的连续换行
        $baseContent = [regex]::Replace($baseContent, "(\r?\n){3,}", "`n`n")

        # 组合两部分，明确区分 fix 内容和原始版本内容
        if ($fixContent -and $baseContent) {
          $manualChangelog = "**修复内容：**`n`n$fixContent`n`n**原始更新内容：**`n`n$baseContent"
        } elseif ($fixContent) {
          $manualChangelog = "**修复内容：**`n`n$fixContent"
        } else {
          $manualChangelog = $baseContent
        }
      } else {
        # 如果没有找到基础版本，获取当前 fix 版本号
        $currentFixNumber = ""
        $fixMatch = [regex]::Match($version, '-fix(\d+)$')
        if ($fixMatch.Success) {
          $currentFixNumber = $fixMatch.Groups[1].Value
        }
        # 转换 fix 版本标题行（当前 fix 版本不添加标题）
        $changelogContent = [regex]::Replace($changelogContent, "(?m)^##\s+v?$escapedBaseVersion-fix(\d+)\s*\r?\n", {
          param($match)
          $matchedFixNumber = $match.Groups[1].Value
          if ($matchedFixNumber -eq $currentFixNumber) {
            # 当前 fix 版本，不添加标题，只保留换行
            return "`n`n"
          } else {
            # 其他 fix 版本，添加标题
            return "`n`n**fix$matchedFixNumber 修复：**`n`n"
          }
        })
        # 清理多余的连续换行
        $changelogContent = [regex]::Replace($changelogContent, "(\r?\n){3,}", "`n`n")
        $manualChangelog = $changelogContent.Trim()
      }
      Write-Host "✓ 从 CHANGELOG.md 提取到 fix 版本 changelog ($($manualChangelog.Length) 字符)"
      if ($manualChangelog.Length -gt 0) {
        Write-Host "预览前100字符: $($manualChangelog.Substring(0, [Math]::Min(100, $manualChangelog.Length)))"
      }
    } else {
      Write-Host "⚠ 未在 CHANGELOG.md 中找到 fix 版本 $version 的内容"
    }
  } else {
    # 非 fix 版本：使用原有逻辑
    $pattern = "(?s)##\s+v?$escapedVersion\s*\r?\n(.*?)(?=\r?\n##\s+v?\d+\.\d+\.\d+|$)"
    $match = [regex]::Match($content, $pattern)

    if ($match.Success) {
      $manualChangelog = $match.Groups[1].Value.Trim()
      Write-Host "✓ 从 CHANGELOG.md 提取到 changelog ($($manualChangelog.Length) 字符)"
      if ($manualChangelog.Length -gt 0) {
        Write-Host "预览前100字符: $($manualChangelog.Substring(0, [Math]::Min(100, $manualChangelog.Length)))"
      }
    } else {
      Write-Host "⚠ 未在 CHANGELOG.md 中找到版本 $version 的内容"
      Write-Host "尝试的正则表达式: $pattern"
    }
  }
}

$headers = @{
  Authorization = "token $env:GITHUB_TOKEN"
  Accept = "application/vnd.github.v3+json"
}

$generateNotesUrl = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases/generate-notes"
$generateNotesBody = @{
  tag_name = $tagName
} | ConvertTo-Json

$autoNotes = ""
try {
  $notesResponse = Invoke-RestMethod -Uri $generateNotesUrl -Method Post -Headers $headers -Body $generateNotesBody -ContentType "application/json"
  $autoNotes = $notesResponse.body
  Write-Host "✓ 获取到自动生成的 release notes"
} catch {
  Write-Host "⚠ 无法获取自动生成的 release notes: $_"
}

$releaseBody = ""
if ($manualChangelog) {
  $releaseBody = $manualChangelog
  if ($autoNotes) {
    $releaseBody += "`n`n---`n`n"
  }
}
if ($autoNotes) {
  $releaseBody += $autoNotes
}

if (-not $releaseBody) {
  $releaseBody = "Release $tagName"
}

echo "release_body<<EOF" >> $env:GITHUB_OUTPUT
echo "$releaseBody" >> $env:GITHUB_OUTPUT
echo "EOF" >> $env:GITHUB_OUTPUT

Write-Host "✓ Release body 准备完成 ($($releaseBody.Length) 字符)"

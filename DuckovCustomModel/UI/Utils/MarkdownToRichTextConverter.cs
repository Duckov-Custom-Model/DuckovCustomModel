using System.Text.RegularExpressions;

namespace DuckovCustomModel.UI.Utils
{
    public static class MarkdownToRichTextConverter
    {
        private static readonly string[] BulletSymbols = { "•", "◦", "▪", "▫" }; // 实心圆、空心圆、实心方块、空心方块

        public static string Convert(string markdown, int baseFontSize = 14, int separatorWidth = 10)
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;

            var result = markdown;

            // 移除代码块
            result = Regex.Replace(result, @"```[\s\S]*?```", "", RegexOptions.Multiline);
            result = Regex.Replace(result, @"`([^`]+)`", "$1", RegexOptions.Multiline);

            // 转换链接 [text](url) -> <link="url"><color=#4A9EFF><u>text</u></color></link>
            result = Regex.Replace(result, @"\[([^\]]+)\]\(([^\)]+)\)",
                "<link=\"$2\"><color=#4A9EFF><u>$1</u></color></link>");

            // 转换纯 URL 为链接（匹配 http:// 或 https:// 开头的 URL）
            result = Regex.Replace(result, @"(https?://[^\s\)]+)",
                "<link=\"$1\"><color=#4A9EFF><u>$1</u></color></link>");

            // 转换粗体 **text** -> <b>text</b>
            result = Regex.Replace(result, @"\*\*([^\*]+)\*\*", "<b>$1</b>");
            result = Regex.Replace(result, @"__([^_]+)__", "<b>$1</b>");

            // 转换斜体 *text* -> <i>text</i>
            result = Regex.Replace(result, @"(?<!\*)\*([^\*]+)\*(?!\*)", "<i>$1</i>");
            result = Regex.Replace(result, @"(?<!_)_([^_]+)_(?!_)", "<i>$1</i>");

            // 转换标题为字号（相对于基础字体大小的增量）
            var h6Size = baseFontSize;
            var h5Size = baseFontSize + 2;
            var h4Size = baseFontSize + 4;
            var h3Size = baseFontSize + 6;
            var h2Size = baseFontSize + 8;
            var h1Size = baseFontSize + 10;

            result = Regex.Replace(result, @"^######\s+(.+)$", $"<size={h6Size}><b>$1</b></size>",
                RegexOptions.Multiline);
            result = Regex.Replace(result, @"^#####\s+(.+)$", $"<size={h5Size}><b>$1</b></size>",
                RegexOptions.Multiline);
            result = Regex.Replace(result, @"^####\s+(.+)$", $"<size={h4Size}><b>$1</b></size>",
                RegexOptions.Multiline);
            result = Regex.Replace(result, @"^###\s+(.+)$", $"<size={h3Size}><b>$1</b></size>", RegexOptions.Multiline);
            result = Regex.Replace(result, @"^##\s+(.+)$", $"<size={h2Size}><b>$1</b></size>", RegexOptions.Multiline);
            result = Regex.Replace(result, @"^#\s+(.+)$", $"<size={h1Size}><b>$1</b></size>", RegexOptions.Multiline);

            // 转换水平线 --- -> 分隔线（必须在列表项之前处理）
            // 匹配整行只有至少3个连续的 -、* 或 _，前后可能有空格或制表符
            var separator = new string('─', separatorWidth);
            result = Regex.Replace(result, @"^[\s\t]*[-*_]{3,}[\s\t]*(\r?\n|$)",
                $"\n<color=#888888>{separator}</color>\n", RegexOptions.Multiline);

            // 转换多级列表项
            // 按行处理，保留缩进级别
            var lines = result.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // 匹配无序列表项：-、*、+ 开头，前面可能有缩进
                var unorderedMatch = Regex.Match(line, @"^(\s*)([-*+])\s+(.+)$");
                if (unorderedMatch.Success)
                {
                    var indent = unorderedMatch.Groups[1].Value;
                    var content = unorderedMatch.Groups[3].Value;
                    // 根据缩进级别选择不同的符号和统一缩进
                    var indentLevel = GetIndentLevel(indent);
                    var bullet = GetBulletForLevel(indentLevel);
                    var uniformIndent = GetUniformIndent(indentLevel);
                    lines[i] = $"{uniformIndent}{bullet} {content}";
                    continue;
                }

                // 匹配有序列表项：数字. 开头，前面可能有缩进
                var orderedMatch = Regex.Match(line, @"^(\s*)(\d+)\.\s+(.+)$");
                if (orderedMatch.Success)
                {
                    var indent = orderedMatch.Groups[1].Value;
                    var number = orderedMatch.Groups[2].Value;
                    var content = orderedMatch.Groups[3].Value;
                    // 统一缩进格式
                    var indentLevel = GetIndentLevel(indent);
                    var uniformIndent = GetUniformIndent(indentLevel);
                    lines[i] = $"{uniformIndent}{number}. {content}";
                }
            }

            result = string.Join("\n", lines);

            // 清理多余的空白行
            result = Regex.Replace(result, @"\n{3,}", "\n\n", RegexOptions.Multiline);

            // 包裹基础字号标签
            return $"<size={baseFontSize}>{result.Trim()}</size>";
        }

        private static int GetIndentLevel(string indent)
        {
            // 计算缩进级别：每 2 个空格或 1 个制表符为一级
            var level = 0;
            foreach (var c in indent)
                if (c == '\t')
                    level += 2;
                else if (c == ' ')
                    level += 1;
            return level / 2;
        }

        private static string GetBulletForLevel(int level)
        {
            // 根据级别返回不同的符号，循环使用
            return BulletSymbols[level % BulletSymbols.Length];
        }

        private static string GetUniformIndent(int level)
        {
            // 每个级别使用 2 个空格缩进
            return new string(' ', level * 2);
        }
    }
}

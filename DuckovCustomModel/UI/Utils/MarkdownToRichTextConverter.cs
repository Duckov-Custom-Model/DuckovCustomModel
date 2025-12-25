using System.Text.RegularExpressions;

namespace DuckovCustomModel.UI.Utils
{
    public static class MarkdownToRichTextConverter
    {
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

            // 转换列表项 - -> •
            result = Regex.Replace(result, @"^\s*[-*+]\s+", "• ", RegexOptions.Multiline);
            result = Regex.Replace(result, @"^\s*\d+\.\s+", "", RegexOptions.Multiline);

            // 清理多余的空白行
            result = Regex.Replace(result, @"\n{3,}", "\n\n", RegexOptions.Multiline);

            // 包裹基础字号标签
            return $"<size={baseFontSize}>{result.Trim()}</size>";
        }
    }
}

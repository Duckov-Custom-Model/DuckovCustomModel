using System.Text.RegularExpressions;

namespace DuckovCustomModel.UI.Utils
{
    public static class MarkdownToRichTextConverter
    {
        public static string Convert(string markdown)
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

            // 转换标题为字号（# 对应 size=24, ## 对应 size=22, ### 对应 size=20, 等等）
            result = Regex.Replace(result, @"^######\s+(.+)$", "<size=14><b>$1</b></size>", RegexOptions.Multiline);
            result = Regex.Replace(result, @"^#####\s+(.+)$", "<size=16><b>$1</b></size>", RegexOptions.Multiline);
            result = Regex.Replace(result, @"^####\s+(.+)$", "<size=18><b>$1</b></size>", RegexOptions.Multiline);
            result = Regex.Replace(result, @"^###\s+(.+)$", "<size=20><b>$1</b></size>", RegexOptions.Multiline);
            result = Regex.Replace(result, @"^##\s+(.+)$", "<size=22><b>$1</b></size>", RegexOptions.Multiline);
            result = Regex.Replace(result, @"^#\s+(.+)$", "<size=24><b>$1</b></size>", RegexOptions.Multiline);

            // 转换列表项 - -> •
            result = Regex.Replace(result, @"^\s*[-*+]\s+", "• ", RegexOptions.Multiline);
            result = Regex.Replace(result, @"^\s*\d+\.\s+", "", RegexOptions.Multiline);

            // 移除水平线
            result = Regex.Replace(result, @"^---+$", "", RegexOptions.Multiline);

            // 清理多余的空白行
            result = Regex.Replace(result, @"\n{3,}", "\n\n", RegexOptions.Multiline);

            return result.Trim();
        }
    }
}

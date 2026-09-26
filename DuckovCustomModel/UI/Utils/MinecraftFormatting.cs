using System.Text;

namespace DuckovCustomModel.UI.Utils
{
    internal static class MinecraftFormatting
    {
        private static readonly string[] Colors =
        {
            "#000000", "#0000AA", "#00AA00", "#00AAAA",
            "#AA0000", "#AA00AA", "#FFAA00", "#AAAAAA",
            "#555555", "#5555FF", "#55FF55", "#55FFFF",
            "#FF5555", "#FF55FF", "#FFFF55", "#FFFFFF",
        };

        public static string ToTmp(string? source)
        {
            if (string.IsNullOrEmpty(source)) return string.Empty;

            var result = new StringBuilder(source!.Length + 32);
            var plain = new StringBuilder();
            string? color = null;
            var bold = false;
            var italic = false;
            var underline = false;
            var strikethrough = false;

            void FlushPlain()
            {
                if (plain.Length == 0) return;
                result.Append("<noparse>").Append(plain.ToString().Replace("<", "<\u200B"))
                    .Append("</noparse>");
                plain.Clear();
            }

            void CloseStyle()
            {
                if (strikethrough) result.Append("</s>");
                if (underline) result.Append("</u>");
                if (italic) result.Append("</i>");
                if (bold) result.Append("</b>");
                if (color != null) result.Append("</color>");
            }

            void OpenStyle()
            {
                if (color != null) result.Append("<color=").Append(color).Append('>');
                if (bold) result.Append("<b>");
                if (italic) result.Append("<i>");
                if (underline) result.Append("<u>");
                if (strikethrough) result.Append("<s>");
            }

            for (var index = 0; index < source.Length; index++)
            {
                if (source[index] != '§' || index + 1 >= source.Length)
                {
                    plain.Append(source[index]);
                    continue;
                }

                var code = char.ToLowerInvariant(source[index + 1]);
                var colorIndex = "0123456789abcdef".IndexOf(code);
                if (colorIndex < 0 && code != 'r' && code != 'l' && code != 'o' &&
                    code != 'n' && code != 'm' && code != 'k')
                {
                    plain.Append(source[index]);
                    continue;
                }

                FlushPlain();
                CloseStyle();
                index++;
                if (colorIndex >= 0 || code == 'r')
                {
                    color = colorIndex >= 0 ? Colors[colorIndex] : null;
                    bold = italic = underline = strikethrough = false;
                }
                else
                {
                    switch (code)
                    {
                        case 'l': bold = true; break;
                        case 'o': italic = true; break;
                        case 'n': underline = true; break;
                        case 'm': strikethrough = true; break;
                    }
                }

                OpenStyle();
            }

            FlushPlain();
            CloseStyle();
            return result.ToString();
        }
    }
}

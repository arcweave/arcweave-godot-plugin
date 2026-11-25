using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Arcweave.Interpreter
{
    public class Utils
    {
        public static readonly string CODE_HEX_COLOR = "#27b7f5";

        // Matches either an HTML tag (group 1: optional "/", group 2: tag name) or a run of text.
        // A "<" that doesn't start a tag is matched on its own, as text.
        private static readonly Regex TokenRegex = new Regex(@"<(/?)([a-zA-Z][a-zA-Z0-9]*)[^>]*>|[^<]+|<", RegexOptions.Compiled);

        /// <summary>
        /// Converts Arcweave rich text (HTML) to Godot BBCode for a RichTextLabel.
        /// Marks become [b], [i], [u] and [s], line breaks and paragraphs become newlines,
        /// code becomes colored text and other tags are removed. Text is entity-decoded,
        /// and "[" is passed through, so BBCode written in Arcweave text is rendered.
        /// </summary>
        public static string CleanString(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            var sb = new StringBuilder(s.Length);
            foreach (Match match in TokenRegex.Matches(s))
            {
                if (!match.Groups[2].Success)
                {
                    // Decoding each text run on its own means decoded "<", ">" and "&" stay
                    // plain text and can't form tags.
                    sb.Append(WebUtility.HtmlDecode(match.Value));
                    continue;
                }

                var closing = match.Groups[1].Value == "/";
                var slash = closing ? "/" : string.Empty;
                switch (match.Groups[2].Value.ToLowerInvariant())
                {
                    case "strong":
                    case "b":
                        sb.Append("[").Append(slash).Append("b]");
                        break;
                    case "em":
                    case "i":
                        sb.Append("[").Append(slash).Append("i]");
                        break;
                    case "u":
                        sb.Append("[").Append(slash).Append("u]");
                        break;
                    case "s":
                        sb.Append("[").Append(slash).Append("s]");
                        break;
                    case "br":
                        sb.Append("\n");
                        break;
                    case "p":
                        if (closing) sb.Append("\n\n");
                        break;
                    case "code":
                        sb.Append(closing ? "[/color]\n" : string.Format("[color={0}]", CODE_HEX_COLOR));
                        break;
                }
            }
            return sb.ToString().TrimEnd();
        }
    }
}

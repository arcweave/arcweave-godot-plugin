namespace Arcweave.Interpreter
{
    public class Utils
    {
        public static readonly string CODE_HEX_COLOR = "#27b7f5";
        public static string CleanString(string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                s = s.Replace("<strong>", "{bold}").Replace("</strong>", "{/bold}");
                s = s.Replace("<em>", "{italic}").Replace("</em>", "{/italic}");
                s = s.Replace("&lt;", string.Empty).Replace("&gt;", string.Empty);
                s = s.Replace("</p>", "\n\n");
                s = s.Replace("<code>", "{code}");
                s = s.Replace("</code>", "{/code}");
                s = System.Text.RegularExpressions.Regex.Replace(s, @"<[^>]*>", string.Empty);
                s = s.Replace("{bold}", "[b]").Replace("{/bold}", "[/b]");
                s = s.Replace("{italic}", "[i]").Replace("{/italic}", "[/i]");
                s = s.Replace("{code}", string.Format("<color={0}>", CODE_HEX_COLOR));
                s = s.Replace("{/code}", "</color>\n");
                s = s.TrimEnd();
                return s;
            }
            return s;
        }
    }
}

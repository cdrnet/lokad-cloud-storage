namespace Lokad.Cloud.Console.WebRole.Helpers
{
    using System.Text;
    using System.Web.Mvc;

    public static class JavascriptHtmlExtensions
    {
        public static MvcHtmlString Enquote(this HtmlHelper htmlHelper, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return MvcHtmlString.Create(@"""""");
            }

            var sb = new StringBuilder(text.Length + 4);
            sb.Append('"');

            var tokens = text.ToCharArray();
            for (int i = 0; i < tokens.Length; i++)
            {
                char c = tokens[i];
                switch(tokens[i])
                {
                    case '\\':
                    case '"':
                    case '>':
                        sb.Append('\\');
                        sb.Append(c);
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    default:
                        if (c >= ' ')
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }

            sb.Append('"');
            return MvcHtmlString.Create(sb.ToString());
        }
    }
}
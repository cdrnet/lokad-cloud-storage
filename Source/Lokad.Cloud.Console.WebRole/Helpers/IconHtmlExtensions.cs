#region Copyright (c) Lokad 2009-2011
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Web.Mvc;

namespace Lokad.Cloud.Console.WebRole.Helpers
{
    public static class IconHtmlExtensions
    {
        private const string InlineImageHtml = @"<span class=""{0} {1}""></span>";
        private const string InlineImageHtmlClientScript = @"<span class=""{0} {0}-' + {1} + '""></span>";
        private const string InlineImageHtmlButton = @"<span id=""{2}"" class=""{0} {1} icon-button""></span>";

        public static MvcHtmlString GoodBadIcon(this HtmlHelper htmlHelper, bool isGood)
        {
            return MvcHtmlString.Create(string.Format(InlineImageHtml, IconMap.GoodBad, IconMap.GoodBadOf(isGood)));
        }

        public static MvcHtmlString LogLevelIcon(this HtmlHelper htmlHelper, string logLevel)
        {
            return MvcHtmlString.Create(string.Format(InlineImageHtml, IconMap.LogLevels, IconMap.LogLevelsOf(logLevel)));
        }
        public static MvcHtmlString LogLevelIconClientScript(this HtmlHelper htmlHelper, string logLevelExpression)
        {
            return MvcHtmlString.Create(string.Format(InlineImageHtmlClientScript, IconMap.LogLevels, logLevelExpression));
        }

        public static MvcHtmlString OkCancelIcon(this HtmlHelper htmlHelper, bool isOk)
        {
            return MvcHtmlString.Create(string.Format(InlineImageHtml, IconMap.OkCancel, IconMap.OkCancelOf(isOk)));
        }

        public static MvcHtmlString PlusMinusIcon(this HtmlHelper htmlHelper, bool isPlus)
        {
            return MvcHtmlString.Create(string.Format(InlineImageHtml, IconMap.PlusMinus, IconMap.PlusMinusOf(isPlus)));
        }

        public static MvcHtmlString StartStopIcon(this HtmlHelper htmlHelper, bool isStart)
        {
            return MvcHtmlString.Create(string.Format(InlineImageHtml, IconMap.StartStop, IconMap.StartStopOf(isStart)));
        }
        public static MvcHtmlString StartStopIconButton(this HtmlHelper htmlHelper, bool isStart, string id)
        {
            return MvcHtmlString.Create(string.Format(InlineImageHtmlButton, IconMap.StartStop, IconMap.StartStopOf(isStart), id));
        }
    }
}
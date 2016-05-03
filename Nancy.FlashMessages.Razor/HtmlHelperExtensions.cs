using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nancy.FlashMessages.Extensions;
using Nancy.ViewEngines.Razor;

namespace Nancy.FlashMessages
{
    public static class HtmlHelperExtensions
    {
        public static IHtmlString FlashMessages<T>(this HtmlHelpers<T> helpers, string messageType)
        {
            var ctx = helpers.RenderContext.Context;
            var flashMessages = ctx.GetFlashMessages();
            var renderer = flashMessages.Configuration.GetRenderer();

            IEnumerable<string> messages = null;
            if (ctx.ViewBag.FlashMessages.HasValue)
            {
                IDictionary<string, IList<string>> messagesAll = ctx.ViewBag.FlashMessages.Value;
                if (messagesAll.ContainsKey(messageType))
                    messages = messagesAll[messageType];
            }
            return new NonEncodedHtmlString(renderer.Render(messageType, messages));
        }
    }
}

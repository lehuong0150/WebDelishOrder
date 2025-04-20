using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebDelishOrder.Helpers
{
    public static class ActivePageHelper
    {
        public static string IsActive(this IHtmlHelper html, string page)
        {
            var activePage = html.ViewData["ActivePage"] as string;
            return page == activePage ? "active" : "";
        }
    }
}
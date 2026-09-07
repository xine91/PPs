using Microsoft.AspNetCore.Mvc;

namespace topfact.Pulse.Controllers
{
    public class PlaceholderController : Controller
    {
        [HttpGet]
        public IActionResult Placeholder(string? page)
        {
            ViewData["Title"] = string.IsNullOrWhiteSpace(page) ? "Placeholder" : page;
            ViewData["PlaceholderTitle"] = string.IsNullOrWhiteSpace(page) ? "Placeholder" : page;
            ViewData["Bereich"] = HttpContext.Request.RouteValues.TryGetValue("bereich", out var bereich)
                ? bereich?.ToString()
                : null;

            return View();
        }
    }
}

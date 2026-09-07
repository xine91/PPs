using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;
using topfact.Pulse.Data;
using topfact.Pulse.Models;
using topfact.Pulse.Services;

namespace topfact.Pulse.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AccountController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppDbContext _db;
        private readonly IPulseAuditService _auditService;

        public AccountController(
            ILogger<AccountController> logger,
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            AppDbContext db,
            IPulseAuditService auditService)
        {
            _logger = logger;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _db = db;
            _auditService = auditService;
        }

        [HttpGet("/login/index")]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Dashboard", "Home");

            return View();
        }

        [HttpPost("/login/index")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            // DEV-Login: Nur für Entwicklungszwecke – jede @topfact.de Adresse mit beliebigem Passwort
            if (!string.IsNullOrWhiteSpace(model.Username) &&
                model.Username.Trim().EndsWith("@topfact.de", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(model.Password))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.Username.Trim()),
                    new Claim(ClaimTypes.Email, model.Username.Trim()),
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim("UserNumber", "DEV-001"),
                    new Claim("Realm", "topfact"),
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties { IsPersistent = model.RememberMe });

                await _auditService.LogLoginAsync(model.Username, success: true);

                return Json(new LoginResult
                {
                    Success = true,
                    RedirectUrl = $"{HttpContext.Request.PathBase}/Home/Dashboard"
                });
            }

            await _auditService.LogLoginAsync(model.Username, success: false);

            return Json(new LoginResult
            {
                Success = false,
                Message = "Ungültige Anmeldedaten. Nur @topfact.de Adressen sind erlaubt."
            });
        }

        [HttpGet("/login/logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return Redirect($"{HttpContext.Request.PathBase}/login/index");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult Profile()
        {
            var vm = new UserProfileViewModel
            {
                Username = User.FindFirstValue(ClaimTypes.Name),
                Email = User.FindFirstValue(ClaimTypes.Email),
                UserNumber = User.FindFirstValue("UserNumber"),
                Realm = User.FindFirstValue("Realm"),
                UserGuid = User.FindFirstValue("UserGuid"),
                OrgGuid = User.FindFirstValue("OrgGuid"),
                UserId = int.TryParse(User.FindFirstValue("UserId"), out var uid) ? uid : 0,
                OrgId = int.TryParse(User.FindFirstValue("OrgId"), out var oid) ? oid : 0
            };
            vm.AvatarUrl = UserProfileViewModel.GetGravatarUrl(vm.Email, 200);

            return View(vm);
        }

        private string? ResolveClientIp()
        {
            var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                var firstIp = forwardedFor.Split(',')[0].Trim();
                if (!string.IsNullOrWhiteSpace(firstIp) && firstIp != "::1" && firstIp != "127.0.0.1")
                    return firstIp;
            }

            var remoteIp = HttpContext.Connection.RemoteIpAddress;
            if (remoteIp is null || IPAddress.IsLoopback(remoteIp))
                return null;

            if (remoteIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                var mapped = remoteIp.MapToIPv4();
                if (!IPAddress.IsLoopback(mapped))
                    return mapped.ToString();
            }

            return remoteIp.ToString();
        }
    }
}

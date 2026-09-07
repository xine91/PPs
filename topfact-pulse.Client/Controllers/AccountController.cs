using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;
using topfact.Pulse.Data;
using topfact.Pulse.Models;

namespace topfact.Pulse.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AccountController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppDbContext _db;

        public AccountController(
            ILogger<AccountController> logger,
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            AppDbContext db)
        {
            _logger = logger;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _db = db;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return Redirect($"{HttpContext.Request.PathBase}/Home/Dashboard");

            return View();
        }

        [HttpPost]
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
                    new Claim(ClaimTypes.Role, "User"),
                    new Claim("UserNumber", "DEV-001"),
                    new Claim("Realm", "topfact"),
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties { IsPersistent = model.RememberMe });

                await LogLoginAttempt(model.Username, success: true);

                return Json(new LoginResult
                {
                    Success = true,
                    RedirectUrl = $"{HttpContext.Request.PathBase}/Home/Dashboard"
                });
            }

            await LogLoginAttempt(model.Username, success: false);

            return Json(new LoginResult
            {
                Success = false,
                Message = "Ungültige Anmeldedaten. Nur @topfact.de Adressen sind erlaubt."
            });
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
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

        private async Task LogLoginAttempt(string? username, bool success)
        {
            var ip = ResolveClientIp();
            var loginTime = DateTime.UtcNow;
            var safeUsername = string.IsNullOrWhiteSpace(username) ? "unknown" : username.Trim();

            try
            {
                // Korrekte Verwendung von SqlParametern mit expliziten Typen
                var userNameParam = new Microsoft.Data.SqlClient.SqlParameter("@user_name", System.Data.SqlDbType.NVarChar, 200) { Value = safeUsername };
                var loginTimeParam = new Microsoft.Data.SqlClient.SqlParameter("@login_time", System.Data.SqlDbType.DateTime) { Value = loginTime };
                var successParam = new Microsoft.Data.SqlClient.SqlParameter("@success", System.Data.SqlDbType.Bit) { Value = success };
                var ipParam = new Microsoft.Data.SqlClient.SqlParameter("@ip_address", System.Data.SqlDbType.NVarChar, 50)
                {
                    Value = (object?)ip ?? DBNull.Value
                };

                await _db.Database.ExecuteSqlRawAsync(
                    "INSERT INTO [dbo].[Pulse_Logins] ([user_name], [login_time], [success], [ip_address]) VALUES (@user_name, @login_time, @success, @ip_address)",
                    userNameParam, loginTimeParam, successParam, ipParam);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Login-Log konnte nicht gespeichert werden. User={User}", safeUsername);
            }
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

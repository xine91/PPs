using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using topfact.Pulse.Data;
using topfact.Pulse.Models;

namespace topfact.CloudManager.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AccountController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppDbContext _db;

        public AccountController(ILogger<AccountController> logger, IConfiguration config, IHttpClientFactory httpClientFactory, AppDbContext db)
        {
            _logger = logger;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _db = db;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("/pulse/login/index")]
        [HttpGet("/login/index")]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost("/pulse/login/index")]
        [HttpPost("/login/index")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            var url = _config["CloudSettings:topfact6Api"];

            using var api = new topfact6.API.ApiClient.ApiClient(url);

            var token = api.UserRepository.Logon(model.Username, model.Password);

            _logger.LogInformation("Token: {key}, UserId: {id}", token?.AccessKey, token?.UserId);

            if (token != null)
            {
                await LogLoginAttempt(model.Username, success: true);

                var claims = new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim(ClaimTypes.Name,           token.Username   ?? string.Empty),
                    new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, token.UserId.ToString()),
                    new System.Security.Claims.Claim(ClaimTypes.Email,          token.EMail      ?? string.Empty),  
                    new System.Security.Claims.Claim("AuthProvider",  "Cookie"), 
                    new System.Security.Claims.Claim("UserId",        token.UserId.ToString()),
                    new System.Security.Claims.Claim("UserGuid",      token.UserGuid   ?? string.Empty),
                    new System.Security.Claims.Claim("UserNumber",    token.UserNumber ?? string.Empty),
                    new System.Security.Claims.Claim("OrgId",         token.OrgId.ToString()),
                    new System.Security.Claims.Claim("OrgGuid",       token.OrgGuid    ?? string.Empty),
                    new System.Security.Claims.Claim("Realm",         token.Realm      ?? string.Empty),
                    new System.Security.Claims.Claim(ClaimTypes.Role, "Mode.Technik"),
                    new System.Security.Claims.Claim(ClaimTypes.Role, "Mode.Organisation"),
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                HttpContext.Session.SetString("Username", token.Username ?? string.Empty);
                HttpContext.Session.SetInt32("UserId", token.UserId);
                HttpContext.Session.SetInt32("OrgId", token.OrgId);
                HttpContext.Session.SetString("AccessKey", token.AccessKey ?? string.Empty);
                HttpContext.Session.SetString("AvatarUrl", UserProfileViewModel.GetGravatarUrl(token.EMail, 32));

                return Json(new LoginResult
                {
                    Success = true,
                    Message = "Anmeldung erfolgreich",
                    Token = token.AccessKey,
                    RedirectUrl = Url.Action("Dashboard", "Home", new { bereich = "Technik" }),
                    User = new UserInfo
                    {
                        Username = token.Username,
                        LoginTime = DateTime.Now
                    }
                });
            }
            else
            {
                await LogLoginAttempt(model.Username, success: false);
                ModelState.AddModelError("", "Ungültiger Benutzername oder Kennwort");
            }

            return Json(new LoginResult
            {
                Success = false,
                Message = "Ungültiger Benutzername oder Kennwort"
            });
        }

        [HttpGet("/pulse/login/entra")]
        [HttpGet("/login/entra")]
        [AllowAnonymous]
        public IActionResult EntraLogin()
        {
            var redirectUrl = Url.Action("Dashboard", "Home", new { bereich = "Technik" });
            var result = Challenge(new AuthenticationProperties { RedirectUri = redirectUrl }, "EntraId");
            return result;
        }

        [HttpGet("/pulse/login/logout")]
        [HttpGet("/login/logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/pulse/login/index");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
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
            HttpContext.Session.SetString("AvatarUrl", UserProfileViewModel.GetGravatarUrl(vm.Email, 32));

            var accessKey = HttpContext.Session.GetString("AccessKey");
            if (string.IsNullOrEmpty(accessKey))
            {
                vm.ErrorMessage = "Kein AccessKey vorhanden. Bitte erneut anmelden.";
                return View(vm);
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessKey);

                var response = await client.GetAsync("https://app.topfactcloud.de/topfact/api/api/User");

                if (!response.IsSuccessStatusCode)
                {
                    vm.ErrorMessage = $"API-Fehler: {(int)response.StatusCode} {response.ReasonPhrase}";
                    return View(vm);
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    // Profilbild-Felder als Base64-Data-URL aufbereiten
                    var isImageField = prop.Name.Equals("UserImage", StringComparison.OrdinalIgnoreCase)
                                    || prop.Name.Equals("Photo",     StringComparison.OrdinalIgnoreCase)
                                    || prop.Name.Equals("Image",     StringComparison.OrdinalIgnoreCase)
                                    || prop.Name.Equals("Avatar",    StringComparison.OrdinalIgnoreCase)
                                    || prop.Name.Equals("Picture",   StringComparison.OrdinalIgnoreCase);

                    if (isImageField && prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var raw = prop.Value.GetString() ?? "";
                        if (!string.IsNullOrWhiteSpace(raw))
                        {
                            // Falls bereits Data-URL → direkt übernehmen
                            // Sonst als JPEG-Base64-Data-URL wrappen
                            var dataUrl = raw.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                                ? raw
                                : $"data:image/jpeg;base64,{raw}";
                            vm.UserImageBase64 = dataUrl;
                            // Auch im Session-Cache speichern (32-px Header-Avatar)
                            HttpContext.Session.SetString("AvatarBase64", dataUrl);
                        }
                    }

                    var value = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString() ?? "",
                        JsonValueKind.Number => prop.Value.GetRawText(),
                        JsonValueKind.True => "Ja",
                        JsonValueKind.False => "Nein",
                        JsonValueKind.Null => "",
                        _ => prop.Value.GetRawText()
                    };
                    vm.ApiProperties[prop.Name] = value;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Abrufen der Benutzerdaten von der API");
                vm.ErrorMessage = "Verbindung zur API fehlgeschlagen.";
            }

            return View(vm);
        }

        private async Task LogLoginAttempt(string? username, bool success)
        {
            var ip = ResolveClientIp();
            var loginTime = DateTime.UtcNow;
            var safeUsername = string.IsNullOrWhiteSpace(username) ? "unknown" : username.Trim();

            try
            {
                var rows = await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"INSERT INTO [dbo].[Pulse_Logins] ([user_name], [login_time], [success], [ip_address]) VALUES ({safeUsername}, {loginTime}, {success}, {ip})");

                if (rows <= 0)
                {
                    _logger.LogWarning("Login-Log wurde nicht gespeichert (0 Rows). User={User}, Success={Success}, Ip={Ip}", safeUsername, success, ip);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Insert in [dbo].[Pulse_Logins] fehlgeschlagen. Fallback ohne Schema wird versucht.");

                try
                {
                    var rows = await _db.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO [Pulse_Logins] ([user_name], [login_time], [success], [ip_address]) VALUES ({safeUsername}, {loginTime}, {success}, {ip})");

                    if (rows <= 0)
                    {
                        _logger.LogWarning("Login-Log (Fallback) wurde nicht gespeichert (0 Rows). User={User}, Success={Success}, Ip={Ip}", safeUsername, success, ip);
                    }
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fehler beim Speichern des Login-Logs. User={User}, Success={Success}, Ip={Ip}", safeUsername, success, ip);
                }
            }
        }

        private string? ResolveClientIp()
        {
            var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                var firstIp = forwardedFor.Split(',')[0].Trim();
                if (!string.IsNullOrWhiteSpace(firstIp) && firstIp != "::1" && firstIp != "127.0.0.1")
                {
                    return firstIp;
                }
            }

            var realIp = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(realIp) && realIp != "::1" && realIp != "127.0.0.1")
            {
                return realIp.Trim();
            }

            var remoteIp = HttpContext.Connection.RemoteIpAddress;
            if (remoteIp is null)
            {
                return null;
            }

            if (IPAddress.IsLoopback(remoteIp))
            {
                return null;
            }

            if (remoteIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                var mapped = remoteIp.MapToIPv4();
                if (!IPAddress.IsLoopback(mapped))
                {
                    return mapped.ToString();
                }
            }

            return remoteIp.ToString();
        }
    }
}
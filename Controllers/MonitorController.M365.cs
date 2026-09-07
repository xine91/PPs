using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Globalization;
using topfact.Pulse.Models;

namespace topfact.Pulse.Controllers
{
    public partial class MonitorController
    {
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> M365(
            int selectedDays = 30,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var oneYearAgo = DateTime.Now.AddDays(-365);
            var configuredChecks = _configuration.GetSection("M365TokenChecks").Get<List<M365TokenCheckConfig>>() ?? new List<M365TokenCheckConfig>();

            var baseFilterQuery = _db.ClientStartupInformations
                .AsNoTracking()
                .Where(x => x.DateCreated != null && x.DateCreated > oneYearAgo)
                .Where(x => x.AppName != null && x.AppName.Contains("M365"));

            var (rangeStart, rangeEnd) = GetRange(selectedDays, dateFrom, dateTo, true);

            var entries = await baseFilterQuery
                .Where(x => x.DateCreated >= rangeStart && x.DateCreated < rangeEnd)
                .OrderByDescending(x => x.DateCreated)
                .Take(10000)
                .Select(x => new M365Entry
                {
                    Tenant = x.Customer ?? "Unbekannt",
                    AppName = x.AppName ?? "Unbekannt",
                    Date = x.DateCreated,
                    Days = x.DateCreated.HasValue
                        ? (int)Math.Floor((DateTime.Today - x.DateCreated.Value.Date).TotalDays)
                        : 0
                })
                .ToListAsync();

            if (configuredChecks.Count > 0)
            {
                var configuredTenants = configuredChecks
                    .Select(x => !string.IsNullOrWhiteSpace(x.Customer) ? x.Customer : x.TenantId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var latestActivityLookup = await _db.ClientStartupInformations
                    .AsNoTracking()
                    .Where(x => x.DateCreated != null && x.DateCreated > oneYearAgo)
                    .Where(x => x.Customer != null && configuredTenants.Contains(x.Customer))
                    .GroupBy(x => x.Customer!)
                    .Select(g => g
                        .OrderByDescending(x => x.DateCreated)
                        .Select(x => new
                        {
                            Tenant = g.Key,
                            AppName = x.AppName,
                            DateCreated = x.DateCreated
                        })
                        .FirstOrDefault())
                    .ToListAsync();

                var existingTenants = new HashSet<string>(entries.Select(x => x.Tenant), StringComparer.OrdinalIgnoreCase);

                foreach (var check in configuredChecks)
                {
                    var tenantName = !string.IsNullOrWhiteSpace(check.Customer) ? check.Customer : check.TenantId;
                    if (string.IsNullOrWhiteSpace(tenantName) || existingTenants.Contains(tenantName))
                        continue;

                    var latestActivity = latestActivityLookup.FirstOrDefault(x => x != null && string.Equals(x.Tenant, tenantName, StringComparison.OrdinalIgnoreCase));

                    entries.Add(new M365Entry
                    {
                        Tenant = tenantName,
                        AppName = latestActivity?.AppName ?? "M365 Token Check",
                        Date = latestActivity?.DateCreated,
                        Days = latestActivity?.DateCreated.HasValue == true
                            ? (int)Math.Floor((DateTime.Today - latestActivity.DateCreated.Value.Date).TotalDays)
                            : 0
                    });
                }

                entries = entries
                    .OrderBy(x => x.Tenant)
                    .ThenByDescending(x => x.Date)
                    .ToList();
            }

            Dictionary<string, M365TokenValidationResult> tokenChecks;

            try
            {
                tokenChecks = await ValidateM365TokensAsync(entries.Select(x => x.Tenant).Distinct());
            }
            catch
            {
                tokenChecks = new Dictionary<string, M365TokenValidationResult>(StringComparer.OrdinalIgnoreCase);
            }

            foreach (var entry in entries)
            {
                if (tokenChecks.TryGetValue(entry.Tenant, out var tokenCheck))
                {
                    entry.TokenStatus = tokenCheck.Status;
                    entry.TokenDetails = tokenCheck.Details;

                    if (tokenCheck.PasswordCredentialEndDate.HasValue)
                    {
                        entry.Date = tokenCheck.PasswordCredentialEndDate.Value;
                        entry.Days = tokenCheck.DaysUntilPasswordCredentialEnd ?? entry.Days;
                    }
                }
            }

            var vm = new M365ViewModel
            {
                SelectedDays = selectedDays,
                DateFrom = selectedDays == 0 ? dateFrom : null,
                DateTo = selectedDays == 0 ? dateTo : null,
                Entries = entries
            };

            return View(vm);
        }

        private async Task<Dictionary<string, M365TokenValidationResult>> ValidateM365TokensAsync(IEnumerable<string> tenants)
        {
            var configuredChecks = _configuration.GetSection("M365TokenChecks").Get<List<M365TokenCheckConfig>>() ?? new List<M365TokenCheckConfig>();
            var requestedTenants = new HashSet<string>(tenants.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.OrdinalIgnoreCase);
            var results = new Dictionary<string, M365TokenValidationResult>(StringComparer.OrdinalIgnoreCase);

            if (configuredChecks.Count == 0 || requestedTenants.Count == 0)
                return results;

            var relevantChecks = configuredChecks
                .Where(x => !string.IsNullOrWhiteSpace(x.Customer) || !string.IsNullOrWhiteSpace(x.TenantId))
                .Where(x =>
                    (!string.IsNullOrWhiteSpace(x.Customer) && requestedTenants.Contains(x.Customer)) ||
                    (!string.IsNullOrWhiteSpace(x.TenantId) && requestedTenants.Contains(x.TenantId)))
                .ToList();

            if (relevantChecks.Count == 0)
                return results;

            using var http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            foreach (var check in relevantChecks)
            {
                M365TokenValidationResult validation;

                try
                {
                    validation = await ValidateM365TokenAsync(http, check);
                }
                catch (Exception ex)
                {
                    validation = new M365TokenValidationResult
                    {
                        Status = "Fehler",
                        Details = ex.Message
                    };
                }

                if (!string.IsNullOrWhiteSpace(check.Customer))
                    results[check.Customer] = validation;

                if (!string.IsNullOrWhiteSpace(check.TenantId))
                    results[check.TenantId] = validation;
            }

            return results;
        }

        private static async Task<M365TokenValidationResult> ValidateM365TokenAsync(HttpClient http, M365TokenCheckConfig check)
        {
            if (string.IsNullOrWhiteSpace(check.TenantId) ||
                string.IsNullOrWhiteSpace(check.ClientId) ||
                string.IsNullOrWhiteSpace(check.ClientSecret))
            {
                return new M365TokenValidationResult
                {
                    Status = "Konfiguration fehlt",
                    Details = "TenantId, ClientId oder ClientSecret ist leer."
                };
            }

            using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/{Uri.EscapeDataString(check.TenantId)}/oauth2/v2.0/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = check.ClientId,
                    ["client_secret"] = check.ClientSecret,
                    ["scope"] = "https://graph.microsoft.com/.default",
                    ["grant_type"] = "client_credentials"
                })
            };

            using var tokenResponse = await http.SendAsync(tokenRequest);
            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();

            if (!tokenResponse.IsSuccessStatusCode)
            {
                return new M365TokenValidationResult
                {
                    Status = "Ungültig",
                    Details = ExtractM365ErrorMessage(tokenJson, $"Token-Endpoint: {(int)tokenResponse.StatusCode}")
                };
            }

            using var tokenDoc = JsonDocument.Parse(tokenJson);
            if (!tokenDoc.RootElement.TryGetProperty("access_token", out var accessTokenElement) || accessTokenElement.ValueKind != JsonValueKind.String)
            {
                return new M365TokenValidationResult
                {
                    Status = "Ungültig",
                    Details = "Kein access_token in der Antwort enthalten."
                };
            }

            var accessToken = accessTokenElement.GetString();
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return new M365TokenValidationResult
                {
                    Status = "Ungültig",
                    Details = "access_token ist leer."
                };
            }

            using var graphRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://graph.microsoft.com/v1.0/applications?$filter={Uri.EscapeDataString($"appId eq '{check.ClientId}'")}");
            graphRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var graphResponse = await http.SendAsync(graphRequest);
            var graphJson = await graphResponse.Content.ReadAsStringAsync();

            if (!graphResponse.IsSuccessStatusCode)
            {
                return new M365TokenValidationResult
                {
                    Status = "Ungültig",
                    Details = ExtractM365ErrorMessage(graphJson, $"Graph-Endpoint: {(int)graphResponse.StatusCode}")
                };
            }

            using var graphDoc = JsonDocument.Parse(graphJson);
            var credentialEndDate = TryGetPasswordCredentialEndDate(graphDoc.RootElement);
            var daysUntilCredentialEnd = credentialEndDate.HasValue
                ? (credentialEndDate.Value.Date - DateTime.UtcNow.Date).Days
                : (int?)null;

            if (graphDoc.RootElement.TryGetProperty("value", out var valueElement) &&
                valueElement.ValueKind == JsonValueKind.Array &&
                valueElement.GetArrayLength() > 0)
            {
                return new M365TokenValidationResult
                {
                    Status = "Gültig",
                    Details = credentialEndDate.HasValue
                        ? $"Token gültig. Passwort-Credential läuft am {credentialEndDate.Value.ToLocalTime():dd.MM.yyyy HH:mm} ab."
                        : "Token erfolgreich abgerufen und Anwendung in Microsoft Graph gefunden.",
                    PasswordCredentialEndDate = credentialEndDate,
                    DaysUntilPasswordCredentialEnd = daysUntilCredentialEnd
                };
            }

            return new M365TokenValidationResult
            {
                Status = "Prüfen",
                Details = credentialEndDate.HasValue
                    ? $"Token gültig. Passwort-Credential läuft am {credentialEndDate.Value.ToLocalTime():dd.MM.yyyy HH:mm} ab, aber App wurde nicht gefunden."
                    : "Token ist gültig, aber die Anwendung wurde in Microsoft Graph nicht gefunden.",
                PasswordCredentialEndDate = credentialEndDate,
                DaysUntilPasswordCredentialEnd = daysUntilCredentialEnd
            };
        }

        private static DateTime? TryGetPasswordCredentialEndDate(JsonElement graphRoot)
        {
            if (!graphRoot.TryGetProperty("value", out var valueElement) || valueElement.ValueKind != JsonValueKind.Array)
                return null;

            var nowUtc = DateTime.UtcNow;
            var allCredentialEndDates = new List<DateTime>();

            foreach (var app in valueElement.EnumerateArray())
            {
                if (!app.TryGetProperty("passwordCredentials", out var credentialsElement) || credentialsElement.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var credential in credentialsElement.EnumerateArray())
                {
                    if (!credential.TryGetProperty("endDateTime", out var endDateElement) || endDateElement.ValueKind != JsonValueKind.String)
                        continue;

                    var endDateRaw = endDateElement.GetString();
                    if (string.IsNullOrWhiteSpace(endDateRaw))
                        continue;

                    if (DateTime.TryParse(
                        endDateRaw,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out var endDateUtc))
                    {
                        allCredentialEndDates.Add(endDateUtc);
                    }
                }
            }

            if (allCredentialEndDates.Count == 0)
                return null;

            var nextValid = allCredentialEndDates
                .Where(x => x >= nowUtc)
                .OrderBy(x => x)
                .FirstOrDefault();

            if (nextValid != default)
                return nextValid;

            return allCredentialEndDates
                .OrderByDescending(x => x)
                .FirstOrDefault();
        }

        private static string ExtractM365ErrorMessage(string json, string fallback)
        {
            if (string.IsNullOrWhiteSpace(json))
                return fallback;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("error_description", out var errorDescription) && errorDescription.ValueKind == JsonValueKind.String)
                    return errorDescription.GetString() ?? fallback;

                if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
                    return error.GetString() ?? fallback;

                if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                    return message.GetString() ?? fallback;
            }
            catch
            {
            }

            return fallback;
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using topfact.Pulse.Models;

namespace topfact.Pulse.Controllers
{
    public partial class MonitorController
    {
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Speicherplatz(
            int selectedDays = 30,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var entries = await FetchStorageEntriesFromApiAsync();

            var vm = new StorageViewModel
            {
                SelectedDays = selectedDays,
                DateFrom = selectedDays == 0 ? dateFrom : null,
                DateTo = selectedDays == 0 ? dateTo : null,
                Entries = entries
            };

            return View(vm);
        }

        private async Task<List<StorageEntry>> FetchStorageEntriesFromApiAsync()
        {
            using var http = new HttpClient();

            var accessKey = await GetTopfactAccessKeyAsync(http);
            if (string.IsNullOrWhiteSpace(accessKey))
                return new List<StorageEntry>();

            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessKey);

            using var response = await http.GetAsync("https://app.topfactcloud.de/0002/topfact6/api/DataView/81fda0d1-805a-4fc7-a1e3-b7ef062b5eb7");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return ParseStorageEntries(json);
        }

        private static async Task<string?> GetTopfactAccessKeyAsync(HttpClient http)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://app.topfactcloud.de/0002/topfact/api/api/auth/user");
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes("topfact.pulse@topfact.de:TopfactPulse2026!"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

            using var response = await http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (doc.RootElement.TryGetProperty("accessKey", out var ak1) && ak1.ValueKind == JsonValueKind.String)
                    return ak1.GetString();
                if (doc.RootElement.TryGetProperty("AccessKey", out var ak2) && ak2.ValueKind == JsonValueKind.String)
                    return ak2.GetString();
                if (doc.RootElement.TryGetProperty("token", out var t1) && t1.ValueKind == JsonValueKind.String)
                    return t1.GetString();
                if (doc.RootElement.TryGetProperty("Token", out var t2) && t2.ValueKind == JsonValueKind.String)
                    return t2.GetString();
            }

            return null;
        }

        private static List<StorageEntry> ParseStorageEntries(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var rows = FindRowsArray(doc.RootElement);
                if (rows is null)
                    return new List<StorageEntry>();

                var result = new List<StorageEntry>();
                foreach (var row in rows.Value.EnumerateArray())
                {
                    var orgId = GetStringAny(row, "OrgId", "OrgID", "Org_Id", "orgId", "id") ?? string.Empty;
                    var organisation = GetStringAny(row, "Organisation", "Organization", "OrgName", "Mandant", "Tenant") ?? string.Empty;
                    var anzahlArchive = GetIntAny(row, "AnzahlArchive", "Anzahl_Archive", "ArchiveCount", "Anzahl") ?? 0;
                    var speicherplatz = GetDecimalAny(row, "Speicherplatz_GB", "Speicherplatz", "Storage", "UsedGb", "UsedGB", "DiskUsedGB") ?? 0m;
                    var lizenzSpeicherplatz = GetDecimalAny(row, "Lizenz_Speicherplatz_GB", "Lizenz_Speicherplatz", "LizenzSpeicherplatz", "LicenseStorage", "QuotaGb", "QuotaGB", "DiskTotalGB") ?? 0m;
                    var wachstumProTag = GetDecimalAny(row, "Wachstum_GB_pro_Tag", "Wachstum_pro_Tag", "WachstumProTag", "GrowthPerDay", "ConsumptionPerDayGb", "ConsumptionGBPerDay") ?? 0m;
                    var tageBisGrenze = GetIntAny(row, "Tage_bis_Grenze_erreicht", "Tage_bis_Grenze", "TageBisGrenze", "DaysUntilQuota", "DaysUntilLimit") ?? 0;

                    result.Add(new StorageEntry
                    {
                        OrgId = orgId,
                        Organisation = organisation,
                        AnzahlArchive = anzahlArchive,
                        Speicherplatz = speicherplatz,
                        Lizenz_Speicherplatz = lizenzSpeicherplatz,
                        Wachstum_pro_Tag = wachstumProTag,
                        Tage_bis_Grenze = tageBisGrenze
                    });
                }

                return result.OrderBy(x => x.Organisation).ToList();
            }
            catch
            {
                return new List<StorageEntry>();
            }
        }

        private static string? GetStringAny(JsonElement obj, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                var value = GetString(obj, name);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }
            return null;
        }

        private static decimal? GetDecimalAny(JsonElement obj, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                var value = GetDecimal(obj, name);
                if (value.HasValue)
                    return value;
            }
            return null;
        }

        private static int? GetIntAny(JsonElement obj, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                var value = GetInt(obj, name);
                if (value.HasValue)
                    return value;
            }
            return null;
        }

        private static JsonElement? FindRowsArray(JsonElement root)
        {
            if (root.ValueKind == JsonValueKind.Array)
                return root;

            if (root.ValueKind != JsonValueKind.Object)
                return null;

            foreach (var name in new[] { "Data", "data", "Rows", "rows", "Value", "value", "Result", "result" })
            {
                if (root.TryGetProperty(name, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.Array)
                        return prop;

                    if (prop.ValueKind == JsonValueKind.String)
                    {
                        var raw = prop.GetString();
                        if (!string.IsNullOrWhiteSpace(raw))
                        {
                            using var nestedDoc = JsonDocument.Parse(raw);
                            if (nestedDoc.RootElement.ValueKind == JsonValueKind.Array)
                                return nestedDoc.RootElement.Clone();
                            var nestedArray = FindRowsArray(nestedDoc.RootElement);
                            if (nestedArray is not null)
                                return nestedArray.Value.Clone();
                        }
                    }

                    if (prop.ValueKind == JsonValueKind.Object)
                    {
                        var nested = FindRowsArray(prop);
                        if (nested is not null)
                            return nested;
                    }
                }
            }

            foreach (var name in new[] { "rows", "Rows", "data", "Data", "result", "Result", "value", "Value" })
            {
                if (root.TryGetProperty(name, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.Array)
                        return prop;
                    if (prop.ValueKind == JsonValueKind.Object)
                    {
                        var nested = FindRowsArray(prop);
                        if (nested is not null)
                            return nested;
                    }
                }
            }

            return null;
        }

        private static string? GetString(JsonElement obj, string propertyName)
        {
            if (obj.ValueKind != JsonValueKind.Object)
                return null;
            if (!obj.TryGetProperty(propertyName, out var prop))
                return null;
            if (prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
            return prop.ToString();
        }

        private static decimal? GetDecimal(JsonElement obj, string propertyName)
        {
            if (obj.ValueKind != JsonValueKind.Object)
                return null;
            if (!obj.TryGetProperty(propertyName, out var prop))
                return null;

            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var d))
                return d;
            if (prop.ValueKind == JsonValueKind.String && decimal.TryParse(prop.GetString(), out var ds))
                return ds;
            return null;
        }

        private static int? GetInt(JsonElement obj, string propertyName)
        {
            if (obj.ValueKind != JsonValueKind.Object)
                return null;
            if (!obj.TryGetProperty(propertyName, out var prop))
                return null;

            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var i))
                return i;

            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var decimalNumber))
                return (int)Math.Round(decimalNumber, MidpointRounding.AwayFromZero);

            if (prop.ValueKind == JsonValueKind.String)
            {
                var raw = prop.GetString();
                if (string.IsNullOrWhiteSpace(raw))
                    return null;

                if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var isv))
                    return isv;

                if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.CurrentCulture, out isv))
                    return isv;

                if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalStringValue))
                    return (int)Math.Round(decimalStringValue, MidpointRounding.AwayFromZero);

                if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out decimalStringValue))
                    return (int)Math.Round(decimalStringValue, MidpointRounding.AwayFromZero);
            }

            return null;
        }
    }
}

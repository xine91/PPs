using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using topfact.Pulse.Data;
using topfact.Pulse.Models;
using Microsoft.Data.SqlClient;

namespace topfact.Pulse.Services
{
    public interface IConnectorService
    {
        Task SaveConnectorAsync(string connectorId, object config);
        Task<(bool success, string message)> TestConnectionAsync(string connectorId);
        Task<Dictionary<string, object>> GetConnectorConfigAsync(string connectorId);
    }

    public class ConnectorService : IConnectorService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ConnectorService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetCurrentUser()
        {
            return _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "system";
        }

        public async Task SaveConnectorAsync(string connectorId, object config)
        {
            var userName = GetCurrentUser();

            // Extrahiere Bezeichnung, Status und kategorieID BEVOR die Config serialisiert wird
            string? bezeichnung = null;
            string? status = null;
            int? kategorieID = null;

            if (config is System.Text.Json.JsonElement jsonConfig)
            {
                if (jsonConfig.TryGetProperty("bezeichnung", out var bez))
                {
                    bezeichnung = bez.GetString();
                }
                if (jsonConfig.TryGetProperty("status", out var stat))
                {
                    status = stat.GetString();
                }
                if (jsonConfig.TryGetProperty("kategorieID", out var kat))
                {
                    if (kat.TryGetInt32(out int katId))
                    {
                        kategorieID = katId;
                    }
                }
            }
            else if (config is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue("bezeichnung", out var bez))
                {
                    bezeichnung = bez?.ToString();
                }
                if (dict.TryGetValue("status", out var stat))
                {
                    status = stat?.ToString();
                }
                if (dict.TryGetValue("kategorieID", out var kat))
                {
                    if (kat is int katId)
                    {
                        kategorieID = katId;
                    }
                    else if (int.TryParse(kat?.ToString(), out int katId2))
                    {
                        kategorieID = katId2;
                    }
                }
            }

            // Erstelle neue Config OHNE bezeichnung, status und kategorieID
            var configFiltered = new Dictionary<string, object>();
            if (config is System.Text.Json.JsonElement jsonConfigObj)
            {
                var dict2 = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonConfigObj.GetRawText())
                    ?? new Dictionary<string, object>();
                foreach (var kvp in dict2)
                {
                    if (kvp.Key != "bezeichnung" && kvp.Key != "status" && kvp.Key != "kategorieID")
                    {
                        configFiltered[kvp.Key] = kvp.Value;
                    }
                }
            }
            else if (config is Dictionary<string, object> dict3)
            {
                foreach (var kvp in dict3)
                {
                    if (kvp.Key != "bezeichnung" && kvp.Key != "status" && kvp.Key != "kategorieID")
                    {
                        configFiltered[kvp.Key] = kvp.Value;
                    }
                }
            }

            var jsonConfig2 = System.Text.Json.JsonSerializer.Serialize(configFiltered);

            // Suche nach ConnectorType und UserName (nicht nach KategorieID)
            // KategorieID wird sp‰ter von Settings.cshtml beim Speichern gesetzt
            var existing = await _context.PulseConnectors
                .FirstOrDefaultAsync(x => x.ConnectorType == connectorId && x.UserName == userName);

            if (existing == null)
            {
                existing = new PulseConnector
                {
                    UserName = userName,
                    ConnectorType = connectorId,
                    KategorieID = 1, // Standardm‰ﬂig auf 1
                    CreatedAt = DateTime.UtcNow
                };
                _context.PulseConnectors.Add(existing);
            }

            // Setze Bezeichnung und Status
            if (bezeichnung != null)
                existing.Bezeichnung = bezeichnung;
            if (status != null)
                existing.Status = status;

            switch (connectorId)
            {
                case "api":
                case "tf6":
                    existing.ApiConfig = jsonConfig2;
                    break;
                case "sql":
                    existing.SqlConfig = jsonConfig2;
                    break;
                case "manual":
                    existing.ManualConfig = jsonConfig2;
                    break;
            }

            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<Dictionary<string, object>> GetConnectorConfigAsync(string connectorId)
        {
            var userName = GetCurrentUser();
            var connector = await _context.PulseConnectors
                .FirstOrDefaultAsync(x => x.ConnectorType == connectorId && x.UserName == userName);

            if (connector == null)
                return new Dictionary<string, object>();

            var result = new Dictionary<string, object>();

            // F¸ge Bezeichnung und Status hinzu, falls vorhanden (diese gehˆren zur Metadata, nicht zur Config)
            if (!string.IsNullOrEmpty(connector.Bezeichnung))
                result["bezeichnung"] = connector.Bezeichnung;
            if (!string.IsNullOrEmpty(connector.Status))
                result["status"] = connector.Status;
            if (connector.KategorieID.HasValue)
                result["kategorieID"] = connector.KategorieID.Value;

            string? jsonConfig = connectorId switch
            {
                "api" or "tf6" => connector.ApiConfig,
                "sql" => connector.SqlConfig,
                "manual" => connector.ManualConfig,
                _ => null
            };

            if (!string.IsNullOrEmpty(jsonConfig))
            {
                var configData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonConfig);
                if (configData != null)
                {
                    foreach (var kvp in configData)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }
            }

            return result;
        }

        public async Task<(bool success, string message)> TestConnectionAsync(string connectorId)
        {
            try
            {
                var config = await GetConnectorConfigAsync(connectorId);

                if (config.Count == 0)
                    return (false, "Keine Konfiguration gefunden");

                using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };

                if (connectorId == "tf6")
                {
                    // tf6: echte Auth + API Test mit Bearer Token
                    if (!config.TryGetValue("authEndpoint", out var authEp) ||
                        !config.TryGetValue("password", out var pwd))
                        return (false, "Auth Endpoint oder Password fehlt");

                    var userName = GetCurrentUser();
                    var json = System.Text.Json.JsonSerializer.Serialize(new { username = userName, password = pwd });
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    var authResp = await client.PostAsync(authEp.ToString(), content);

                    if (!authResp.IsSuccessStatusCode)
                        return (false, $"Auth fehlgeschlagen: {authResp.StatusCode}");

                    // AccessKey aus Auth-Response extrahieren
                    var authBody = await authResp.Content.ReadAsStringAsync();
                    var authJson = System.Text.Json.JsonDocument.Parse(authBody).RootElement;
                    var accessKey = authJson.TryGetProperty("AccessKey", out var keyProp) ? keyProp.GetString() : null;

                    if (string.IsNullOrEmpty(accessKey))
                        return (false, "AccessKey in Response nicht gefunden");

                    // API mit Bearer Token testen
                    if (config.TryGetValue("apiEndpoint", out var apiEp))
                    {
                        var apiReq = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, apiEp.ToString());
                        apiReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessKey);
                        var apiResp = await client.SendAsync(apiReq);

                        if (apiResp.IsSuccessStatusCode)
                            return (true, $"? Verbindung erfolgreich ∑ Auth: OK ∑ API: OK ∑ AccessKey erhalten");
                        else
                            return (false, $"API Test fehlgeschlagen: {apiResp.StatusCode}");
                    }
                    return (true, $"? Auth erfolgreich ∑ AccessKey: {accessKey.Substring(0, Math.Min(20, accessKey.Length))}...");
                }
                else if (connectorId == "api")
                {
                    if (config.TryGetValue("endpointUrl", out var url))
                    {
                        var response = await client.GetAsync(url.ToString());
                        if (response.IsSuccessStatusCode)
                            return (true, $"? API erreichbar ∑ Status: {response.StatusCode}");
                        else
                            return (false, $"API nicht erreichbar: {response.StatusCode}");
                    }
                    return (false, "Endpoint URL nicht konfiguriert");
                }
                else if (connectorId == "sql")
                {
                    if (!config.TryGetValue("server", out var server))
                        return (false, "Server nicht konfiguriert");

                    var database = config.TryGetValue("database", out var db) ? db.ToString() : "";
                    var userId = config.TryGetValue("userId", out var uid) ? uid.ToString() : "sa";
                    var password = config.TryGetValue("password", out var pwd2) ? pwd2.ToString() : "";

                    var connStr = $"Server={server};Database={database};User Id={userId};Password={password};Encrypt=False;TrustServerCertificate=True;Connection Timeout=5;";
                    using var conn = new SqlConnection(connStr);
                    await conn.OpenAsync();
                    conn.Close();
                    return (true, $"? SQL-Verbindung erfolgreich ∑ Server: {server} ∑ DB: {database}");
                }

                return (true, "Test erfolgreich");
            }
            catch (Exception ex)
            {
                return (false, $"Fehler: {ex.Message}");
            }
        }
    }
}

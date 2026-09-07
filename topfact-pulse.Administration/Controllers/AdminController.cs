using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using topfact.Pulse.Data;
using topfact.Pulse.Models;
using topfact.Pulse.Services;

namespace topfact.Pulse.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly IConnectorService _connectorService;
        private readonly AppDbContext _context;

        public AdminController(IConnectorService connectorService, AppDbContext context)
        {
            _connectorService = connectorService;
            _context = context;
        }

        public async Task<IActionResult> ConnectorManager()
        {
            return View();
        }

        public async Task<IActionResult> DataSources()
        {
            return View();
        }

        public async Task<IActionResult> Rights()
        {
            var accessList = await _context.PulseAccess
                .Include(a => a.Bereich)
                .Include(a => a.Gruppe)
                .ToListAsync();
            ViewBag.AccessList = accessList.Select(a => new
            {
                a.Id,
                a.Username,
                a.BereichID,
                bereichName = a.Bereich?.DisplayName ?? "-",
                a.GruppeID,
                gruppeName = a.Gruppe?.DisplayName ?? "-",
                a.Permission,
                a.IsActive,
                a.UpdatedBy
            }).ToList();
            return View();
        }

        [HttpGet("admin/bereiche")]
        [Produces("application/json")]
        public async Task<IActionResult> GetBereiche()
        {
            var bereiche = await _context.PulseBereiche.Select(b => new { b.BereichID, b.DisplayName }).ToListAsync();
            return Ok(bereiche);
        }

        [HttpGet("admin/gruppen")]
        [Produces("application/json")]
        public async Task<IActionResult> GetGruppen()
        {
            var gruppen = await _context.PulseGruppen.Select(g => new { g.GruppeID, g.DisplayName }).ToListAsync();
            return Ok(gruppen);
        }

        // ── Kategorien für Connector-Zuordnung ─────────────────────────────────
        [HttpGet("admin/categories")]
        [Produces("application/json")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.PulseKategorien
                    .Where(k => k.IsActive)
                    .OrderBy(k => k.Title)
                    .Select(k => new
                    {
                        id    = k.KategorieID,
                        title = k.Title
                    })
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // ── Connectoren für Settings ─────────────────────────────────
        [HttpGet("admin/connectors")]
        [Produces("application/json")]
        public async Task<IActionResult> GetConnectors()
        {
            try
            {
                var connectors = await _context.PulseConnectors
                    .Select(c => new
                    {
                        id = c.Id,
                        bezeichnung = c.Bezeichnung,
                        connectorType = c.ConnectorType,
                        type = c.ConnectorType
                    })
                    .ToListAsync();

                return Ok(connectors);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // ── Kategorie in Connector speichern ─────────────────────────────────
        [HttpPost("admin/connector/{connectorId}/kategorie/{kategorieId}")]
        [Produces("application/json")]
        public async Task<IActionResult> UpdateConnectorKategorie(int connectorId, int kategorieId)
        {
            try
            {
                var connector = await _context.PulseConnectors.FindAsync(connectorId);
                if (connector == null)
                    return NotFound(new { error = "Connector nicht gefunden" });

                connector.KategorieID = kategorieId;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Kategorie im Connector gespeichert" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("admin/connector/{id}")]
        [Produces("application/json")]
        public async Task<IActionResult> GetConnector(string id)
        {
            var config = await _connectorService.GetConnectorConfigAsync(id);
            return Ok(config);
        }

        [HttpPost("admin/connector/{id}")]
        [Produces("application/json")]
        public async Task<IActionResult> SaveConnector(string id, [FromBody] object config)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { error = "Connector ID erforderlich" });

            await _connectorService.SaveConnectorAsync(id, config);
            return Ok(new { success = true });
        }

        [HttpPost("admin/connector/{id}/test")]
        [Produces("application/json")]
        public async Task<IActionResult> TestConnection(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { error = "Connector ID erforderlich" });

            var (success, message) = await _connectorService.TestConnectionAsync(id);
            return Ok(new { success, message });
        }

        // Access Management API
        [HttpPost("admin/access")]
        [Produces("application/json")]
        public async Task<IActionResult> CreateAccess([FromBody] PulseAccess access)
        {
            access.UpdatedBy = User?.Identity?.Name ?? "system";
            _context.PulseAccess.Add(access);
            await _context.SaveChangesAsync();
            return Ok(new { id = access.Id });
        }

        [HttpPut("admin/access/{id}")]
        [Produces("application/json")]
        public async Task<IActionResult> UpdateAccess(int id, [FromBody] PulseAccess access)
        {
            var existing = await _context.PulseAccess.FindAsync(id);
            if (existing == null)
                return NotFound();

            existing.Username = access.Username;
            existing.BereichID = access.BereichID;
            existing.GruppeID = access.GruppeID;
            existing.Permission = access.Permission;
            existing.IsActive = access.IsActive;
            existing.UpdatedBy = User?.Identity?.Name ?? "system";

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpDelete("admin/access/{id}")]
        [Produces("application/json")]
        public async Task<IActionResult> DeleteAccess(int id)
        {
            var access = await _context.PulseAccess.FindAsync(id);
            if (access == null)
                return NotFound();

            _context.PulseAccess.Remove(access);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // DataSource Management API
        [HttpPost("admin/datasource/create")]
        [Produces("application/json")]
        public async Task<IActionResult> CreateDataSource([FromBody] DataSourceRequest request)
        {
            if (string.IsNullOrEmpty(request.Name) || string.IsNullOrEmpty(request.Type))
                return BadRequest(new { error = "Name und Type erforderlich" });

            // Validiere Type
            if (request.Type != "api" && request.Type != "db")
                return BadRequest(new { error = "Type muss 'api' oder 'db' sein" });

            try
            {
                // UserName bleibt der von der Anfrage kommende Wert (oder aktueller Benutzer)
                var userName = User?.Identity?.Name ?? "system";
                // ConnectorType speichert die spezifische Bezeichnung
                var connectorType = request.Type == "api" ? "api-user" : "sql-user";

                var connector = new PulseConnector
                {
                    UserName = userName,
                    ConnectorType = connectorType,
                    Bezeichnung = request.Name,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.PulseConnectors.Add(connector);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, id = connector.Id, message = $"Datenquelle '{request.Name}' erstellt" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Fehler beim Erstellen: {ex.Message}" });
            }
        }

        // Fetch all DataSources
        [HttpGet("admin/datasources")]
        [Produces("application/json")]
        public async Task<IActionResult> GetDataSources()
        {
            try
            {
                var dataSources = await _context.PulseConnectors
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new
                    {
                        x.Id,
                        x.UserName,
                        x.ConnectorType,
                        x.Bezeichnung,
                        x.CreatedAt,
                        x.UpdatedAt,
                        displayName = x.Bezeichnung ?? (x.ConnectorType == "api-user" ? "API" : (x.ConnectorType == "sql-user" ? "Datenbank" : x.ConnectorType))
                    })
                    .ToListAsync();

                return Ok(dataSources);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Delete DataSource
        [HttpDelete("admin/datasource/{id}")]
        [Produces("application/json")]
        public async Task<IActionResult> DeleteDataSource(int id)
        {
            try
            {
                var dataSource = await _context.PulseConnectors.FindAsync(id);
                if (dataSource == null)
                    return NotFound(new { error = "Datenquelle nicht gefunden" });

                _context.PulseConnectors.Remove(dataSource);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Datenquelle gelöscht" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Get DataSource Config
        [HttpGet("admin/datasource/{id}/config")]
        [Produces("application/json")]
        public async Task<IActionResult> GetDataSourceConfig(int id)
        {
            try
            {
                var dataSource = await _context.PulseConnectors.FindAsync(id);
                if (dataSource == null)
                    return NotFound(new { error = "Datenquelle nicht gefunden" });

                var config = new Dictionary<string, object>
                {
                    { "id", dataSource.Id },
                    { "connectorType", dataSource.ConnectorType },
                    { "userName", dataSource.UserName }
                };

                // Füge Bezeichnung, Status und kategorieID hinzu, falls vorhanden
                if (!string.IsNullOrEmpty(dataSource.Bezeichnung))
                    config["bezeichnung"] = dataSource.Bezeichnung;
                if (!string.IsNullOrEmpty(dataSource.Status))
                    config["status"] = dataSource.Status;
                if (dataSource.KategorieID.HasValue)
                    config["kategorieID"] = dataSource.KategorieID.Value;

                if (dataSource.ApiConfig != null)
                {
                    var apiConfigObj = System.Text.Json.JsonSerializer.Deserialize<object>(dataSource.ApiConfig);
                    config["apiConfig"] = apiConfigObj;
                }
                else
                {
                    config["apiConfig"] = new { };
                }

                if (dataSource.SqlConfig != null)
                {
                    var sqlConfigObj = System.Text.Json.JsonSerializer.Deserialize<object>(dataSource.SqlConfig);
                    config["sqlConfig"] = sqlConfigObj;
                }
                else
                {
                    config["sqlConfig"] = new { };
                }

                return Ok(config);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Save DataSource Config
        [HttpPost("admin/datasource/{id}/config")]
        [Produces("application/json")]
        public async Task<IActionResult> SaveDataSourceConfig(int id, [FromBody] object configData)
        {
            try
            {
                var dataSource = await _context.PulseConnectors.FindAsync(id);
                if (dataSource == null)
                    return NotFound(new { error = "Datenquelle nicht gefunden" });

                // Extrahiere Bezeichnung, Status und kategorieID BEVOR die Config serialisiert wird
                string? bezeichnung = null;
                string? status = null;
                int? kategorieID = null;

                if (configData is System.Text.Json.JsonElement jsonElement)
                {
                    if (jsonElement.TryGetProperty("bezeichnung", out var bez))
                    {
                        bezeichnung = bez.GetString();
                    }
                    if (jsonElement.TryGetProperty("status", out var stat))
                    {
                        status = stat.GetString();
                    }
                    if (jsonElement.TryGetProperty("kategorieID", out var kat))
                    {
                        if (kat.TryGetInt32(out int katId))
                        {
                            kategorieID = katId;
                        }
                    }
                }
                else if (configData is Dictionary<string, object> dict)
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
                if (configData is System.Text.Json.JsonElement jsonElem)
                {
                    var dictFromJson = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonElem.GetRawText())
                        ?? new Dictionary<string, object>();
                    foreach (var kvp in dictFromJson)
                    {
                        if (kvp.Key != "bezeichnung" && kvp.Key != "status" && kvp.Key != "kategorieID")
                        {
                            configFiltered[kvp.Key] = kvp.Value;
                        }
                    }
                }
                else if (configData is Dictionary<string, object> dict2)
                {
                    foreach (var kvp in dict2)
                    {
                        if (kvp.Key != "bezeichnung" && kvp.Key != "status" && kvp.Key != "kategorieID")
                        {
                            configFiltered[kvp.Key] = kvp.Value;
                        }
                    }
                }

                var json = System.Text.Json.JsonSerializer.Serialize(configFiltered);

                // Setze Bezeichnung, Status und kategorieID
                if (bezeichnung != null)
                    dataSource.Bezeichnung = bezeichnung;
                if (status != null)
                    dataSource.Status = status;
                if (kategorieID.HasValue)
                    dataSource.KategorieID = kategorieID;

                if (dataSource.ConnectorType == "api-user")
                {
                    dataSource.ApiConfig = json;
                }
                else if (dataSource.ConnectorType == "sql-user")
                {
                    dataSource.SqlConfig = json;
                }

                dataSource.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Konfiguration gespeichert" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Test DataSource Connection
        [HttpPost("admin/datasource/{id}/test")]
        [Produces("application/json")]
        public async Task<IActionResult> TestDataSourceConnection(int id)
        {
            try
            {
                var dataSource = await _context.PulseConnectors.FindAsync(id);
                if (dataSource == null)
                    return NotFound(new { error = "Datenquelle nicht gefunden" });

                if (dataSource.ConnectorType == "api-user")
                {
                    return await TestApiConnection(dataSource);
                }
                else if (dataSource.ConnectorType == "sql-user")
                {
                    return await TestSqlConnection(dataSource);
                }

                return Ok(new { success = false, message = "Unbekannter Verbindungstyp" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = $"Verbindungstest fehlgeschlagen: {ex.Message}" });
            }
        }

        private async Task<IActionResult> TestApiConnection(PulseConnector dataSource)
        {
            try
            {
                if (string.IsNullOrEmpty(dataSource.ApiConfig))
                    return Ok(new { success = false, message = "API-Konfiguration nicht vorhanden" });

                var config = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(dataSource.ApiConfig);
                if (config == null || !config.ContainsKey("endpointUrl"))
                    return Ok(new { success = false, message = "Endpoint URL nicht konfiguriert" });

                var endpointUrl = config["endpointUrl"].ToString();
                if (string.IsNullOrEmpty(endpointUrl))
                    return Ok(new { success = false, message = "Endpoint URL ist leer" });

                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);

                    // Versuche einen HEAD-Request (ohne Body)
                    var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Head, endpointUrl);

                    // Füge Header hinzu, falls vorhanden
                    if (config.ContainsKey("customHeaders") && config["customHeaders"] is System.Text.Json.JsonElement headersJson)
                    {
                        var headers = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(headersJson.GetRawText());
                        if (headers != null)
                        {
                            foreach (var header in headers)
                            {
                                request.Headers.Add(header.Key, header.Value);
                            }
                        }
                    }

                    var response = await client.SendAsync(request);
                    if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return Ok(new { success = true, message = $"✓ API antwortet erfolgreich (HTTP {(int)response.StatusCode})" });
                    }
                    else
                    {
                        return Ok(new { success = false, message = $"✕ API antwortet mit Fehler (HTTP {(int)response.StatusCode})" });
                    }
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                return Ok(new { success = false, message = $"✕ Verbindung fehlgeschlagen: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = $"✕ Fehler beim Testen: {ex.Message}" });
            }
        }

        private async Task<IActionResult> TestSqlConnection(PulseConnector dataSource)
        {
            try
            {
                if (string.IsNullOrEmpty(dataSource.SqlConfig))
                    return Ok(new { success = false, message = "SQL-Konfiguration nicht vorhanden" });

                var config = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(dataSource.SqlConfig);
                if (config == null)
                    return Ok(new { success = false, message = "SQL-Konfiguration ungültig" });

                var server = config.ContainsKey("server") ? config["server"].ToString() : "";
                var database = config.ContainsKey("database") ? config["database"].ToString() : "";
                var userId = config.ContainsKey("userId") ? config["userId"].ToString() : "";
                var password = config.ContainsKey("password") ? config["password"].ToString() : "";

                if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(database))
                    return Ok(new { success = false, message = "Server oder Datenbank nicht konfiguriert" });

                var connectionString = $"Server={server};Database={database};User Id={userId};Password={password};Encrypt=False;TrustServerCertificate=True;Connection Timeout=10;";

                try
                {
                    using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        await connection.CloseAsync();
                    }
                    return Ok(new { success = true, message = $"✓ Datenbankverbindung erfolgreich zu {server}\\{database}" });
                }
                catch (Exception sqlEx)
                {
                    return Ok(new { success = false, message = $"✕ SQL-Fehler: {sqlEx.Message}" });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = $"✕ Verbindungsfehler: {ex.Message}" });
            }
        }
    }

    // Helper class für DataSource Request
    public class DataSourceRequest
    {
        public string Name { get; set; }
        public string Type { get; set; } // "api" oder "db"
        public string DataSourceName { get; set; } // wird nicht mehr benötigt
    }
}

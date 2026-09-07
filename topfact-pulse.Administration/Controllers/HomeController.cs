using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using topfact.Pulse.Data;
using topfact.Pulse.Models;
using topfact.Pulse.Services;

namespace topfact.Pulse.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPulseNavigationService _navigationService;
        private readonly IPulseAccessService _accessService;

        public HomeController(
            AppDbContext context,
            IPulseNavigationService navigationService,
            IPulseAccessService accessService)
        {
            _context = context;
            _navigationService = navigationService;
            _accessService = accessService;
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Settings(int? editBereichId, int? editGruppeId, int? editKategorieId)
        {
            var vm = await BuildNavigationSettingsViewModelAsync(editBereichId, editGruppeId, editKategorieId);
            return View("/Views/Admin/Settings.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBereich([Bind(Prefix = "BereichForm")] BereichFormModel form)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, error = "Validierungsfehler" });

            var bereich = form.BereichID > 0
                ? await _context.PulseBereiche.FirstOrDefaultAsync(x => x.BereichID == form.BereichID)
                : null;

            if (bereich is null)
            {
                bereich = new PulseBereich();
                _context.PulseBereiche.Add(bereich);
            }

            bereich.Code = await GenerateUniqueBereichCodeAsync(form.DisplayName, bereich.BereichID);
            bereich.DisplayName = form.DisplayName.Trim();
            bereich.RequiredRole = string.IsNullOrWhiteSpace(form.RequiredRole) ? null : form.RequiredRole.Trim();
            bereich.SortOrder = form.SortOrder;
            bereich.IsActive = form.IsActive;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Bereich erfolgreich gespeichert" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBereich(int bereichId)
        {
            if (bereichId <= 0)
                return BadRequest(new { success = false, error = "Ungültige Bereich-ID" });

            var bereich = await _context.PulseBereiche.FirstOrDefaultAsync(x => x.BereichID == bereichId);
            if (bereich is null)
                return NotFound(new { success = false, error = "Bereich nicht gefunden" });

            // Alle Gruppen für diesen Bereich erhalten
            var gruppen = await _context.PulseGruppen
                .Where(x => x.BereichID == bereichId)
                .ToListAsync();

            var gruppeIds = gruppen.Select(x => x.GruppeID).ToList();

            // Alle Kategorien für diese Gruppen löschen
            if (gruppeIds.Count > 0)
            {
                var kategorien = await _context.PulseKategorien
                    .Where(x => gruppeIds.Contains(x.GruppeID))
                    .ToListAsync();
                _context.PulseKategorien.RemoveRange(kategorien);
            }

            // Alle abhängigen Pulse_Access-Einträge für diese Gruppen löschen
            if (gruppeIds.Count > 0)
            {
                var dependentAccess = await _context.PulseAccess
                    .Where(x => x.GruppeID.HasValue && gruppeIds.Contains(x.GruppeID.Value))
                    .ToListAsync();
                _context.PulseAccess.RemoveRange(dependentAccess);
            }

            // Alle Gruppen für diesen Bereich löschen
            _context.PulseGruppen.RemoveRange(gruppen);

            // Dann den Bereich löschen
            _context.PulseBereiche.Remove(bereich);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Bereich erfolgreich gelöscht" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGruppe([Bind(Prefix = "GruppeForm")] GruppeFormModel form)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, error = "Validierungsfehler" });

            var bereichExists = await _context.PulseBereiche.AnyAsync(x => x.BereichID == form.BereichID);
            if (!bereichExists)
                return BadRequest(new { success = false, error = "Ungültiger Bereich" });

            var gruppe = form.GruppeID > 0
                ? await _context.PulseGruppen.FirstOrDefaultAsync(x => x.GruppeID == form.GruppeID)
                : null;

            if (gruppe is null)
            {
                gruppe = new PulseGruppe();
                _context.PulseGruppen.Add(gruppe);
            }

            gruppe.BereichID = form.BereichID;
            gruppe.Code = await GenerateUniqueGruppeCodeAsync(form.BereichID, form.DisplayName, gruppe.GruppeID);
            gruppe.DisplayName = form.DisplayName.Trim();
            gruppe.SortOrder = form.SortOrder;
            gruppe.IsCollapsible = form.IsCollapsible;
            gruppe.IsExpandedDefault = form.IsExpandedDefault;
            gruppe.IsActive = form.IsActive;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Gruppe erfolgreich gespeichert" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGruppe(int gruppeId)
        {
            if (gruppeId <= 0)
                return BadRequest(new { success = false, error = "Ungültige Gruppe-ID" });

            var gruppe = await _context.PulseGruppen.FirstOrDefaultAsync(x => x.GruppeID == gruppeId);
            if (gruppe is null)
                return NotFound(new { success = false, error = "Gruppe nicht gefunden" });

            // Alle Kategorien für diese Gruppe löschen
            var kategorien = await _context.PulseKategorien
                .Where(x => x.GruppeID == gruppeId)
                .ToListAsync();
            _context.PulseKategorien.RemoveRange(kategorien);

            // Alle abhängigen Pulse_Access-Einträge löschen
            var dependentAccess = await _context.PulseAccess
                .Where(x => x.GruppeID == gruppeId)
                .ToListAsync();
            _context.PulseAccess.RemoveRange(dependentAccess);

            // Dann die Gruppe löschen
            _context.PulseGruppen.Remove(gruppe);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Gruppe erfolgreich gelöscht" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveKategorie([Bind(Prefix = "KategorieForm")] KategorieFormModel form)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, error = "Validierungsfehler" });

            var selectedGruppe = await _context.PulseGruppen
                .Include(x => x.Bereich)
                .FirstOrDefaultAsync(x => x.GruppeID == form.GruppeID);

            if (selectedGruppe is null)
                return BadRequest(new { success = false, error = "Ungültige Gruppe" });

            var kategorie = form.KategorieID > 0
                ? await _context.PulseKategorien.FirstOrDefaultAsync(x => x.KategorieID == form.KategorieID)
                : null;

            if (kategorie is null)
            {
                kategorie = new PulseKategorie();
                _context.PulseKategorien.Add(kategorie);
                kategorie.IconCss = "ri-file-list-3-line";
            }

            kategorie.GruppeID = form.GruppeID;
            kategorie.Title = form.Title.Trim();
            kategorie.RouteBereich = selectedGruppe.Bereich?.Code;
            kategorie.SortOrder = form.SortOrder;
            kategorie.IconCss = form.IconCss ?? "ri-file-list-3-line";
            kategorie.Sql_query = form.Sql_query;
            kategorie.ConnectorID = form.ConnectorID;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Kategorie erfolgreich gespeichert" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteKategorie(int kategorieId)
        {
            if (kategorieId <= 0)
                return BadRequest(new { success = false, error = "Ungültige Kategorie-ID" });

            var kategorie = await _context.PulseKategorien.FirstOrDefaultAsync(x => x.KategorieID == kategorieId);
            if (kategorie is null)
                return NotFound(new { success = false, error = "Kategorie nicht gefunden" });

            _context.PulseKategorien.Remove(kategorie);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Kategorie erfolgreich gelöscht" });
        }

        public IActionResult Error()
        {
            return View();
        }

        private async Task<NavigationSettingsViewModel> BuildNavigationSettingsViewModelAsync(int? editBereichId, int? editGruppeId, int? editKategorieId)
        {
            var bereiche = await _context.PulseBereiche
                .AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.DisplayName)
                .ToListAsync();

            var gruppen = await _context.PulseGruppen
                .AsNoTracking()
                .Include(x => x.Bereich)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.DisplayName)
                .ToListAsync();

            var kategorien = await _context.PulseKategorien
                .AsNoTracking()
                .Include(x => x.Gruppe)
                .ThenInclude(x => x!.Bereich)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Title)
                .ToListAsync();

            var vm = new NavigationSettingsViewModel
            {
                Bereiche = bereiche,
                Gruppen = gruppen,
                Kategorien = kategorien
            };

            if (editBereichId.HasValue)
            {
                var bereich = bereiche.FirstOrDefault(x => x.BereichID == editBereichId.Value);
                if (bereich is not null)
                    vm.BereichForm = new BereichFormModel
                    {
                        BereichID = bereich.BereichID,
                        DisplayName = bereich.DisplayName,
                        RequiredRole = bereich.RequiredRole,
                        SortOrder = bereich.SortOrder,
                        IsActive = bereich.IsActive
                    };
            }

            if (editGruppeId.HasValue)
            {
                var gruppe = gruppen.FirstOrDefault(x => x.GruppeID == editGruppeId.Value);
                if (gruppe is not null)
                    vm.GruppeForm = new GruppeFormModel
                    {
                        GruppeID = gruppe.GruppeID,
                        BereichID = gruppe.BereichID,
                        DisplayName = gruppe.DisplayName,
                        SortOrder = gruppe.SortOrder,
                        IsCollapsible = gruppe.IsCollapsible,
                        IsExpandedDefault = gruppe.IsExpandedDefault,
                        IsActive = gruppe.IsActive
                    };
            }

            if (editKategorieId.HasValue)
            {
                var kategorie = kategorien.FirstOrDefault(x => x.KategorieID == editKategorieId.Value);
                if (kategorie is not null)
                    vm.KategorieForm = new KategorieFormModel
                    {
                        KategorieID = kategorie.KategorieID,
                        GruppeID = kategorie.GruppeID,
                        Title = kategorie.Title,
                        IconCss = kategorie.IconCss,
                        SortOrder = kategorie.SortOrder,
                        ConnectorID = kategorie.ConnectorID,
                        Sql_query = kategorie.Sql_query
                    };
            }

            return vm;
        }

        private static string GenerateCode(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "neu";

            var cleaned = new string(input.Trim()
                .ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
                .ToArray());

            while (cleaned.Contains("--"))
                cleaned = cleaned.Replace("--", "-");

            cleaned = cleaned.Trim('-');
            return string.IsNullOrWhiteSpace(cleaned) ? "neu" : cleaned;
        }

        private async Task<string> GenerateUniqueBereichCodeAsync(string? displayName, int currentBereichId)
        {
            var baseCode = GenerateCode(displayName);
            var code = baseCode;
            var index = 2;

            while (await _context.PulseBereiche.AnyAsync(x => x.BereichID != currentBereichId && x.Code == code))
                code = $"{baseCode}-{index++}";

            return code;
        }

        private async Task<string> GenerateUniqueGruppeCodeAsync(int bereichId, string? displayName, int currentGruppeId)
        {
            var baseCode = GenerateCode(displayName);
            var code = baseCode;
            var index = 2;

            while (await _context.PulseGruppen.AnyAsync(x => x.GruppeID != currentGruppeId && x.BereichID == bereichId && x.Code == code))
                code = $"{baseCode}-{index++}";

            return code;
        }

        private static bool IsProtectedBereich(PulseBereich bereich) =>
            IsProtectedBereichName(bereich.Code) || IsProtectedBereichName(bereich.DisplayName);

        private static bool IsProtectedBereichName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return string.Equals(value.Trim(), "Technik", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value.Trim(), "Organisation", StringComparison.OrdinalIgnoreCase);
        }

        [HttpPost]
        public async Task<IActionResult> ExecuteKategorieSqlQuery([FromBody] ExecuteSqlQueryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.SqlQuery))
                {
                    return BadRequest(new { error = "SQL-Query ist erforderlich" });
                }

                // Query über raw SQL ausführen mit EF Core
                var resultsList = new List<Dictionary<string, object>>();

                try
                {
                    // Versuche, einen Connector für die Kategorie zu finden
                    System.Data.Common.DbConnection connection = null;
                    bool useExternalConnection = false;

                    // Bestimme ConnectorID: Kategorie-ConnectorID hat Vorrang (Standarddatenquelle für die Kategorie)
                    int? connectorIdToUse = null;

                    // Zuerst versuchen, die Standard-ConnectorID aus der Kategorie zu nutzen
                    if (request.KategorieID.HasValue && request.KategorieID.Value > 0)
                    {
                        var kategorie = await _context.PulseKategorien
                            .FirstOrDefaultAsync(k => k.KategorieID == request.KategorieID.Value);

                        if (kategorie?.ConnectorID.HasValue == true)
                        {
                            connectorIdToUse = kategorie.ConnectorID;
                        }
                    }

                    // Falls vom Frontend explizit ein anderer Connector übermittelt wurde, diesen verwenden
                    if (request.ConnectorID.HasValue && request.ConnectorID.Value > 0)
                    {
                        connectorIdToUse = request.ConnectorID;
                    }

                    // Lade und verwende den Connector, falls vorhanden
                    if (connectorIdToUse.HasValue && connectorIdToUse.Value > 0)
                    {
                        var connector = await _context.PulseConnectors
                            .FirstOrDefaultAsync(c => c.Id == connectorIdToUse.Value);

                        if (connector != null && !string.IsNullOrEmpty(connector.SqlConfig))
                        {
                            var connStr = ParseConnectorConfig(connector);
                            if (!string.IsNullOrEmpty(connStr))
                            {
                                connection = new SqlConnection(connStr);
                                useExternalConnection = true;
                            }
                        }
                    }

                    // Fallback zur Standard-Admin-Datenbankverbindung
                    if (connection == null)
                    {
                        await _context.Database.OpenConnectionAsync();
                        connection = _context.Database.GetDbConnection();
                    }

                    try
                    {
                        if (useExternalConnection)
                        {
                            await connection.OpenAsync();
                        }

                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = request.SqlQuery;
                            command.CommandTimeout = 30;

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                int fieldCount = reader.FieldCount;

                                while (await reader.ReadAsync())
                                {
                                    var row = new Dictionary<string, object>();
                                    for (int i = 0; i < fieldCount; i++)
                                    {
                                        string columnName = reader.GetName(i);
                                        object value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                        row[columnName] = value ?? "";
                                    }
                                    resultsList.Add(row);
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (useExternalConnection)
                        {
                            if (connection != null)
                            {
                                await connection.CloseAsync();
                                connection.Dispose();
                            }
                        }
                        else
                        {
                            _context.Database.CloseConnection();
                        }
                    }
                }
                catch (Exception readerEx)
                {
                    return BadRequest(new { error = $"Fehler beim Ausführen der Abfrage: {readerEx.Message}" });
                }

                // Ergebnisse zurückgeben ohne zu speichern
                return Ok(new 
                { 
                    success = true, 
                    message = $"Query erfolgreich ausgeführt. {resultsList.Count} Zeilen.",
                    rowCount = resultsList.Count,
                    data = resultsList
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Fehler bei der Abfrage: {ex.Message}" });
            }
        }

        private string ParseConnectorConfig(PulseConnector connector)
        {
            if (string.IsNullOrEmpty(connector.SqlConfig))
                return null;

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(connector.SqlConfig);
                var root = doc.RootElement;

                if (root.TryGetProperty("server", out var serverEl) &&
                    root.TryGetProperty("database", out var dbEl))
                {
                    var server = serverEl.GetString();
                    var database = dbEl.GetString();
                    var userId = root.TryGetProperty("userId", out var uidEl) ? uidEl.GetString() : null;
                    var password = root.TryGetProperty("password", out var pwEl) ? pwEl.GetString() : null;

                    var builder = new SqlConnectionStringBuilder
                    {
                        DataSource = server,
                        InitialCatalog = database,
                        Encrypt = true,
                        TrustServerCertificate = true
                    };

                    if (!string.IsNullOrEmpty(userId))
                    {
                        builder.UserID = userId;
                        builder.Password = password;
                    }
                    else
                    {
                        builder.IntegratedSecurity = true;
                    }

                    return builder.ConnectionString;
                }
            }
            catch { }

            return connector.SqlConfig;
        }
    }
}

public class ExecuteSqlQueryRequest
{
    public string? SqlQuery { get; set; }
    public int? KategorieID { get; set; }
    public int? ConnectorID { get; set; }
}

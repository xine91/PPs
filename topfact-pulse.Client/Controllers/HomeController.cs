using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using topfact.Pulse.Data;
using topfact.Pulse.Models;
using topfact.Pulse.Services;

namespace topfact.Pulse.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPulseNavigationService _navigationService;

        public HomeController(
            AppDbContext context,
            IPulseNavigationService navigationService)
        {
            _context = context;
            _navigationService = navigationService;
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Settings(int? editBereichId, int? editGruppeId, int? editKategorieId)
        {
            var vm = await BuildNavigationSettingsViewModelAsync(editBereichId, editGruppeId, editKategorieId);
                return View(vm);
            }

            /// <summary>Zeigt alle verfügbaren Kategorien an</summary>
            [HttpGet]
            public async Task<IActionResult> KategorieIndex()
            {
                var kategorien = await _context.PulseKategorien
                    .AsNoTracking()
                    .Include(k => k.Gruppe).ThenInclude(g => g!.Bereich)
                    .Where(k => k.IsActive)
                    .OrderBy(k => k.Gruppe!.Bereich!.Code).ThenBy(k => k.Gruppe!.DisplayName).ThenBy(k => k.Title)
                    .ToListAsync();
                return View(kategorien);
            }

            /// <summary>Test Seite für Kategorie Routes</summary>
            [HttpGet]
            public IActionResult KategorieTest()
            {
                return View();
            }

            /// <summary>Admin: Test-Daten in DB einfügen</summary>
            [HttpGet]
            public async Task<IActionResult> InsertTestData()
            {
                try
                {
                    // Alte Daten löschen
                    var oldData = await _context.PulseQueryResults2
                        .Where(q => q.KategorieID == 4)
                        .ToListAsync();
                    _context.PulseQueryResults2.RemoveRange(oldData);
                    await _context.SaveChangesAsync();

                    // Neue Test-Daten erstellen
                    var testData = new List<Dictionary<string, object>>
                    {
                        new() { { "KategorieID", 4 }, { "Title", "Test Kategorie 4" }, { "IsActive", true }, { "Wert", 100 } },
                        new() { { "KategorieID", 4 }, { "Title", "Test Kategorie 4" }, { "IsActive", true }, { "Wert", 200 } },
                        new() { { "KategorieID", 4 }, { "Title", "Test Kategorie 4" }, { "IsActive", true }, { "Wert", 300 } }
                    };

                    var jsonData = System.Text.Json.JsonSerializer.Serialize(testData);

                    var queryResult = new PulseQueryResult2
                    {
                        KategorieID = 4,
                        Sql_query = "SELECT * FROM Pulse_Kategorie WHERE KategorieID = 4",
                        Daten = jsonData,
                        created_at = DateTime.UtcNow
                    };

                    _context.PulseQueryResults2.Add(queryResult);
                    await _context.SaveChangesAsync();

                    return Ok(new { success = true, message = "Test-Daten eingefügt", queryId = queryResult.QueryID });
                }
                catch (Exception ex)
                {
                    return BadRequest(new { success = false, error = ex.Message });
                }
            }

            /// <summary>Debug: Zeigt Connector- und KPI-Informationen</summary>
            [HttpGet]
            public async Task<IActionResult> DebugKpi(int kategorieId = 4)
            {
                try
                {
                    var kpis = await _context.PulseKpiDefinitions
                        .Where(k => k.KategorieID == kategorieId)
                        .ToListAsync();

                    var connectors = await _context.PulseConnectors.ToListAsync();

                    var kpiDebugList = kpis.Select(k => new
                    {
                        k.KpiDefinitionID,
                        k.Title,
                        k.QuerySql,
                        k.ConnectorID,
                        k.IsActive,
                        HasConnectorConfig = k.ConnectorID.HasValue && connectors.Any(c => c.Id == k.ConnectorID.Value && !string.IsNullOrWhiteSpace(c.SqlConfig))
                    }).ToList();

                    var connectorDebugList = connectors.Select(c => new
                    {
                        c.Id,
                        c.UserName,
                        c.ConnectorType,
                        HasSqlConfig = !string.IsNullOrWhiteSpace(c.SqlConfig),
                        SqlConfigLength = c.SqlConfig?.Length ?? 0,
                        SqlConfigPreview = c.SqlConfig?.Substring(0, Math.Min(100, c.SqlConfig?.Length ?? 0))
                    }).ToList();

                    return Ok(new
                    {
                        success = true,
                        kategorieId,
                        kpiCount = kpis.Count,
                        connectorCount = connectors.Count,
                        kpis = kpiDebugList,
                        connectors = connectorDebugList
                    });
                }
                catch (Exception ex)
                {
                    return BadRequest(new { success = false, error = ex.Message, stackTrace = ex.StackTrace });
                }
            }

            [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBereich([Bind(Prefix = "BereichForm")] BereichFormModel form)
        {
            if (!ModelState.IsValid)
            {
                var invalidVm = await BuildNavigationSettingsViewModelAsync(form.BereichID > 0 ? form.BereichID : null, null, null);
                invalidVm.BereichForm = form;
                return View("Settings", invalidVm);
            }

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
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBereich([Bind] DeleteBereichModel model)
        {
            if (model?.bereichId <= 0)
                return RedirectToAction(nameof(Settings));

            var bereich = await _context.PulseBereiche.FirstOrDefaultAsync(x => x.BereichID == model.bereichId);
            if (bereich is null)
                return RedirectToAction(nameof(Settings));

            if (IsProtectedBereich(bereich))
                return RedirectToAction(nameof(Settings));

            _context.PulseBereiche.Remove(bereich);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGruppe([Bind(Prefix = "GruppeForm")] GruppeFormModel form)
        {
            if (!ModelState.IsValid)
            {
                var invalidVm = await BuildNavigationSettingsViewModelAsync(null, form.GruppeID > 0 ? form.GruppeID : null, null);
                invalidVm.GruppeForm = form;
                return View("Settings", invalidVm);
            }

            var bereichExists = await _context.PulseBereiche.AnyAsync(x => x.BereichID == form.BereichID);
            if (!bereichExists)
            {
                ModelState.AddModelError(nameof(form.BereichID), "Bitte einen gÃ¼ltigen Bereich auswÃ¤hlen.");
                var invalidVm = await BuildNavigationSettingsViewModelAsync(null, form.GruppeID > 0 ? form.GruppeID : null, null);
                invalidVm.GruppeForm = form;
                return View("Settings", invalidVm);
            }

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
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGruppe([Bind] DeleteGruppeModel model)
        {
            if (model?.gruppeId <= 0)
                return RedirectToAction(nameof(Settings));

            var gruppe = await _context.PulseGruppen.FirstOrDefaultAsync(x => x.GruppeID == model.gruppeId);
            if (gruppe is null)
                return RedirectToAction(nameof(Settings));

            _context.PulseGruppen.Remove(gruppe);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveKategorie([Bind(Prefix = "KategorieForm")] KategorieFormModel form)
        {
            if (!ModelState.IsValid)
            {
                var invalidVm = await BuildNavigationSettingsViewModelAsync(null, null, form.KategorieID > 0 ? form.KategorieID : null);
                invalidVm.KategorieForm = form;
                return View("Settings", invalidVm);
            }

            var selectedGruppe = await _context.PulseGruppen
                .Include(x => x.Bereich)
                .FirstOrDefaultAsync(x => x.GruppeID == form.GruppeID);

            if (selectedGruppe is null)
            {
                ModelState.AddModelError(nameof(form.GruppeID), "Bitte eine gÃ¼ltige Gruppe auswÃ¤hlen.");
                var invalidVm = await BuildNavigationSettingsViewModelAsync(null, null, form.KategorieID > 0 ? form.KategorieID : null);
                invalidVm.KategorieForm = form;
                return View("Settings", invalidVm);
            }

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
            kategorie.Sql_query = form.Sql_query;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteKategorie([Bind] DeleteKategorieModel model)
        {
            if (model?.kategorieId <= 0)
                return RedirectToAction(nameof(Settings));

            var kategorie = await _context.PulseKategorien.FirstOrDefaultAsync(x => x.KategorieID == model.kategorieId);
            if (kategorie is null)
                return RedirectToAction(nameof(Settings));

            _context.PulseKategorien.Remove(kategorie);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Settings));
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
                        SortOrder = kategorie.SortOrder,
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
        [HttpPost]
        public async Task<IActionResult> SaveQueryResult([FromBody] SaveQueryResultRequest request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SaveQueryResult] START - kategorieId: {request?.KategorieId}");

                if (request == null || string.IsNullOrEmpty(request.SqlQuery))
                {
                    System.Diagnostics.Debug.WriteLine("[SaveQueryResult] Request ist null oder SqlQuery ist leer");
                    return Json(new { success = false, message = "Ungültige Parameter" });
                }

                var serializedData = System.Text.Json.JsonSerializer.Serialize(request.Daten);
                System.Diagnostics.Debug.WriteLine($"[SaveQueryResult] Data serialized: {serializedData.Length} chars");

                // Erstelle ein neues PulseQueryResult2 Objekt - EF generiert QueryID automatisch
                var queryResult = new PulseQueryResult2
                {
                    KategorieID = request.KategorieId > 0 ? request.KategorieId : (int?)null,
                    Sql_query = request.SqlQuery,
                    Daten = serializedData,
                    created_at = DateTime.UtcNow
                };

                System.Diagnostics.Debug.WriteLine("[SaveQueryResult] Adding entity to context");
                _context.PulseQueryResults2.Add(queryResult);

                System.Diagnostics.Debug.WriteLine("[SaveQueryResult] Saving changes...");
                await _context.SaveChangesAsync();

                var queryId = queryResult.QueryID;
                System.Diagnostics.Debug.WriteLine($"[SaveQueryResult] SUCCESS - Entity saved with QueryId: {queryId}");

                return Json(new { success = true, message = "Daten erfolgreich gespeichert", id = queryId });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SaveQueryResult] ERROR: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[SaveQueryResult] InnerException: {ex.InnerException?.Message}");
                System.Diagnostics.Debug.WriteLine($"[SaveQueryResult] Stack: {ex.StackTrace}");
                return Json(new { success = false, message = $"Fehler: {ex.Message}" });
            }
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

                if (request.SqlQuery.Contains('[') || request.SqlQuery.Contains(']'))
                {
                    return BadRequest(new { error = "SQL-Query darf keine eckigen Klammern enthalten" });
                }

                var resultsList = new List<Dictionary<string, object>>();

                try
                {
                    await _context.Database.OpenConnectionAsync();
                    try
                    {
                        var connection = _context.Database.GetDbConnection();
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
                        _context.Database.CloseConnection();
                    }
                }
                catch (Exception readerEx)
                {
                    return BadRequest(new { error = $"Fehler beim AusfÃ¼hren der Abfrage: {readerEx.Message}" });
                }

                if (resultsList.Count == 0)
                {
                    return Ok(new { success = true, message = "Query ausgefÃ¼hrt, aber keine Ergebnisse", rowCount = 0, queryId = 0 });
                }

                if (request.KategorieID.HasValue)
                {
                    var existingQueryResult = await _context.PulseQueryResults2
                        .FirstOrDefaultAsync(x => x.KategorieID == request.KategorieID.Value);

                    if (existingQueryResult is null)
                    {
                        var newQueryResult = new PulseQueryResult2
                        {
                            KategorieID = request.KategorieID,
                            Sql_query = request.SqlQuery,
                            Daten = resultsList.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(new { rowCount = resultsList.Count, rows = resultsList.Take(100).ToList() }) : "{}",
                            created_at = DateTime.UtcNow
                        };
                        _context.PulseQueryResults2.Add(newQueryResult);
                    }
                    else
                    {
                        existingQueryResult.Sql_query = request.SqlQuery;
                        existingQueryResult.Daten = resultsList.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(new { rowCount = resultsList.Count, rows = resultsList.Take(100).ToList() }) : "{}";
                    }

                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    success = true,
                    message = $"Query erfolgreich ausgefÃ¼hrt. {resultsList.Count} Zeilen gespeichert.",
                    rowCount = resultsList.Count,
                    queryId = 0
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Fehler bei der Abfrage: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetKategorieData(int kategorieId)
        {
            try
            {
                var kategorieDataService = HttpContext.RequestServices.GetService<IKategorieDataService>();
                if (kategorieDataService == null)
                    return BadRequest(new { error = "Kategorie-Dienst nicht verfuegbar" });

                var kategorieData = await kategorieDataService.GetKategorieDataAsync(kategorieId);
                if (kategorieData == null)
                    return NotFound(new { error = "Kategorie nicht gefunden" });

                return Ok(new
                {
                    statusCode = 200,
                    isSuccess = true,
                    body = kategorieData,
                    headers = new { },
                    requestBody = new { kategorieId }
                });
            }
            catch (Exception ex)
            {
                // Fehler wird serverseitig geloggt; generische Fehlermeldung wird an Client gesendet
                return StatusCode(500, new { error = "Fehler beim Laden der Kategorie-Daten" });
            }
        }
    }

    public class ExecuteSqlQueryRequest
    {
        public string? SqlQuery { get; set; }
        public int? KategorieID { get; set; }
    }

    public class SaveQueryResultRequest
    {
        public int KategorieId { get; set; }
        public string SqlQuery { get; set; }
        public object Daten { get; set; }
    }
}

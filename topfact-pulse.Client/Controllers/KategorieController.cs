using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using topfact.Pulse.Data;
using topfact.Pulse.Models;
using topfact.Pulse.Services;

namespace topfact.Pulse.Web.Controllers
{
    public class KategorieController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IKpiCalculationService _kpiService;

        public KategorieController(AppDbContext context, IKpiCalculationService kpiService)
        {
            _context = context;
            _kpiService = kpiService;
        }

        [HttpGet]
        [Route("kategorie/view/{id?}")]
        public async Task<IActionResult> View(string bereich, int id)
        {
            try
            {
                var kategorie = await _context.PulseKategorien
                    .AsNoTracking()
                    .Include(k => k.Gruppe)
                    .ThenInclude(g => g!.Bereich)
                    .FirstOrDefaultAsync(k => k.KategorieID == id);

                if (kategorie == null)
                {
                    var errorMsg = "Kategorie mit ID " + id + " wurde nicht gefunden.";
                    return BadRequest(new { error = errorMsg, kategorieId = id });
                }

                var queryResult = await _context.PulseQueryResults2
                    .AsNoTracking()
                    .Where(q => q.KategorieID == id)
                    .OrderByDescending(q => q.created_at)
                    .FirstOrDefaultAsync();

                List<Dictionary<string, object>> rows = new();
                List<string> columns = new();
                DateTime? executedAt = null;
                int? totalRowCount = null;

                if (queryResult != null && !string.IsNullOrWhiteSpace(queryResult.Daten))
                {
                    try
                    {
                        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                        // Erkenne SQL vs API Format
                        System.Text.Json.JsonElement parseTest;
                        try
                        {
                            parseTest = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(queryResult.Daten, options);
                        }
                        catch
                        {
                            parseTest = new();
                        }

                        bool isApiFormat = parseTest.ValueKind == System.Text.Json.JsonValueKind.Object &&
                                         parseTest.TryGetProperty("statusCode", out _);
                        bool isSqlFormat = parseTest.ValueKind == System.Text.Json.JsonValueKind.Object &&
                                          parseTest.TryGetProperty("rowCount", out _);

                        if (isApiFormat)
                        {
                            // API-Format: { statusCode, isSuccess, body, contentType, executedAtUtc }
                            System.Diagnostics.Debug.WriteLine("[KategorieController] API-Format erkannt");

                            if (parseTest.TryGetProperty("body", out var bodyToken))
                            {
                                if (bodyToken.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    // Body ist ein Array
                                    rows = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(bodyToken.GetRawText(), options) ?? new();
                                }
                                else if (bodyToken.ValueKind == System.Text.Json.JsonValueKind.Object)
                                {
                                    // Body ist ein Objekt - versuche Subproperties zu extrahieren
                                    bool foundSubArray = false;
                                    foreach (var propName in new[] { "rows", "data", "results", "items" })
                                    {
                                        if (bodyToken.TryGetProperty(propName, out var subArray) && 
                                            subArray.ValueKind == System.Text.Json.JsonValueKind.Array)
                                        {
                                            rows = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(subArray.GetRawText(), options) ?? new();
                                            foundSubArray = true;
                                            break;
                                        }
                                    }

                                    // Wenn keine Subarray gefunden, packe das ganze Objekt in ein Array
                                    if (!foundSubArray)
                                    {
                                        var singleRow = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(bodyToken.GetRawText(), options);
                                        if (singleRow != null)
                                        {
                                            rows = new List<Dictionary<string, object>> { singleRow };
                                        }
                                    }
                                }
                            }

                            if (parseTest.TryGetProperty("executedAtUtc", out var execTime))
                            {
                                if (execTime.TryGetDateTime(out var dt))
                                {
                                    executedAt = dt;
                                }
                            }
                        }
                        else if (isSqlFormat)
                        {
                            // SQL-Format: { rowCount, executedAtUtc, rows }
                            System.Diagnostics.Debug.WriteLine("[KategorieController] SQL-Format erkannt");

                            var wrapper = System.Text.Json.JsonSerializer.Deserialize<JsonDataWrapper>(queryResult.Daten, options);

                            if (wrapper?.Rows != null && wrapper.Rows.Any())
                            {
                                rows = wrapper.Rows;
                                if (wrapper.ExecutedAtUtc.HasValue) executedAt = wrapper.ExecutedAtUtc.Value;
                                if (wrapper.RowCount.HasValue) totalRowCount = wrapper.RowCount.Value;
                            }
                        }
                        else
                        {
                            // Unbekanntes Format - versuche als Array zu deserialisieren
                            System.Diagnostics.Debug.WriteLine("[KategorieController] Unbekanntes Format erkannt");
                            rows = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(queryResult.Daten, options) ?? new();
                        }

                        if (rows.Any())
                        {
                            columns = rows.First().Keys.ToList();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Fehler beim Deserialisieren: " + ex.Message);
                        System.Diagnostics.Debug.WriteLine("Stack: " + ex.StackTrace);
                        rows = new();
                    }
                }

                var kpiValues = await _kpiService.GetKpiValuesAsync(id);

                // Lade Connector-Bezeichnung basierend auf KategorieID und aktuellem Benutzer
                var currentUsername = User.Identity?.Name 
                    ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                    ?? string.Empty;

                if (currentUsername.Contains('\\'))
                    currentUsername = currentUsername.Split('\\').Last();

                var connector = await _context.PulseConnectors
                    .AsNoTracking()
                    .Where(c => c.KategorieID == id && c.UserName == currentUsername)
                    .OrderByDescending(c => c.CreatedAt)  // Nimm den letzten (aktuellsten) Connector
                    .FirstOrDefaultAsync();

                // Fallback: Wenn kein Connector für den User gefunden, nimm any connector für diese Kategorie
                if (connector == null)
                {
                    connector = await _context.PulseConnectors
                        .AsNoTracking()
                        .Where(c => c.KategorieID == id)
                        .OrderByDescending(c => c.CreatedAt)
                        .FirstOrDefaultAsync();
                }

                // DEBUG
                System.Diagnostics.Debug.WriteLine($"[KategorieController] KategorieID={id}, CurrentUser='{currentUsername}'");
                System.Diagnostics.Debug.WriteLine($"[KategorieController] Connector gefunden: {connector != null}");
                if (connector != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[KategorieController] Connector.Bezeichnung: '{connector.Bezeichnung}'");
                    System.Diagnostics.Debug.WriteLine($"[KategorieController] Connector.UserName: '{connector.UserName}'");
                    System.Diagnostics.Debug.WriteLine($"[KategorieController] Connector.ConnectorType: '{connector.ConnectorType}'");
                }

                ViewBag.Kategorie = kategorie;
                ViewBag.SqlQuery = queryResult?.Sql_query ?? kategorie.Sql_query ?? string.Empty;
                ViewBag.Rows = rows;
                ViewBag.Columns = columns;
                ViewBag.KpiValues = kpiValues;
                ViewBag.Bereich = bereich;
                ViewBag.LastUpdated = queryResult?.created_at;
                ViewBag.ExecutedAt = executedAt;
                ViewBag.RowCount = totalRowCount ?? rows.Count;
                ViewBag.ConnectorBezeichnung = connector?.Bezeichnung ?? string.Empty;

                return View();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, details = ex.ToString() });
            }
        }
    }

    public class JsonDataWrapper
    {
        public int? RowCount { get; set; }
        public DateTime? ExecutedAtUtc { get; set; }
        public List<Dictionary<string, object>>? Rows { get; set; }
    }
}

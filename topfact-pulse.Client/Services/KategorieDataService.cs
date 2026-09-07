using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using topfact.Pulse.Data;
using topfact.Pulse.Models;

namespace topfact.Pulse.Services
{
    /// <summary>
    /// Service für Datenbeschaffung basierend auf KategorieID
    /// Kombiniert Daten aus Pulse_QueryResults2 und Pulse_KpiDefinition
    /// </summary>
    public interface IKategorieDataService
    {
        Task<KategorieDataDto> GetKategorieDataAsync(int kategorieId);
    }

    public class KategorieDataService : IKategorieDataService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<KategorieDataService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public KategorieDataService(AppDbContext context, ILogger<KategorieDataService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Erkennt, ob die Daten von SQL oder API stammen
        /// Erkennung basiert auf:
        /// 1. ConnectorType in Pulse_ConnectorManager (wenn verfügbar)
        /// 2. Struktur der Daten (SQL: rowCount, API: statusCode)
        /// 3. Inhalt der Sql_query (HTTP-URL = API, SQL-Statement = SQL)
        /// </summary>
        private string DetectDataSource(string? datenJson, string? sqlQuery, string? connectorType)
        {
            // Höchste Priorität: ConnectorType aus Pulse_ConnectorManager
            if (!string.IsNullOrWhiteSpace(connectorType))
            {
                var lowerType = connectorType.ToLowerInvariant();
                if (lowerType.Contains("api") || lowerType.Contains("http") || lowerType.Contains("rest"))
                    return "API";
                if (lowerType.Contains("sql") || lowerType.Contains("database") || lowerType.Contains("db"))
                    return "SQL";
            }

            // Zweite Priorität: Inhalt der Sql_query
            if (!string.IsNullOrWhiteSpace(sqlQuery))
            {
                var lowerQuery = sqlQuery.Trim().ToLowerInvariant();
                // HTTP-URL oder API-typische Marker
                if (lowerQuery.StartsWith("http://") || lowerQuery.StartsWith("https://") ||
                    lowerQuery.StartsWith("api:") || lowerQuery.StartsWith("api/") ||
                    lowerQuery.Contains("api.request") || lowerQuery.Contains("api.fetch"))
                    return "API";

                // SELECT/INSERT/UPDATE/DELETE = SQL
                if (lowerQuery.StartsWith("select ") || lowerQuery.StartsWith("insert ") ||
                    lowerQuery.StartsWith("update ") || lowerQuery.StartsWith("delete ") ||
                    lowerQuery.StartsWith("with "))
                    return "SQL";
            }

            // Dritte Priorität: Struktur der Daten
            if (string.IsNullOrWhiteSpace(datenJson))
                return "UNKNOWN";

            try
            {
                using var doc = JsonDocument.Parse(datenJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("rowCount", out _))
                    return "SQL";

                if (root.TryGetProperty("statusCode", out _))
                    return "API";
            }
            catch
            {
                // Ignoriere Parse-Fehler
            }

            return "UNKNOWN";
        }

        /// <summary>
        /// Extrahiert Zeilen aus API-Body
        /// Body kann sein: Array, Objekt mit rows/data/results/items, oder einzelnes Objekt
        /// </summary>
        private static List<Dictionary<string, object>>? ExtractApiRows(JsonElement bodyElement)
        {
            try
            {
                // Fall 1: Body ist ein Array
                if (bodyElement.ValueKind == JsonValueKind.Array)
                {
                    var arrayRows = new List<Dictionary<string, object>>();
                    foreach (var item in bodyElement.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object)
                        {
                            arrayRows.Add(JsonElementToDictionary(item));
                        }
                    }
                    return arrayRows;
                }

                // Fall 2: Body ist ein Objekt
                if (bodyElement.ValueKind == JsonValueKind.Object)
                {
                    // Versuche Subproperties zu extrahieren
                    foreach (var propName in new[] { "rows", "data", "results", "items" })
                    {
                        if (bodyElement.TryGetProperty(propName, out var subArray) &&
                            subArray.ValueKind == JsonValueKind.Array)
                        {
                            var subRows = new List<Dictionary<string, object>>();
                            foreach (var item in subArray.EnumerateArray())
                            {
                                if (item.ValueKind == JsonValueKind.Object)
                                {
                                    subRows.Add(JsonElementToDictionary(item));
                                }
                            }
                            return subRows;
                        }
                    }

                    // Fallback: Body ist ein einzelnes Objekt -> in Array packen
                    return new List<Dictionary<string, object>>
                    {
                        JsonElementToDictionary(bodyElement)
                    };
                }
            }
            catch
            {
                // Ignoriere Fehler
            }

            return null;
        }

        /// <summary>
        /// Konvertiert ein JsonElement-Objekt in ein Dictionary
        /// </summary>
        private static Dictionary<string, object> JsonElementToDictionary(JsonElement element)
        {
            var dict = new Dictionary<string, object>();
            foreach (var prop in element.EnumerateObject())
            {
                dict[prop.Name] = JsonElementToObject(prop.Value);
            }
            return dict;
        }

        /// <summary>
        /// Konvertiert ein JsonElement in ein .NET-Objekt
        /// </summary>
        private static object JsonElementToObject(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null!,
                JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
                JsonValueKind.Object => JsonElementToDictionary(element),
                _ => element.ToString()
            };
        }

        /// <summary>
        /// Normalisiert die Daten aus Pulse_QueryResults2
        /// Erkennt SQL/API und gibt einheitliche Zeilen zurück
        /// </summary>
        private (string dataSource, string normalizedDaten) NormalizeQueryData(int queryId, string? datenJson, string? sqlQuery, string? connectorType = null)
        {
            if (string.IsNullOrWhiteSpace(datenJson))
                return ("UNKNOWN", string.Empty);

            var dataSource = DetectDataSource(datenJson, sqlQuery, connectorType);
            _logger.LogInformation($"[KategorieDataService] Query #{queryId} erkannt als: {dataSource} (ConnectorType: {connectorType ?? "n/a"}, SqlQuery: {(string.IsNullOrEmpty(sqlQuery) ? "leer" : "vorhanden")})");

            // Bei API-Daten: SQL-Query ignorieren
            if (dataSource == "API")
            {
                _logger.LogInformation($"[KategorieDataService] Query #{queryId} ist API-Daten - SQL-Query wird ignoriert");
                return (dataSource, datenJson);
            }

            // Bei SQL-Daten: Original beibehalten
            return (dataSource, datenJson);
        }

        /// <summary>
        /// Fetcht alle Query-Ergebnisse und KPIs für eine KategorieID
        /// </summary>
        public async Task<KategorieDataDto> GetKategorieDataAsync(int kategorieId)
        {
            try
            {
                _logger.LogInformation($"[KategorieDataService] Fetche Daten für KategorieID: {kategorieId}");

                var kategorie = await _context.PulseKategorien
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.KategorieID == kategorieId);

                if (kategorie == null)
                {
                    _logger.LogWarning($"[KategorieDataService] Kategorie {kategorieId} nicht gefunden");
                    return new KategorieDataDto { KategorieID = kategorieId };
                }

                // Fetche alle Query-Ergebnisse für diese KategorieID
                var queryResults = await _context.PulseQueryResults2
                    .AsNoTracking()
                    .Where(x => x.KategorieID == kategorieId)
                    .OrderByDescending(x => x.created_at)
                    .ToListAsync();

                _logger.LogInformation($"[KategorieDataService] {queryResults.Count} Query-Ergebnisse gefunden");

                // Fetche alle Connector-Einträge aus Pulse_ConnectorManager für diese KategorieID
                // (Wird genutzt um den ConnectorType pro Query Result zu bestimmen)
                var connectors = await _context.PulseConnectors
                    .AsNoTracking()
                    .Where(c => c.KategorieID == kategorieId)
                    .ToListAsync();

                _logger.LogInformation($"[KategorieDataService] {connectors.Count} Connectoren für Kategorie {kategorieId} gefunden");

                // Logge alle gefundenen Connector-Typen für Debugging
                foreach (var conn in connectors)
                {
                    _logger.LogInformation($"[KategorieDataService] Connector #{conn.Id}: Type='{conn.ConnectorType}', Bezeichnung='{conn.Bezeichnung}', HatApiConfig={!string.IsNullOrWhiteSpace(conn.ApiConfig)}, HatSqlConfig={!string.IsNullOrWhiteSpace(conn.SqlConfig)}");
                }

                // Bestimme einen primären Connector-Type für die Kategorie
                // (bei mehreren wird der erste verwendet)
                string? primaryConnectorType = connectors.FirstOrDefault()?.ConnectorType;

                // Verarbeite Query Results mit Datenquellen-Erkennung
                var processedQueryResults = new List<QueryResultDto>();
                foreach (var x in queryResults)
                {
                    var (dataSource, normalizedDaten) = NormalizeQueryData(x.QueryID, x.Daten, x.Sql_query, primaryConnectorType);

                    // Bei API-Daten: SqlQuery auf erklärenden Text setzen
                    var sqlQueryForDto = dataSource == "API"
                        ? $"[API Connector] Daten via HTTP-Request abgerufen"
                        : x.Sql_query;

                    processedQueryResults.Add(new QueryResultDto
                    {
                        QueryID = x.QueryID,
                        KategorieID = x.KategorieID,
                        SqlQuery = sqlQueryForDto,
                        Daten = normalizedDaten,
                        DataSource = dataSource,
                        ConnectorType = primaryConnectorType,
                        CreatedAt = x.created_at
                    });

                    _logger.LogInformation($"[KategorieDataService] Query #{x.QueryID}: Source={dataSource}, SqlQuery={(dataSource == "API" ? "[IGNORIERT]" : "verwendet")}, ConnectorType={primaryConnectorType ?? "n/a"}");
                }

                // Fetche alle KPI-Definitionen für diese KategorieID
                var kpis = await _context.PulseKpiDefinitions
                    .AsNoTracking()
                    .Where(x => x.KategorieID == kategorieId && (x.IsActive == true || x.IsActive == true))
                    .ToListAsync();

                _logger.LogInformation($"[KategorieDataService] {kpis.Count} KPIs gefunden");

                return new KategorieDataDto
                {
                    KategorieID = kategorieId,
                    KategorieName = kategorie.Title,
                    QueryResults = processedQueryResults,
                    Kpis = kpis.Select(x => new KpiDefinitionDto
                    {
                        KpiDefinitionID = x.KpiDefinitionID,
                        Title = x.Title,
                        Description = x.Description,
                        IconCss = x.IconCss,
                        Color = x.Color,
                        Unit = x.Unit,
                        IsActive = x.IsActive
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"[KategorieDataService] Fehler: {ex.Message}");
                return new KategorieDataDto { KategorieID = kategorieId };
            }
        }
    }

    /// <summary>
    /// DTO für Kategorie-Daten Response
    /// </summary>
    public class KategorieDataDto
    {
        public int KategorieID { get; set; }
        public string? KategorieName { get; set; }
        public List<QueryResultDto> QueryResults { get; set; } = new();
        public List<KpiDefinitionDto> Kpis { get; set; } = new();
    }

    /// <summary>
    /// DTO für Query-Ergebnis
    /// </summary>
    public class QueryResultDto
    {
        public int QueryID { get; set; }
        public int? KategorieID { get; set; }

        /// <summary>
        /// SQL-Query (bei API-Daten: erklärender Platzhalter-Text)
        /// </summary>
        public string SqlQuery { get; set; } = string.Empty;

        /// <summary>
        /// Original-JSON der Daten (SQL: { rowCount, rows }, API: { statusCode, body })
        /// </summary>
        public string? Daten { get; set; }

        /// <summary>
        /// Datenquelle: "SQL" oder "API" oder "UNKNOWN"
        /// </summary>
        public string DataSource { get; set; } = "UNKNOWN";

        /// <summary>
        /// Connector-Typ aus Pulse_ConnectorManager (z.B. "API", "SQL", "HttpClient")
        /// </summary>
        public string? ConnectorType { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO für KPI-Definition
    /// </summary>
    public class KpiDefinitionDto
    {
        public int KpiDefinitionID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconCss { get; set; }
        public string? Color { get; set; }
        public string? Unit { get; set; }
        public bool IsActive { get; set; }
    }
}

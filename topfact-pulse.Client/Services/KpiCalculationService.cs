using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using topfact.Pulse.Data;
using topfact.Pulse.Models;

namespace topfact.Pulse.Services
{
    public interface IKpiCalculationService
    {
        Task<double?> CalculateKpiValueAsync(PulseKpiDefinition kpi);
        Task<List<KpiValueDto>> GetKpiValuesAsync(int kategorieId);
    }

    public class KpiCalculationService : IKpiCalculationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<KpiCalculationService> _logger;

        public KpiCalculationService(AppDbContext context, ILogger<KpiCalculationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Berechnet den KPI-Wert ausschliesslich ueber den ConnectorManager.
        /// Ohne gueltige ConnectorID/SqlConfig wird null zurueckgegeben.
        /// </summary>
        public async Task<double?> CalculateKpiValueAsync(PulseKpiDefinition kpi)
        {
            if (string.IsNullOrWhiteSpace(kpi.QuerySql))
            {
                _logger.LogWarning("KPI '{Title}' hat keine QuerySql definiert.", kpi.Title);
                return null;
            }

            if (!kpi.ConnectorID.HasValue)
            {
                _logger.LogWarning("KPI '{Title}' hat keine ConnectorID - Wert kann nicht berechnet werden.", kpi.Title);
                return null;
            }

            // Connector-Eintrag aus Pulse_ConnectorManager laden
            var connector = await _context.PulseConnectors
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == kpi.ConnectorID.Value);

            if (connector == null)
            {
                _logger.LogWarning("KPI '{Title}' verweist auf Connector {Id}, der nicht existiert.",
                    kpi.Title, kpi.ConnectorID);
                return null;
            }

            if (string.IsNullOrWhiteSpace(connector.SqlConfig))
            {
                _logger.LogWarning("Connector '{Name}' (Id={Id}) hat keine SqlConfig.",
                    connector.UserName, connector.Id);
                return null;
            }

            // SqlConfig aus dem ConnectorManager parsen (unterstuetzt 'sql' und 'sql_user')
            var parsedConfig = ParseSqlConfig(connector.SqlConfig);
            if (parsedConfig == null || string.IsNullOrWhiteSpace(parsedConfig.Server))
            {
                _logger.LogWarning("Connector '{Name}' (Id={Id}) hat ungueltige SqlConfig: {Config}",
                    connector.UserName, connector.Id, connector.SqlConfig);
                return null;
            }

            var connectionString = parsedConfig.GetConnectionString();
            var externalDbInfo = parsedConfig.Server + "/" + parsedConfig.Database;
            var connectorDisplayName = !string.IsNullOrWhiteSpace(connector.Bezeichnung)
                ? connector.Bezeichnung
                : connector.UserName;

            try
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = kpi.QuerySql;
                        command.CommandTimeout = 30;
                        var result = await command.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            _logger.LogInformation("KPI '{Title}' erfolgreich von '{Db}' via Connector '{Conn}' abgerufen: {Value}",
                                kpi.Title, externalDbInfo, connectorDisplayName, result);
                            return Convert.ToDouble(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Fehler bei KPI '{Title}' via Connector '{Conn}' auf '{Db}': {Msg}",
                    kpi.Title, connectorDisplayName, externalDbInfo, ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Parst die SqlConfig-JSON. Unterstuetzt beide Formate:
        /// - sql: PascalCase (Server, Database, UserId, ...)
        /// - sql_user: camelCase (server, database, userId, ...) mit zusaetzlichem "type"-Feld
        ///
        /// Robust gegenueber inkonsistenten Typen im JSON: connectionTimeout kann als
        /// Zahl ODER als String ("30") auftreten, encrypt/trustServerCertificate koennen
        /// als bool ODER als String ("True"/"False") kommen. In diesen Faellen schlaegt
        /// JsonSerializer.Deserialize fehl und der manuelle JsonDocument-Fallback uebernimmt.
        /// </summary>
        private SqlConnectorConfig? ParseSqlConfig(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            // 1) Primaerer Versuch: Deserialisierung via JsonSerializer
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var config = JsonSerializer.Deserialize<SqlConnectorConfig>(json, options);

                if (config != null && !string.IsNullOrWhiteSpace(config.Server))
                {
                    return config;
                }
            }
            catch (JsonException)
            {
                // Typ-Mismatch (z.B. connectionTimeout als String, encrypt als String)
                // wird vom Fallback unten behandelt.
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Fehler beim Parsen der SqlConfig-JSON via JsonSerializer, fallback auf JsonDocument.");
            }

            // 2) Fallback:
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var result = new SqlConnectorConfig();

            foreach (var prop in root.EnumerateObject())
            {
                var name = prop.Name.ToLowerInvariant();

                switch (name)
                {
                    case "server":
                        result.Server = ReadString(prop.Value);
                        break;
                    case "database":
                        result.Database = ReadString(prop.Value);
                        break;
                    case "userid":
                    case "user":
                    case "username":
                        result.UserId = ReadString(prop.Value);
                        break;
                    case "password":
                    case "pwd":
                        result.Password = ReadString(prop.Value);
                        break;
                    case "encrypt":
                        result.Encrypt = ReadBoolAsString(prop.Value);
                        break;
                    case "trustservercertificate":
                        result.TrustServerCertificate = ReadBoolAsString(prop.Value);
                        break;
                    case "connectiontimeout":
                    case "timeout":
                        if (TryReadInt(prop.Value, out var timeout))
                            result.ConnectionTimeout = timeout;
                        break;
                }
            }

            return result;
        }

        // --- Helper fuer robustes Parsen der SqlConfig-JSON ----------------------
        // Die SqlConfig kann je nach Connector-Typ unterschiedliche Typen fuer
        // dieselben Felder liefern (z.B. connectionTimeout als int oder als String,
        // encrypt als bool oder als String "True"/"False"). Diese Helfer normalisieren
        // die Werte, bevor sie in SqlConnectorConfig geschrieben werden.

        private static string ReadString(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
                return element.GetString() ?? string.Empty;
            if (element.ValueKind == JsonValueKind.Number)
                return element.ToString();
            if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
                return element.GetBoolean() ? "true" : "false";
            if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
                return string.Empty;
            return element.ToString();
        }

        private static string ReadBoolAsString(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.True)
                return "True";
            if (element.ValueKind == JsonValueKind.False)
                return "False";
            if (element.ValueKind == JsonValueKind.String)
            {
                var s = element.GetString();
                if (bool.TryParse(s, out var b))
                    return b ? "True" : "False";
                return s ?? string.Empty;
            }
            if (element.ValueKind == JsonValueKind.Number)
                return element.GetInt32() != 0 ? "True" : "False";
            return string.Empty;
        }

        private static bool TryReadInt(JsonElement element, out int value)
        {
            value = 0;
            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var n))
            {
                value = n;
                return true;
            }
            if (element.ValueKind == JsonValueKind.String)
            {
                var s = element.GetString();
                return int.TryParse(s, out value);
            }
            return false;
        }

        public async Task<List<KpiValueDto>> GetKpiValuesAsync(int kategorieId)
        {
            var kpis = await _context.PulseKpiDefinitions
                .AsNoTracking()
                .Where(k => k.KategorieID == kategorieId && k.IsActive)
                .OrderBy(k => k.SortOrder)
                .ToListAsync();

            var result = new List<KpiValueDto>();
            foreach (var kpi in kpis)
            {
                var value = await CalculateKpiValueAsync(kpi);
                result.Add(new KpiValueDto
                {
                    KpiDefinitionID = kpi.KpiDefinitionID,
                    Title = kpi.Title,
                    Description = kpi.Description,
                    IconCss = kpi.IconCss,
                    Color = kpi.Color,
                    Value = value,
                    Unit = kpi.Unit,
                    DisplayStyle = kpi.DisplayStyle,
                    SortOrder = kpi.SortOrder,
                    TargetValue = kpi.TargetValue,
                    ThresholdGreen = kpi.ThresholdGreen,
                    ThresholdYellow = kpi.ThresholdYellow,
                    ConnectorID = kpi.ConnectorID
                });
            }
            return result;
        }
    }

    public class KpiValueDto
    {
        public int KpiDefinitionID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? IconCss { get; set; }
        public string? Color { get; set; }
        public double? Value { get; set; }
        public string? Unit { get; set; }
        public string? DisplayStyle { get; set; }
        public int SortOrder { get; set; }
        public double? TargetValue { get; set; }
        public double? ThresholdGreen { get; set; }
        public double? ThresholdYellow { get; set; }
        public int? ConnectorID { get; set; }
        public string? StatusMessage { get; set; }

        public string GetStatusColor()
        {
            if (!Value.HasValue) return Color ?? "#64748b";
            if (ThresholdGreen.HasValue && Value >= ThresholdGreen) return "#10b981";
            if (ThresholdYellow.HasValue && Value >= ThresholdYellow) return "#f59e0b";
            return "#ef4444";
        }
    }
}

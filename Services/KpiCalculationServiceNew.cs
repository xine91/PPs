using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using topfact.Pulse.Data;
using topfact.Pulse.Models;

namespace topfact.Pulse.Services
{
    public interface IKpiCalculationServiceNew
    {
        Task<double?> CalculateKpiValueAsync(PulseKpiDefinition kpi);
        Task<List<KpiValueDto>> GetKpiValuesAsync(int kategorieId);
    }

    public class KpiCalculationServiceNew : IKpiCalculationServiceNew
    {
        private readonly AppDbContext _context;
        private readonly ILogger<KpiCalculationServiceNew> _logger;

        public KpiCalculationServiceNew(AppDbContext context, ILogger<KpiCalculationServiceNew> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<double?> CalculateKpiValueAsync(PulseKpiDefinition kpi)
        {
            if (string.IsNullOrWhiteSpace(kpi.QuerySql))
                return null;

            string connectionString = _context.Database.GetConnectionString() ?? string.Empty;
            string? connectorName = null;

            if (kpi.ConnectorID.HasValue)
            {
                var connector = await _context.PulseConnectors
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == kpi.ConnectorID.Value);

                if (connector != null && !string.IsNullOrWhiteSpace(connector.SqlConfig))
                {
                    try
                    {
                        var sqlConfig = JsonSerializer.Deserialize<SqlConnectorConfig>(connector.SqlConfig);
                        if (sqlConfig != null && !string.IsNullOrWhiteSpace(sqlConfig.Server))
                        {
                            connectionString = sqlConfig.GetConnectionString();
                            connectorName = connector.Bezeichnung ?? connector.UserName;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fehler beim Parsen der SqlConfig von Connector {Id}", kpi.ConnectorID);
                    }
                }
            }

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
                            return Convert.ToDouble(result);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler bei KPI {Title} auf {Conn}", kpi.Title, connectorName ?? "lokale DB");
            }

            return null;
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

        public string GetStatusColor()
        {
            if (!Value.HasValue) return Color ?? "#64748b";
            if (ThresholdGreen.HasValue && Value >= ThresholdGreen) return "#10b981";
            if (ThresholdYellow.HasValue && Value >= ThresholdYellow) return "#f59e0b";
            return "#ef4444";
        }
    }
}

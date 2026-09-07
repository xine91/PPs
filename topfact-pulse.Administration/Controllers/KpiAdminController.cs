using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using topfact.Pulse.Data;
using topfact.Pulse.Models;

namespace topfact.Pulse.Controllers
{
    public class KpiAdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public KpiAdminController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpGet("Admin/KpiAdmin")]
        [HttpGet("KpiAdmin/KpiAdmin")]
        public IActionResult KpiAdmin() => View("~/Views/Admin/KpiAdmin.cshtml");

        [HttpGet("Admin/KpiAdmin/List")]
        public IActionResult List()
        {
            var kpis = _db.PulseKpiDefinitions
                .Where(k => k.IsActive)
                .Select(k => new KpiResponseDto
                {
                    Id = k.KpiDefinitionID,
                    Title = k.Title,
                    Description = k.Description,
                    IconCss = k.IconCss,
                    Color = k.Color,
                    Bereich = k.Bereich,
                    QuerySql = k.QuerySql,
                    TargetValue = k.TargetValue,
                    ToleranceAbsolute = k.ToleranceAbsolute,
                    TolerancePercent = k.TolerancePercent,
                    ThresholdGreen = k.ThresholdGreen,
                    ThresholdYellow = k.ThresholdYellow,
                    Unit = k.Unit,
                    DisplayStyle = k.DisplayStyle,
                    SortOrder = k.SortOrder,
                    KategorieID = k.KategorieID,
                    ConnectorID = k.ConnectorID,
                    IsActive = k.IsActive
                })
                .OrderBy(k => k.SortOrder)
                .ToList();

            return Json(kpis);
        }

        [HttpGet("Admin/KpiAdmin/Get/{id:int}")]
        public IActionResult Get(int id)
        {
            var kpi = _db.PulseKpiDefinitions.FirstOrDefault(k => k.KpiDefinitionID == id);
            if (kpi == null)
                return NotFound();

            var dto = new KpiResponseDto
            {
                Id = kpi.KpiDefinitionID,
                Title = kpi.Title,
                Description = kpi.Description,
                IconCss = kpi.IconCss,
                Color = kpi.Color,
                Bereich = kpi.Bereich,
                QuerySql = kpi.QuerySql,
                TargetValue = kpi.TargetValue,
                ToleranceAbsolute = kpi.ToleranceAbsolute,
                TolerancePercent = kpi.TolerancePercent,
                ThresholdGreen = kpi.ThresholdGreen,
                ThresholdYellow = kpi.ThresholdYellow,
                Unit = kpi.Unit,
                DisplayStyle = kpi.DisplayStyle,
                SortOrder = kpi.SortOrder,
                KategorieID = kpi.KategorieID,
                ConnectorID = kpi.ConnectorID,
                IsActive = kpi.IsActive
            };

            return Json(dto);
        }

        [HttpPost("Admin/KpiAdmin/Save")]
        public IActionResult Save([FromBody] KpiSaveDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Title))
                return Json(new { success = false, error = "Titel ist erforderlich." });

            try
            {
                PulseKpiDefinition kpi;

                if (dto.Id == 0)
                {
                    kpi = new PulseKpiDefinition
                    {
                        Title = dto.Title,
                        Description = dto.Description,
                        IconCss = dto.IconCss,
                        Color = dto.Color,
                        Bereich = dto.Bereich,
                        QuerySql = dto.QuerySql,
                        TargetValue = dto.TargetValue,
                        ToleranceAbsolute = dto.ToleranceAbsolute,
                        TolerancePercent = dto.TolerancePercent,
                        ThresholdGreen = dto.ThresholdGreen,
                        ThresholdYellow = dto.ThresholdYellow,
                        Unit = dto.Unit,
                        DisplayStyle = dto.DisplayStyle,
                        SortOrder = dto.SortOrder,
                        KategorieID = dto.KategorieID,
                        ConnectorID = dto.ConnectorID,
                        IsActive = dto.IsActive,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = User?.Identity?.Name ?? "system"
                    };
                    _db.PulseKpiDefinitions.Add(kpi);
                }
                else
                {
                    kpi = _db.PulseKpiDefinitions.FirstOrDefault(k => k.KpiDefinitionID == dto.Id);
                    if (kpi == null)
                        return Json(new { success = false, error = "KPI nicht gefunden." });

                    kpi.Title = dto.Title;
                    kpi.Description = dto.Description;
                    kpi.IconCss = dto.IconCss;
                    kpi.Color = dto.Color;
                    kpi.Bereich = dto.Bereich;
                    kpi.QuerySql = dto.QuerySql;
                    kpi.TargetValue = dto.TargetValue;
                    kpi.ToleranceAbsolute = dto.ToleranceAbsolute;
                    kpi.TolerancePercent = dto.TolerancePercent;
                    kpi.ThresholdGreen = dto.ThresholdGreen;
                    kpi.ThresholdYellow = dto.ThresholdYellow;
                    kpi.Unit = dto.Unit;
                    kpi.DisplayStyle = dto.DisplayStyle;
                    kpi.SortOrder = dto.SortOrder;
                    kpi.KategorieID = dto.KategorieID;
                    kpi.ConnectorID = dto.ConnectorID;
                    kpi.IsActive = dto.IsActive;
                    kpi.UpdatedAt = DateTime.UtcNow;
                    kpi.UpdatedBy = User?.Identity?.Name ?? "system";

                    _db.PulseKpiDefinitions.Update(kpi);
                }

                _db.SaveChanges();

                return Json(new { success = true, data = new { id = kpi.KpiDefinitionID } });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpDelete("Admin/KpiAdmin/Delete/{id:int}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var kpi = _db.PulseKpiDefinitions.FirstOrDefault(k => k.KpiDefinitionID == id);
                if (kpi == null)
                    return Json(new { success = false, error = "KPI nicht gefunden." });

                _db.PulseKpiDefinitions.Remove(kpi);
                _db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("Admin/KpiAdmin/RunQuery")]
        public async Task<IActionResult> RunQuery([FromBody] RunQueryRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Sql))
                return Json(new { error = "Kein SQL angegeben." });

            var trimmed = req.Sql.TrimStart();
            if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
             && !trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
                return Json(new { error = "Nur SELECT- oder WITH-Abfragen sind erlaubt." });

            try
            {
                using var conn = GetConnection(req.ConnectorId);
                await conn.OpenAsync();

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = req.Sql;
                cmd.CommandTimeout = 15;

                var scalar = await cmd.ExecuteScalarAsync();

                double? value = scalar == null || scalar == DBNull.Value
                    ? null
                    : Convert.ToDouble(scalar);

                return Json(new { value });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet("Admin/KpiAdmin/DataSources")]
        public IActionResult DataSources()
        {
            try
            {
                var connectors = _db.PulseConnectors
                    .Select(c => new DataSourceDto
                    {
                        Id = c.Id,
                        Name = c.Bezeichnung,
                        Type = c.ConnectorType,
                        KategorieID = c.KategorieID
                    })
                    .ToList();

                return Json(connectors);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        private System.Data.Common.DbConnection GetConnection(int? connectorId)
        {
            if (connectorId.HasValue && connectorId > 0)
            {
                var connector = _db.PulseConnectors.FirstOrDefault(c => c.Id == connectorId);
                if (connector != null && !string.IsNullOrEmpty(connector.SqlConfig))
                {
                    string connStr = ParseConnectorConfig(connector);
                    if (!string.IsNullOrEmpty(connStr))
                        return new SqlConnection(connStr);
                }
            }

            return _db.Database.GetDbConnection();
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

        public sealed class RunQueryRequest
        {
            public string Sql { get; set; } = string.Empty;
            public int? ConnectorId { get; set; }
        }
    }
}

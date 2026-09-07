using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using topfact.Pulse.Data;

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

        // ── Page ─────────────────────────────────────────────────────────────
        [HttpGet("/pulse/Admin/KpiAdmin")]
        [HttpGet("/pulse/KpiAdmin/KpiAdmin")]
        public IActionResult KpiAdmin() => View("~/Views/Admin/KpiAdmin.cshtml");

        // ── API: list all KPI definitions ────────────────────────────────────
        [HttpGet("/pulse/Admin/KpiAdmin/List")]
        public IActionResult List() => Json(Array.Empty<object>());

        // ── API: single KPI ──────────────────────────────────────────────────
        [HttpGet("/pulse/Admin/KpiAdmin/Get/{id:int}")]
        public IActionResult Get(int id) => NotFound();

        // ── API: save (create or update) ─────────────────────────────────────
        [HttpPost("/pulse/Admin/KpiAdmin/Save")]
        public IActionResult Save() => StatusCode(501);

        // ── API: delete ───────────────────────────────────────────────────────
        [HttpDelete("/pulse/Admin/KpiAdmin/Delete/{id:int}")]
        public IActionResult Delete(int id) => NotFound();

        // ── API: execute query and return live value ──────────────────────────
        [HttpPost("/pulse/Admin/KpiAdmin/RunQuery")]
        public async Task<IActionResult> RunQuery([FromBody] RunQueryRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Sql))
                return Json(new { error = "Kein SQL angegeben." });

            // Safety: only SELECT / WITH allowed
            var trimmed = req.Sql.TrimStart();
            if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
             && !trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
                return Json(new { error = "Nur SELECT- oder WITH-Abfragen sind erlaubt." });

            try
            {
                var conn = _db.Database.GetDbConnection();
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = req.Sql;
                cmd.CommandTimeout = 15;
                var scalar = await cmd.ExecuteScalarAsync();
                await conn.CloseAsync();

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

        // ── API: list available tables/views from DB schema ───────────────────
        [HttpGet("/pulse/Admin/KpiAdmin/DataSources")]
        public async Task<IActionResult> DataSources()
        {
            try
            {
                const string sql = """
                    SELECT TABLE_SCHEMA + '.' + TABLE_NAME AS FullName, TABLE_TYPE
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_TYPE IN ('BASE TABLE','VIEW')
                    ORDER BY TABLE_TYPE, TABLE_NAME
                    """;

                var conn = _db.Database.GetDbConnection();
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                var reader = await cmd.ExecuteReaderAsync();
                var list = new List<object>();
                while (await reader.ReadAsync())
                    list.Add(new { name = reader.GetString(0), type = reader.GetString(1) });
                await reader.CloseAsync();
                await conn.CloseAsync();
                return Json(list);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        public sealed class RunQueryRequest
        {
            public string Sql { get; set; } = string.Empty;
        }
    }
}

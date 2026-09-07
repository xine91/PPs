using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using topfact.Pulse.Data;
using topfact.Pulse.Models;

namespace topfact.Pulse.Controllers
{
    public partial class UserLoginsController : Controller
    {
        private readonly AnalyticsDbContext _db;
        private readonly IConfiguration _configuration;

        public UserLoginsController(AnalyticsDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Index(
            string? selectedCustomer,
            string? selectedAppName,
            int selectedDays = 30,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var oneYearAgo = DateTime.Now.AddDays(-365);

            // Liste der auszuschließenden Apps (Zentral für alle Abfragen)
            var excludedApps = new[] {
                "topfact6 Jobserver",
                "topfact6 Administration",
                "topfactCloud TransferService",
                "topfactCloud TransferService UI"
            };

            // ── 1. Die "Saubere" Basis-Query ──────────────────────────────────────────
            // Diese Query dient als Fundament. Alles was hier gefiltert wird, 
            // gilt auch für Customers und AppNames Dropdowns.
            var baseFilterQuery = _db.ClientStartupInformations
                .AsNoTracking()
                .Where(x => x.DateCreated != null && x.DateCreated > oneYearAgo)
                .Where(x => x.AppName != null && !excludedApps.Contains(x.AppName));

            // ── 2. Dropdown-Werte (Erben die Filter von baseFilterQuery) ──────────────
            var availableCustomers = await baseFilterQuery
                .Where(x => x.Customer != null)
                .Select(x => x.Customer!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var availableAppNames = await baseFilterQuery
                .Select(x => x.AppName!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            // ── 3. Tabellen-Daten (Nutzt ebenfalls die Basis-Filter) ──────────────────
            DateTime cutoffStart;
            DateTime? cutoffEnd = null;

            if (selectedDays == 0 && dateFrom.HasValue)
            {
                cutoffStart = dateFrom.Value.Date;
                cutoffEnd = dateTo.HasValue ? dateTo.Value.Date.AddDays(1) : DateTime.Now.AddDays(1);
            }
            else
            {
                cutoffStart = DateTime.Now.AddDays(-selectedDays);
            }

            var tableQuery = baseFilterQuery
                .Where(x => x.DateCreated >= cutoffStart);

            if (cutoffEnd.HasValue)
                tableQuery = tableQuery.Where(x => x.DateCreated < cutoffEnd);

            if (!string.IsNullOrEmpty(selectedCustomer))
            {
                tableQuery = tableQuery.Where(x => x.Customer == selectedCustomer);
            }

            if (!string.IsNullOrEmpty(selectedAppName))
            {
                tableQuery = tableQuery.Where(x => x.AppName == selectedAppName);
            }

            // ── KPI-Werte direkt aus DB zählen (ohne Take-Limit) ─────────────
            var today = DateTime.Today;
            var totalLogins  = await tableQuery.CountAsync();
            var loginsToday  = await tableQuery.CountAsync(x => x.DateCreated != null && x.DateCreated.Value.Date == today);
            var uniqueUsers  = await tableQuery.Select(x => x.Username).Distinct().CountAsync();

            var logins = await tableQuery
                .OrderByDescending(x => x.DateCreated)
                .Take(1500)
                .Select(x => new topfact.Pulse.Models.UserLoginEntry
                {
                    Customer = x.Customer ?? "Unbekannter Tenant",
                    Clientname = x.Clientname ?? "Unbekannt",
                    Username = x.Username ?? "N/A",
                    Domainname = x.Domainname ?? "",
                    AppName = x.AppName ?? "Unbekannte App",
                    AppVersion = x.AppVersion ?? "",
                    DateCreated = x.DateCreated ?? DateTime.MinValue
                })
                .ToListAsync();

            var vm = new UserLoginViewModel
            {
                AvailableCustomers = availableCustomers,
                AvailableAppNames = availableAppNames,
                SelectedCustomer = selectedCustomer,
                SelectedAppName = selectedAppName,
                SelectedDays = selectedDays,
                DateFrom = selectedDays == 0 ? dateFrom : null,
                DateTo = selectedDays == 0 ? dateTo : null,
                Logins = logins,
                TotalLogins = totalLogins,
                LoginsToday = loginsToday,
                UniqueUsers = uniqueUsers
            };

            return View(vm);
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> GeplanteFunktionen(
            string? selectedBearbeiter,
            string? selectedFirma,
            int selectedDays = 30,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var vm = new GeplanteFunktionenViewModel
            {
                SelectedBearbeiter = selectedBearbeiter,
                SelectedFirma = selectedFirma,
                SelectedDays = selectedDays,
                DateFrom = selectedDays == 0 ? dateFrom : null,
                DateTo = selectedDays == 0 ? dateTo : null
            };

            var connectionString = _configuration.GetConnectionString("ProjectControllingConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                return View(vm);

            DateTime? cutoffStart = null;
            DateTime? cutoffEnd = null;

            if (selectedDays == 0 && dateFrom.HasValue)
            {
                cutoffStart = dateFrom.Value.Date;
                cutoffEnd = dateTo.HasValue ? dateTo.Value.Date.AddDays(1) : DateTime.Now.AddDays(1);
            }
            else if (selectedDays > 0)
            {
                cutoffStart = DateTime.Now.AddDays(-selectedDays);
                cutoffEnd = DateTime.Now.AddDays(1);
            }

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            const string bearbeiterSql = @"SELECT DISTINCT [Bearbeiter]
FROM [topteam].[dbo].[view_geplante_Funktionen]
WHERE [Bearbeiter] IS NOT NULL
ORDER BY [Bearbeiter];";

            const string firmenSql = @"SELECT DISTINCT [Name1]
FROM [topteam].[dbo].[view_geplante_Funktionen]
WHERE [Name1] IS NOT NULL
ORDER BY [Name1];";

            await using (var bearbeiterCmd = new SqlCommand(bearbeiterSql, connection))
            await using (var bearbeiterReader = await bearbeiterCmd.ExecuteReaderAsync())
            {
                while (await bearbeiterReader.ReadAsync())
                    vm.AvailableBearbeiter.Add(bearbeiterReader.GetString(0));
            }

            await using (var firmenCmd = new SqlCommand(firmenSql, connection))
            await using (var firmenReader = await firmenCmd.ExecuteReaderAsync())
            {
                while (await firmenReader.ReadAsync())
                    vm.AvailableFirmen.Add(firmenReader.GetString(0));
            }

            var sql = @"SELECT TOP (1000) [Bearbeiter]
      ,[ID]
      ,[Name1]
      ,[Titel]
      ,[Benennung]
      ,[Stunden]
      ,[Datum]
      ,[Status]
      ,[fk_tblAnsprechpartner]
      ,[StartDatum]
  FROM [topteam].[dbo].[view_geplante_Funktionen]
  WHERE (@from IS NULL OR [Datum] >= @from)
    AND (@to IS NULL OR [Datum] < @to)
    AND (@bearbeiter IS NULL OR [Bearbeiter] = @bearbeiter)
    AND (@firma IS NULL OR [Name1] = @firma)
  ORDER BY [Datum] DESC;";

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@from", (object?)cutoffStart ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@to", (object?)cutoffEnd ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@bearbeiter", (object?)selectedBearbeiter ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@firma", (object?)selectedFirma ?? DBNull.Value);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                vm.Entries.Add(new GeplanteFunktionEntry
                {
                    Bearbeiter = reader["Bearbeiter"]?.ToString() ?? string.Empty,
                    ID = reader["ID"] != DBNull.Value ? Convert.ToInt32(reader["ID"]) : 0,
                    Name1 = reader["Name1"]?.ToString() ?? string.Empty,
                    Titel = reader["Titel"]?.ToString() ?? string.Empty,
                    Benennung = reader["Benennung"]?.ToString() ?? string.Empty,
                    Stunden = reader["Stunden"] != DBNull.Value ? Convert.ToDecimal(reader["Stunden"]) : 0m,
                    Datum = reader["Datum"] != DBNull.Value ? Convert.ToDateTime(reader["Datum"]) : null,
                    Status = reader["Status"]?.ToString() ?? string.Empty,
                    FkTblAnsprechpartner = reader["fk_tblAnsprechpartner"] != DBNull.Value ? Convert.ToInt32(reader["fk_tblAnsprechpartner"]) : null,
                    StartDatum = reader["StartDatum"] != DBNull.Value ? Convert.ToDateTime(reader["StartDatum"]) : null
                });
            }

            return View(vm);
        }
    }
}
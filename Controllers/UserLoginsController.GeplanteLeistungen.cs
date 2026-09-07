using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using topfact.Pulse.Models;

namespace topfact.Pulse.Controllers
{
    public partial class UserLoginsController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> GeplanteLeistungen()
        {
            var vm = new GeplanteLeistungenViewModel();
            var connectionString = _configuration.GetConnectionString("ProjectControllingConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                return View(vm);

            // Detaildaten
            var sqlDetail = @"SELECT TOP (1000) [Bearbeiter], [ID], [Name1], [Titel], [Aktion], [Stunden], [Datum], [Status], [fk_tblAnsprechpartner] FROM [topteam].[dbo].[view_geplante_Leistungen]";
            // Gruppentabelle 1
            var sqlGruppe = @"SELECT [Bearbeiter], Sum([Stunden]) as PlanStunden, count(bearbeiter) as AnzahlPlanungen, Sum([Stunden]) * count(bearbeiter) / 100 as FaktorLeistungsplanung FROM [topteam].[dbo].[view_geplante_Leistungen] group by Bearbeiter";
            // Gruppentabelle 2
            var sqlLeistungen14Tg = @"SELECT [Bearbeiter], Sum([Stunden]) as LeistungsStunden_14Tg, count(bearbeiter) as AnzahlLeistungen_14Tg, Sum([Stunden]) * count(bearbeiter) / 100 as FaktorLeistung_14Tg FROM [topteam].[dbo].[view_erbrachte_Leistungen_14Tg] group by Bearbeiter";

            await using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                // Detaildaten
                await using (var cmd = new SqlCommand(sqlDetail, conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        vm.Entries.Add(new GeplanteLeistungEntry
                        {
                            Bearbeiter = reader["Bearbeiter"]?.ToString() ?? string.Empty,
                            ID = reader["ID"] as int? ?? Convert.ToInt32(reader["ID"]),
                            Name1 = reader["Name1"]?.ToString() ?? string.Empty,
                            Titel = reader["Titel"]?.ToString() ?? string.Empty,
                            Aktion = reader["Aktion"]?.ToString() ?? string.Empty,
                            Stunden = reader["Stunden"] as decimal? ?? Convert.ToDecimal(reader["Stunden"]),
                            Datum = reader["Datum"] as DateTime?,
                            Status = reader["Status"]?.ToString() ?? string.Empty,
                            FkTblAnsprechpartner = reader["fk_tblAnsprechpartner"] as int?
                        });
                    }
                }
                // Gruppentabelle 1
                await using (var cmd = new SqlCommand(sqlGruppe, conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        vm.Gruppen.Add(new GeplanteLeistungenGruppe
                        {
                            Bearbeiter = reader["Bearbeiter"]?.ToString() ?? string.Empty,
                            PlanStunden = reader["PlanStunden"] as decimal? ?? Convert.ToDecimal(reader["PlanStunden"]),
                            AnzahlPlanungen = reader["AnzahlPlanungen"] as int? ?? Convert.ToInt32(reader["AnzahlPlanungen"]),
                            FaktorLeistungsplanung = reader["FaktorLeistungsplanung"] as decimal? ?? Convert.ToDecimal(reader["FaktorLeistungsplanung"])
                        });
                    }
                }
                // Gruppentabelle 2
                await using (var cmd = new SqlCommand(sqlLeistungen14Tg, conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        vm.Leistungen14Tg.Add(new ErbrachteLeistungen14TgGruppe
                        {
                            Bearbeiter = reader["Bearbeiter"]?.ToString() ?? string.Empty,
                            LeistungsStunden_14Tg = reader["LeistungsStunden_14Tg"] as decimal? ?? Convert.ToDecimal(reader["LeistungsStunden_14Tg"]),
                            AnzahlLeistungen_14Tg = reader["AnzahlLeistungen_14Tg"] as int? ?? Convert.ToInt32(reader["AnzahlLeistungen_14Tg"]),
                            FaktorLeistung_14Tg = reader["FaktorLeistung_14Tg"] as decimal? ?? Convert.ToDecimal(reader["FaktorLeistung_14Tg"])
                        });
                    }
                }
            }
            return View(vm);
        }
    }
}

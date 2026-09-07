using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using topfact.Pulse.Data;
using topfact.Pulse.Models;

namespace topfact.Pulse.Controllers
{
    public class OrganisationController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _db;

        public OrganisationController(IConfiguration configuration, AppDbContext db)
        {
            _configuration = configuration;
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Vorgaenge()
        {
            var vm = new VorgaengeIndexViewModel();
            try
            {
                vm.Vorgaenge = await LoadVorgaengeAsync();
                vm.AufwandGeplantMinutenOverride = await LoadAufwandGeplantMinutenAsync();
            }
            catch (Exception ex)
            {
                vm.ErrorMessage = "Fehler beim Laden der Vorgänge: " + ex.Message;
                return View(vm);
            }

            try
            {
                await SaveDailySnapshotIfNeededAsync(vm.Vorgaenge);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "(kein InnerException)";
                vm.ErrorMessage = $"Snapshot-Fehler: {ex.Message} | Inner: {inner}";
            }

            return View(vm);
        }

        private async Task SaveDailySnapshotIfNeededAsync(List<VorgangListEntry> vorgaenge)
        {
            var today = DateTime.UtcNow.Date;
            var alreadySaved = await _db.VorgangSnapshots
                .AnyAsync(s => s.SnapshotTimeUtc >= today && s.SnapshotTimeUtc < today.AddDays(1));
            if (alreadySaved) return;

            var snapshotTime = DateTime.UtcNow;
            var snapshots = vorgaenge.Select(v => new VorgangSnapshot
            {
                SnapshotTimeUtc  = snapshotTime,
                VorgangsNr       = v.VorgangsNr,
                Firma            = v.Firma,
                Vorgangstitel    = v.Vorgangstitel,
                Vorgangsart      = v.Vorgangsart,
                Artikel          = v.Artikel,
                StatusName       = v.StatusName,
                StatusColor      = v.StatusColor,
                Bearbeiter       = v.Bearbeiter,
                Erfassungsdatum  = v.Erfassungsdatum,
                DateModified     = v.DateModified,
                DauerIstMinuten      = v.DauerIstMinuten,
                AktionenOffen        = v.Aktionen1,
                AktionenInBearbeitung = v.Aktionen2,
                AktionenErledigt     = v.Aktionen3,
                AnzahlKommentare     = v.AnzahlKommentare,
                KundeWartet          = v.KundeWartet ? 1 : 0,
            }).ToList();

            await _db.VorgangSnapshots.AddRangeAsync(snapshots);
            await _db.SaveChangesAsync();
        }

        private async Task<int> LoadAufwandGeplantMinutenAsync()
        {
            const string sql = "SELECT ISNULL(SUM([Stunden]), 0) FROM [dbo].[view_geplante_Leistungen]";
            var connStr = _configuration.GetConnectionString("ProjectControllingConnection") ?? "";
            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync();
            var stunden = result is DBNull || result is null ? 0m : Convert.ToDecimal(result);
            return (int)Math.Round(stunden * 60);
        }

        private static bool IsKundeWartet(string? statusName)
        {
            if (string.IsNullOrWhiteSpace(statusName)) return false;
            var s = statusName.ToLowerInvariant();
            return s.Contains("kunde") || s.Contains("feedback") || s.Contains("warte") || s.Contains("waiting");
        }

        private async Task<List<VorgangListEntry>> LoadVorgaengeAsync()
        {
            const string sql = @"
SELECT
    v.ID                                                AS VorgangsNr,
    f.Name1                                             AS Firma,
    v.Titel                                             AS Vorgangstitel,
    at.Aktivitaetentyp                                  AS Vorgangsart,
    ISNULL(a.Bezeichnung, N'kein Artikel')              AS Artikel,
    st.[Status]                                         AS StatusName,
    st.Color                                            AS StatusColor,
    LTRIM(RTRIM(COALESCE(ap.Vorname + N' ', N'') +
                COALESCE(ap.Nachname,    N'')))         AS Bearbeiter,
    v.Erfassungsdatum,
    v.DateModified,
    dbo.fn_GetVorgangDauerIst(v.ID)                     AS DauerIst,
    ISNULL(akt.Aktionen1, 0)                            AS Aktionen1,
    ISNULL(akt.Aktionen2, 0)                            AS Aktionen2,
    ISNULL(akt.Aktionen3, 0)                            AS Aktionen3,
    ISNULL(k.AnzahlKommentare, 0)                       AS AnzahlKommentare,
    k.LetzterKommentar
FROM dbo.tblAktivitaet            v
INNER JOIN dbo.tblAktivitaetentyp at ON v.fk_tblAktivitaetentyp     = at.ID
INNER JOIN dbo.tblFirma           f  ON v.fk_tblFirma               = f.ID
LEFT  JOIN dbo.tblAnsprechpartner ap ON v.fk_tblUserVerantwortlicher = ap.ID
LEFT  JOIN dbo.tblStatus          st ON v.fk_tblStatus              = st.ID
LEFT  JOIN dbo.tblArtikel         a  ON v.fk_tblArtikel             = a.ID
LEFT  JOIN (
    SELECT  a.fk_tblAktivitaet,
            SUM(CASE WHEN s.WorkTyp = 1 THEN 1 ELSE 0 END) AS Aktionen1,
            SUM(CASE WHEN s.WorkTyp = 2 THEN 1 ELSE 0 END) AS Aktionen2,
            SUM(CASE WHEN s.WorkTyp = 3 THEN 1 ELSE 0 END) AS Aktionen3
      FROM  dbo.tblAktivitaetAktion a
INNER JOIN  dbo.tblStatus          s ON s.ID = a.fk_tblStatus
  GROUP BY  a.fk_tblAktivitaet
) akt ON akt.fk_tblAktivitaet = v.ID
LEFT  JOIN (
    SELECT  EntityId,
            COUNT(*)         AS AnzahlKommentare,
            MAX(DateCreated) AS LetzterKommentar
      FROM  dbo.tblKommentare
     WHERE  EntityType = N'task'
  GROUP BY  EntityId
) k ON k.EntityId = v.ID
WHERE v.fk_tblAktivitaetentyp IN (10, 14)
  AND v.fk_tblBereich         IN (3, 5)
  AND st.WorkTyp              IN (1, 2)
ORDER BY v.Erfassungsdatum DESC, v.ID DESC;";

            var list = new List<VorgangListEntry>();
            var connStr = _configuration.GetConnectionString("ProjectControllingConnection") ?? "";

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();
            using var cmd = new SqlCommand(sql, conn);
            using var rdr = await cmd.ExecuteReaderAsync();

            while (await rdr.ReadAsync())
            {
                list.Add(new VorgangListEntry
                {
                    VorgangsNr = rdr.GetInt32(rdr.GetOrdinal("VorgangsNr")),
                    Firma = rdr["Firma"] as string ?? "",
                    Vorgangstitel = rdr["Vorgangstitel"] as string ?? "",
                    Vorgangsart = rdr["Vorgangsart"] as string ?? "",
                    Artikel = rdr["Artikel"] as string ?? "",
                    StatusName = rdr["StatusName"] as string ?? "",
                    StatusColor = rdr["StatusColor"] as string,
                    Bearbeiter = rdr["Bearbeiter"] as string ?? "",
                    Erfassungsdatum = rdr["Erfassungsdatum"] as DateTime?,
                    DateModified = rdr["DateModified"] as DateTime?,
                    DauerIstMinuten = rdr["DauerIst"] is int di ? di : Convert.ToInt32(rdr["DauerIst"] ?? 0),
                    Aktionen1 = Convert.ToInt32(rdr["Aktionen1"] ?? 0),
                    Aktionen2 = Convert.ToInt32(rdr["Aktionen2"] ?? 0),
                    Aktionen3 = Convert.ToInt32(rdr["Aktionen3"] ?? 0),
                    AnzahlKommentare = Convert.ToInt32(rdr["AnzahlKommentare"] ?? 0),
                    LetzterKommentar = rdr["LetzterKommentar"] as DateTime?,
                    KundeWartet = IsKundeWartet(rdr["StatusName"] as string),
                });
            }
            return list;
        }
    }
}

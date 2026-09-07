using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using topfact.Pulse.Models;

namespace topfact.Pulse.Pages
{
    /// <summary>
    /// Index-Seite "Aktive Vorgänge" — Liste aller laufenden Projekte/Aufträge.
    /// Klick auf eine Zeile öffnet /ProjektControlling?vorgangId=NNN (Detail-Controlling).
    /// </summary>
    public class VorgaengeModel : PageModel
    {
        private readonly IConfiguration _config;
        public VorgaengeIndexViewModel ViewModel { get; private set; } = new();

        public VorgaengeModel(IConfiguration config) => _config = config;

        public async Task OnGetAsync()
        {
            try
            {
                ViewModel.Vorgaenge = await LoadVorgaengeAsync();
            }
            catch (Exception ex)
            {
                ViewModel.ErrorMessage = "Fehler beim Laden der Vorgänge: " + ex.Message;
            }
        }

        /// <summary>JSON-Endpoint für AG Grid (asynchron nachladen, Filter, Suche).</summary>
        public async Task<IActionResult> OnGetDataAsync()
        {
            try
            {
                var rows = await LoadVorgaengeAsync();
                return new JsonResult(rows.Select(v => new {
                    vorgangsNr = v.VorgangsNr,
                    firma = v.Firma,
                    titel = v.Vorgangstitel,
                    vorgangsart = v.Vorgangsart,
                    artikel = v.Artikel,
                    statusName = v.StatusName,
                    statusColor = v.StatusColor,
                    statusKategorie = v.StatusKategorie,
                    bearbeiter = v.Bearbeiter,
                    erfassungsdatum = v.Erfassungsdatum,
                    dateModified = v.DateModified,
                    dauerIstMinuten = v.DauerIstMinuten,
                    aktionen1 = v.Aktionen1,
                    aktionen2 = v.Aktionen2,
                    aktionen3 = v.Aktionen3,
                    aktionenGesamt = v.AktionenGesamt,
                    kundeWartet = v.KundeWartet,
                    dauerPlanMinuten = v.DauerPlanMinuten,
                    anzahlChecklistenpunkte = v.AnzahlChecklistenpunkte,
                    abrechnbareAktionenMonat = v.AbrechnbareAktionenMonat,
                    anzahlKommentare = v.AnzahlKommentare,
                    letzterKommentar = v.LetzterKommentar,
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ────────────────────────────────────────────────────────────
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
    ISNULL((SELECT SUM(CP.Dauer) FROM dbo.tblAktivitaetChecklistenPunkte CP
             WHERE CP.fk_tblAktivitaet = v.ID), 0)     AS DauerPlanMinuten,
    ISNULL((SELECT COUNT(*) FROM dbo.tblAktivitaetChecklistenPunkte CP2
             WHERE CP2.fk_tblAktivitaet = v.ID), 0)    AS AnzahlChecklistenpunkte,
    ISNULL(akt.Aktionen1, 0)                            AS Aktionen1,
    ISNULL(akt.Aktionen2, 0)                            AS Aktionen2,
    ISNULL(akt.Aktionen3, 0)                            AS Aktionen3,
    ISNULL(k.AnzahlKommentare, 0)                       AS AnzahlKommentare,
    k.LetzterKommentar,
    CASE WHEN LOWER(st.[Status]) LIKE N'%kund%wart%'
              OR LOWER(st.[Status]) LIKE N'%wart%kund%'
              OR LOWER(st.[Status]) LIKE N'%kunde%'
              OR LOWER(st.[Status]) LIKE N'%feedback%'
         THEN 1 ELSE 0 END                             AS KundeWartet,
    ISNULL(abrMonat.AbrechnbareAktionen, 0)            AS AbrechnbareAktionenMonat
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
LEFT  JOIN (
    SELECT  aa.fk_tblAktivitaet,
            COUNT(*) AS AbrechnbareAktionen
      FROM  dbo.tblAktivitaetAktion aa
     WHERE  aa.Kostenpflichtig = 1
       AND  YEAR(aa.DatumErstellt)  = YEAR(GETDATE())
       AND  MONTH(aa.DatumErstellt) = MONTH(GETDATE())
  GROUP BY  aa.fk_tblAktivitaet
) abrMonat ON abrMonat.fk_tblAktivitaet = v.ID
WHERE v.fk_tblAktivitaetentyp IN (10, 14)
  AND v.fk_tblBereich         IN (3, 5)
  AND (
        st.WorkTyp IN (1, 2)
        OR LOWER(st.[Status]) LIKE N'%kund%'
        OR LOWER(st.[Status]) LIKE N'%wart%'
        OR LOWER(st.[Status]) LIKE N'%feedback%'
      )
ORDER BY v.Erfassungsdatum DESC, v.ID DESC;";

            var list = new List<VorgangListEntry>();
            var connStr = _config.GetConnectionString("Topteam") ?? "";

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
                    DauerPlanMinuten = Convert.ToInt32(rdr["DauerPlanMinuten"] ?? 0),
                    AnzahlChecklistenpunkte = Convert.ToInt32(rdr["AnzahlChecklistenpunkte"] ?? 0),
                    Aktionen1 = Convert.ToInt32(rdr["Aktionen1"] ?? 0),
                    Aktionen2 = Convert.ToInt32(rdr["Aktionen2"] ?? 0),
                    Aktionen3 = Convert.ToInt32(rdr["Aktionen3"] ?? 0),
                    KundeWartet = Convert.ToInt32(rdr["KundeWartet"] ?? 0) == 1,
                    AbrechnbareAktionenMonat = Convert.ToInt32(rdr["AbrechnbareAktionenMonat"] ?? 0),
                    AnzahlKommentare = Convert.ToInt32(rdr["AnzahlKommentare"] ?? 0),
                    LetzterKommentar = rdr["LetzterKommentar"] as DateTime?,
                });
            }
            return list;
        }
    }
}

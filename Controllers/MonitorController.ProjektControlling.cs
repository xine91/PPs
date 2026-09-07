using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using topfact.Pulse.Models;

namespace topfact.Pulse.Controllers
{
    public partial class MonitorController
    {
        // =====================================================================
        // ProjektControlling – Hauptansicht
        // =====================================================================
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> ProjektControlling(
            int selectedDays = 0,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string? vorgangNummernInput = null)
        {
            if (string.IsNullOrWhiteSpace(vorgangNummernInput))
                return Redirect("/pulse/Organisation/Organisation/Vorgaenge");

            var vm = new ProjektControllingControllerViewModel
            {
                SelectedDays = selectedDays,
                DateFrom = selectedDays == 0 ? dateFrom : null,
                DateTo = selectedDays == 0 ? dateTo : null,
                VorgangNummernInput = vorgangNummernInput
            };

            var connectionString = _configuration.GetConnectionString("ProjectControllingConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                vm.ErrorMessage = "Bitte `ConnectionStrings:ProjectControllingConnection` konfigurieren.";
                return View(vm);
            }

            if (!int.TryParse(
                    vorgangNummernInput
                        .Split(new[] { ',', ';', ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault()?.Trim(),
                    out var fkId))
            {
                vm.ErrorMessage = "Bitte eine gültige Vorgangs-ID eingeben.";
                return View(vm);
            }

            // ─────────────────────────────────────────────────────────────────
            // SQL 1: Vorgang-Header + Plan vs Ist
            // ─────────────────────────────────────────────────────────────────
            const string sqlVorgang = @"
                SELECT
                    V.ID AS VorgangId,
                    V.Titel AS VorgangTitel,
                    F.Name1 AS Firma,
                    V.Erfassungsdatum AS VorgangStart,
                    V.SollEndeDatum AS VorgangEnde,
                    AP.Vorname + ' ' + AP.Nachname AS Verantwortlicher,

                    (
                        SELECT COUNT(*)
                        FROM topteam.dbo.tblAktivitaetChecklistenPunkte CP
                        WHERE CP.fk_tblAktivitaet = V.ID
                    ) AS AnzahlChecklistenpunkte,

                    (
                        SELECT ISNULL(SUM(CP.Dauer), 0)
                        FROM topteam.dbo.tblAktivitaetChecklistenPunkte CP
                        WHERE CP.fk_tblAktivitaet = V.ID
                    ) AS PlanMinuten,

                    (
                        SELECT ISNULL(SUM(AA.Dauer), 0)
                        FROM topteam.dbo.tblAktivitaetAktion AA
                        WHERE AA.fk_tblAktivitaet = V.ID
                    ) AS IstMinuten,

                    (
                        SELECT COUNT(*)
                        FROM topteam.dbo.tblAktivitaetChecklistenPunkte CP
                        JOIN topteam.dbo.tblStatus S
                             ON S.ID = CP.fk_tblStatus
                        WHERE CP.fk_tblAktivitaet = V.ID
                          AND S.Status IN (N'Erledigt', N'Abgeschlossen')
                    ) AS AnzahlErledigt

                FROM topteam.dbo.tblAktivitaet V

                LEFT JOIN topteam.dbo.tblFirma F
                       ON F.ID = V.fk_tblFirma

                LEFT JOIN topteam.dbo.tblAnsprechpartner AP
                       ON AP.ID = V.fk_tblUserVerantwortlicher

                WHERE V.ID = @fkId";

            // ─────────────────────────────────────────────────────────────────
            // SQL 2: Bearbeiter-Breakdown
            // ─────────────────────────────────────────────────────────────────
            const string sqlBearbeiter = @"
                SELECT
                    AP.ID AS BearbeiterId,
                    AP.Vorname + ' ' + AP.Nachname AS Bearbeiter,

                    COUNT(DISTINCT CP.ID) AS AnzahlAktionen,

                    ISNULL(SUM(CP.Dauer), 0) AS PlanMinuten,

                    ISNULL(SUM(IstSummen.IstMin), 0) AS IstMinuten,

                    SUM(
                        CASE
                            WHEN S.Status IN (N'Erledigt', N'Abgeschlossen')
                            THEN 1
                            ELSE 0
                        END
                    ) AS Erledigt

                FROM topteam.dbo.tblAktivitaetChecklistenPunkte CP

                JOIN topteam.dbo.tblAnsprechpartner AP
                     ON AP.ID = CP.fk_Bearbeiter

                JOIN topteam.dbo.tblStatus S
                     ON S.ID = CP.fk_tblStatus

                OUTER APPLY (
                    SELECT SUM(AD.Dauer) AS IstMin
                    FROM topteam.dbo.tblAktivitaetAktion AA
                    JOIN topteam.dbo.tblAktivitaetAktionDauer AD
                         ON AD.fk_tblAktivitaetAktion = AA.ID
                    WHERE AA.AktivitaetChecklistenPunkteID = CP.ID
                ) IstSummen

                WHERE CP.fk_tblAktivitaet = @fkId

                GROUP BY
                    AP.ID,
                    AP.Vorname,
                    AP.Nachname

                ORDER BY PlanMinuten DESC";

            // ─────────────────────────────────────────────────────────────────
            // SQL 3: Aktionen / Tätigkeiten
            // BUGFIX: war hardcodiert auf 60927, jetzt @fkId
            // ─────────────────────────────────────────────────────────────────
            const string sqlAktionen = @"
                SELECT
                    AA.ID,
                    AA.DatumErstellt AS Datum,
                    AA.Aktion AS AktionTitel,
                    AP.Vorname + ' ' + AP.Nachname AS Bearbeiter,
                    ISNULL(SUM(AA.Dauer), 0) AS IstMinuten,
                    S.Status AS StatusText

                FROM topteam.dbo.tblAktivitaetAktion AA

                LEFT JOIN topteam.dbo.tblAnsprechpartner AP
                       ON AP.ID = AA.fk_tblBearbeiter

                LEFT JOIN topteam.dbo.tblStatus S
                       ON S.ID = AA.fk_tblStatus

                WHERE AA.fk_tblAktivitaet = @fkId

                GROUP BY
                    AA.ID,
                    AA.DatumErstellt,
                    AA.Aktion,
                    AP.Vorname,
                    AP.Nachname,
                    S.Status

                ORDER BY AA.DatumErstellt DESC";

            // ─────────────────────────────────────────────────────────────────
            // SQL 4: Checklisten-Punkte
            // ─────────────────────────────────────────────────────────────────
            const string sqlChecklist = @"
                SELECT
                    C.ID,
                    C.Nummer,
                    C.Benennung,

                    C.Dauer AS ArbeitszeitMinuten,

                    C.Plandatum,
                    C.DateCreated,

                    A.Vorname + ' ' + A.Nachname AS Bearbeiter,

                    S.Status AS StatusText,

                    V.Titel AS Vorgang,

                    F.Name1 AS Firma

                FROM topteam.dbo.tblAktivitaetChecklistenPunkte AS C

                JOIN topteam.dbo.tblAnsprechpartner AS A
                     ON C.fk_Bearbeiter = A.ID

                JOIN topteam.dbo.tblStatus AS S
                     ON C.fk_tblStatus = S.ID

                LEFT JOIN topteam.dbo.tblAktivitaet AS V
                       ON V.ID = C.fk_tblAktivitaet

                LEFT JOIN topteam.dbo.tblFirma AS F
                       ON V.fk_tblFirma = F.ID

                WHERE C.fk_tblAktivitaet = @fkId

                ORDER BY C.Plandatum";

            try
            {
                await using var connection = new SqlConnection(connectionString);

                await connection.OpenAsync();

                // ─────────────────────────────────────────────────────────────
                // Vorgang laden
                // ─────────────────────────────────────────────────────────────
                try
                {
                    await using var cmd = new SqlCommand(sqlVorgang, connection);

                    cmd.Parameters.AddWithValue("@fkId", fkId);

                    await using var r = await cmd.ExecuteReaderAsync();

                    if (await r.ReadAsync())
                    {
                        ViewBag.VorgangTitel = r["VorgangTitel"]?.ToString() ?? "";
                        ViewBag.VorgangFirma = r["Firma"]?.ToString() ?? "";
                        ViewBag.Verantwortlicher = r["Verantwortlicher"]?.ToString() ?? "";

                        ViewBag.VorgangStart =
                            r["VorgangStart"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(r["VorgangStart"]);

                        ViewBag.VorgangEnde =
                            r["VorgangEnde"] == DBNull.Value
                                ? (DateTime?)null
                                : Convert.ToDateTime(r["VorgangEnde"]);

                        ViewBag.AnzahlChecklistenpunkte =
                            Convert.ToInt32(r["AnzahlChecklistenpunkte"]);

                        ViewBag.PlanMinutenGesamt =
                            Convert.ToDecimal(r["PlanMinuten"]);

                        ViewBag.IstMinutenGesamt =
                            Convert.ToDecimal(r["IstMinuten"]);

                        ViewBag.AnzahlErledigt =
                            Convert.ToInt32(r["AnzahlErledigt"]);
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.VorgangQueryError = ex.ToString();
                }

                // ─────────────────────────────────────────────────────────────
                // Bearbeiter laden
                // ─────────────────────────────────────────────────────────────
                var bearbeiterStats = new List<dynamic>();

                try
                {
                    await using var cmd2 = new SqlCommand(sqlBearbeiter, connection);

                    cmd2.Parameters.AddWithValue("@fkId", fkId);

                    await using var r2 = await cmd2.ExecuteReaderAsync();

                    while (await r2.ReadAsync())
                    {
                        bearbeiterStats.Add(new
                        {
                            BearbeiterId = Convert.ToInt32(r2["BearbeiterId"]),
                            Bearbeiter = r2["Bearbeiter"]?.ToString() ?? "Unbekannt",
                            AnzahlAktionen = Convert.ToInt32(r2["AnzahlAktionen"]),
                            PlanMinuten = Convert.ToDecimal(r2["PlanMinuten"]),
                            IstMinuten = Convert.ToDecimal(r2["IstMinuten"]),
                            Erledigt = Convert.ToInt32(r2["Erledigt"])
                        });
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.BearbeiterQueryError = ex.ToString();
                }

                ViewBag.BearbeiterStats = bearbeiterStats;

                // ─────────────────────────────────────────────────────────────
                // Tätigkeiten laden
                // ─────────────────────────────────────────────────────────────
                var aktionenList = new List<dynamic>();

                try
                {
                    await using var cmdAk = new SqlCommand(sqlAktionen, connection);

                    cmdAk.Parameters.AddWithValue("@fkId", fkId);

                    await using var rAk = await cmdAk.ExecuteReaderAsync();

                    var datumOrdinal = PcGetOrdinal(rAk, "Datum", "DatumErstellt");
                    var titelOrdinal = PcGetOrdinal(rAk, "AktionTitel", "Aktion", "Titel", "Benennung");
                    var bearbeiterOrdinal = PcGetOrdinal(rAk, "Bearbeiter");
                    var istOrdinal = PcGetOrdinal(rAk, "IstMinuten");
                    var statusOrdinal = PcGetOrdinal(rAk, "StatusText", "Status");
                    var idOrdinal = PcGetOrdinal(rAk, "ID");

                    while (await rAk.ReadAsync())
                    {
                        aktionenList.Add(new
                        {
                            Id =
                                idOrdinal >= 0 && !rAk.IsDBNull(idOrdinal)
                                    ? Convert.ToInt32(rAk.GetValue(idOrdinal))
                                    : 0,

                            Datum =
                                datumOrdinal >= 0 && !rAk.IsDBNull(datumOrdinal)
                                    ? Convert.ToDateTime(rAk.GetValue(datumOrdinal))
                                    : (DateTime?)null,

                            Titel =
                                titelOrdinal >= 0 && !rAk.IsDBNull(titelOrdinal)
                                    ? rAk.GetValue(titelOrdinal)?.ToString() ?? ""
                                    : "",

                            Bearbeiter =
                                bearbeiterOrdinal >= 0 && !rAk.IsDBNull(bearbeiterOrdinal)
                                    ? rAk.GetValue(bearbeiterOrdinal)?.ToString() ?? "Unbekannt"
                                    : "Unbekannt",

                            IstMinuten =
                                istOrdinal >= 0 && !rAk.IsDBNull(istOrdinal)
                                    ? Convert.ToDecimal(rAk.GetValue(istOrdinal))
                                    : 0m,

                            StatusText =
                                statusOrdinal >= 0 && !rAk.IsDBNull(statusOrdinal)
                                    ? rAk.GetValue(statusOrdinal)?.ToString() ?? ""
                                    : ""
                        });
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.AktionenQueryError = ex.ToString();
                }

                ViewBag.AktionenList = aktionenList;

                // ─────────────────────────────────────────────────────────────
                // Checklisten laden
                // ─────────────────────────────────────────────────────────────
                await using var cmd3 = new SqlCommand(sqlChecklist, connection);

                cmd3.Parameters.AddWithValue("@fkId", fkId);

                await using var r3 = await cmd3.ExecuteReaderAsync();

                var bearbeiterOrd = PcGetOrdinal(r3, "Bearbeiter");
                var datumOrd = PcGetOrdinal(r3, "Plandatum", "DateCreated");
                var arbeitszeitOrd = PcGetOrdinal(r3, "ArbeitszeitMinuten", "Dauer");
                var idOrd = PcGetOrdinal(r3, "ID");
                var nummerOrd = PcGetOrdinal(r3, "Nummer");
                var vorgangOrd = PcGetOrdinal(r3, "Vorgang");
                var firmaOrd = PcGetOrdinal(r3, "Firma");
                var titelOrd = PcGetOrdinal(r3, "Benennung", "Titel");
                var statusOrd = PcGetOrdinal(r3, "StatusText", "Status");

                if (bearbeiterOrd < 0 || datumOrd < 0 || arbeitszeitOrd < 0)
                {
                    vm.ErrorMessage = "SQL fehlt Bearbeiter, Datum oder ArbeitszeitMinuten.";
                    return View(vm);
                }

                var entries = new List<ProjektControllingEntry>();

                while (await r3.ReadAsync())
                {
                    if (r3.IsDBNull(datumOrd))
                        continue;

                    var arbeitszeit =
                        r3.IsDBNull(arbeitszeitOrd)
                            ? 0m
                            : Convert.ToDecimal(r3.GetValue(arbeitszeitOrd));

                    entries.Add(new ProjektControllingEntry
                    {
                        Id =
                            idOrd >= 0 && !r3.IsDBNull(idOrd)
                                ? Convert.ToInt32(r3.GetValue(idOrd))
                                : null,

                        Nummer =
                            nummerOrd >= 0 && !r3.IsDBNull(nummerOrd)
                                ? r3.GetValue(nummerOrd)?.ToString() ?? ""
                                : "",

                        Vorgang =
                            vorgangOrd >= 0 && !r3.IsDBNull(vorgangOrd)
                                ? r3.GetValue(vorgangOrd)?.ToString() ?? ""
                                : "",

                        Firma =
                            firmaOrd >= 0 && !r3.IsDBNull(firmaOrd)
                                ? r3.GetValue(firmaOrd)?.ToString() ?? ""
                                : "",

                        Bearbeiter =
                            r3.IsDBNull(bearbeiterOrd)
                                ? "Unbekannt"
                                : r3.GetValue(bearbeiterOrd)?.ToString() ?? "Unbekannt",

                        Datum =
                            Convert.ToDateTime(r3.GetValue(datumOrd)),

                        ArbeitszeitMinuten = arbeitszeit,

                        Titel =
                            titelOrd >= 0 && !r3.IsDBNull(titelOrd)
                                ? r3.GetValue(titelOrd)?.ToString() ?? ""
                                : "",

                        StatusText =
                            statusOrd >= 0 && !r3.IsDBNull(statusOrd)
                                ? r3.GetValue(statusOrd)?.ToString() ?? ""
                                : ""
                    });
                }

                // ─────────────────────────────────────────────────────────────
                // Zeitraumfilter
                // ─────────────────────────────────────────────────────────────
                if (selectedDays > 0)
                {
                    (DateTime rangeStart, DateTime? rangeEnd) =
                        GetRange(selectedDays, dateFrom, dateTo, true);

                    entries = entries
                        .Where(x =>
                            x.Datum >= rangeStart &&
                            (!rangeEnd.HasValue || x.Datum < rangeEnd.Value))
                        .ToList();
                }
                else if (dateFrom.HasValue || dateTo.HasValue)
                {
                    if (dateFrom.HasValue)
                        entries = entries
                            .Where(x => x.Datum >= dateFrom.Value)
                            .ToList();

                    if (dateTo.HasValue)
                        entries = entries
                            .Where(x => x.Datum <= dateTo.Value)
                            .ToList();
                }

                vm.Entries = entries
                    .OrderBy(x => x.Datum)
                    .ThenBy(x => x.Bearbeiter)
                    .ToList();

                if (vm.Entries.Count == 0 &&
                    (ViewBag.AnzahlChecklistenpunkte ?? 0) == 0)
                {
                    vm.ErrorMessage = "Keine Daten für diesen Vorgang gefunden.";
                }
            }
            catch (Exception ex)
            {
                vm.ErrorMessage = $"Fehler: {ex}";
            }

            return View(vm);
        }

        // =====================================================================
        // PDF-Export – Wochenbericht
        // GET /Monitor/ExportWeeklyPdf?vorgangNummernInput=60927
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> ExportWeeklyPdf(string? vorgangNummernInput)
        {
            if (string.IsNullOrWhiteSpace(vorgangNummernInput))
                return BadRequest("Keine Vorgangs-ID angegeben.");

            if (!int.TryParse(
                    vorgangNummernInput
                        .Split(new[] { ',', ';', ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault()?.Trim(),
                    out var fkId))
                return BadRequest("Ungültige Vorgangs-ID.");

            var connectionString = _configuration.GetConnectionString("ProjectControllingConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                return StatusCode(500, "Datenbankverbindung nicht konfiguriert.");

            // Aktuelle Woche: Montag 00:00 bis Sonntag 23:59
            var today = DateTime.Today;
            var dow = (int)today.DayOfWeek;
            var weekStart = today.AddDays(dow == 0 ? -6 : -(dow - 1));
            var weekEnd = weekStart.AddDays(7);

            var model = await BuildWeeklyReportModelAsync(connectionString, fkId, weekStart, weekEnd);

            QuestPDF.Settings.License = LicenseType.Community;
            var pdfBytes = new ProjektWeeklyReportDocument(model).GeneratePdf();
            var filename = $"Wochenbericht_{model.VorgangNr}_{weekStart:yyyy-MM-dd}.pdf";

            return File(pdfBytes, "application/pdf", filename);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Daten für den Wochenbericht laden
        // ─────────────────────────────────────────────────────────────────────
        private async Task<WeeklyReportModel> BuildWeeklyReportModelAsync(
            string connectionString, int fkId, DateTime weekStart, DateTime weekEnd)
        {
            var m = new WeeklyReportModel
            {
                VorgangNr = fkId.ToString(),
                WeekStart = weekStart,
                WeekEnd = weekEnd.AddSeconds(-1)
            };

            const string sqlHeader = @"
                SELECT
                    V.Titel,
                    F.Name1                           AS Firma,
                    V.Erfassungsdatum                 AS VorgangStart,
                    V.SollEndeDatum                   AS VorgangEnde,
                    AP.Vorname + ' ' + AP.Nachname    AS Verantwortlicher,
                    (SELECT COUNT(*)
                     FROM topteam.dbo.tblAktivitaetChecklistenPunkte CP
                     WHERE CP.fk_tblAktivitaet = V.ID)                        AS AnzahlGesamt,
                    (SELECT ISNULL(SUM(CP.Dauer),0)
                     FROM topteam.dbo.tblAktivitaetChecklistenPunkte CP
                     WHERE CP.fk_tblAktivitaet = V.ID)                        AS PlanMinuten,
                    (SELECT ISNULL(SUM(AA.Dauer),0)
                     FROM topteam.dbo.tblAktivitaetAktion AA
                     WHERE AA.fk_tblAktivitaet = V.ID)                        AS IstMinuten,
                    (SELECT COUNT(*)
                     FROM topteam.dbo.tblAktivitaetChecklistenPunkte CP
                     JOIN topteam.dbo.tblStatus S ON S.ID = CP.fk_tblStatus
                     WHERE CP.fk_tblAktivitaet = V.ID
                       AND S.Status IN (N'Erledigt',N'Abgeschlossen',N'Abgerechnet')) AS Erledigt,
                    (SELECT COUNT(*)
                     FROM topteam.dbo.tblAktivitaetChecklistenPunkte CP
                     JOIN topteam.dbo.tblStatus S ON S.ID = CP.fk_tblStatus
                     WHERE CP.fk_tblAktivitaet = V.ID
                       AND S.Status IN (N'Durchführung',N'Bearbeitung',N'Aktiv'))     AS Aktiv
                FROM topteam.dbo.tblAktivitaet V
                LEFT JOIN topteam.dbo.tblFirma F            ON F.ID = V.fk_tblFirma
                LEFT JOIN topteam.dbo.tblAnsprechpartner AP ON AP.ID = V.fk_tblUserVerantwortlicher
                WHERE V.ID = @fkId";

            const string sqlAktionenWoche = @"
                SELECT
                    AA.DatumErstellt                 AS Datum,
                    AA.Aktion                        AS Titel,
                    AP.Vorname + ' ' + AP.Nachname   AS Bearbeiter,
                    ISNULL(SUM(AA.Dauer),0)          AS IstMinuten,
                    S.Status                         AS StatusText
                FROM topteam.dbo.tblAktivitaetAktion AA
                LEFT JOIN topteam.dbo.tblAnsprechpartner AP ON AP.ID = AA.fk_tblBearbeiter
                LEFT JOIN topteam.dbo.tblStatus S           ON S.ID  = AA.fk_tblStatus
                WHERE AA.fk_tblAktivitaet = @fkId
                  AND AA.DatumErstellt   >= @weekStart
                  AND AA.DatumErstellt   <  @weekEnd
                GROUP BY AA.DatumErstellt, AA.Aktion, AP.Vorname, AP.Nachname, S.Status
                ORDER BY AA.DatumErstellt ASC";

            const string sqlBearbeiterWoche = @"
                SELECT
                    AP.Vorname + ' ' + AP.Nachname  AS Bearbeiter,
                    COUNT(DISTINCT AA.ID)           AS Anzahl,
                    ISNULL(SUM(AA.Dauer),0)         AS IstMinuten
                FROM topteam.dbo.tblAktivitaetAktion AA
                LEFT JOIN topteam.dbo.tblAnsprechpartner AP ON AP.ID = AA.fk_tblBearbeiter
                WHERE AA.fk_tblAktivitaet = @fkId
                  AND AA.DatumErstellt   >= @weekStart
                  AND AA.DatumErstellt   <  @weekEnd
                GROUP BY AP.Vorname, AP.Nachname
                ORDER BY IstMinuten DESC";

            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            await using (var cmd = new SqlCommand(sqlHeader, conn))
            {
                cmd.Parameters.AddWithValue("@fkId", fkId);
                await using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    m.VorgangTitel = r["Titel"]?.ToString() ?? "–";
                    m.Firma = r["Firma"]?.ToString() ?? "–";
                    m.Verantwortlicher = r["Verantwortlicher"]?.ToString() ?? "–";
                    m.VorgangStart = r["VorgangStart"] == DBNull.Value ? null : Convert.ToDateTime(r["VorgangStart"]);
                    m.VorgangEnde = r["VorgangEnde"] == DBNull.Value ? null : Convert.ToDateTime(r["VorgangEnde"]);
                    m.PlanMinuten = Convert.ToDecimal(r["PlanMinuten"]);
                    m.IstMinuten = Convert.ToDecimal(r["IstMinuten"]);
                    m.AnzahlGesamt = Convert.ToInt32(r["AnzahlGesamt"]);
                    m.AnzahlErledigt = Convert.ToInt32(r["Erledigt"]);
                    m.AnzahlAktiv = Convert.ToInt32(r["Aktiv"]);
                }
            }

            await using (var cmd = new SqlCommand(sqlAktionenWoche, conn))
            {
                cmd.Parameters.AddWithValue("@fkId", fkId);
                cmd.Parameters.AddWithValue("@weekStart", weekStart);
                cmd.Parameters.AddWithValue("@weekEnd", weekEnd);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    m.Taetigkeiten.Add(new WochenTaetigkeit
                    {
                        Titel = r["Titel"]?.ToString() ?? "–",
                        Bearbeiter = r["Bearbeiter"]?.ToString() ?? "–",
                        Datum = r["Datum"] == DBNull.Value ? null : Convert.ToDateTime(r["Datum"]),
                        IstMinuten = Convert.ToDecimal(r["IstMinuten"]),
                        StatusText = r["StatusText"]?.ToString() ?? ""
                    });
            }

            await using (var cmd = new SqlCommand(sqlBearbeiterWoche, conn))
            {
                cmd.Parameters.AddWithValue("@fkId", fkId);
                cmd.Parameters.AddWithValue("@weekStart", weekStart);
                cmd.Parameters.AddWithValue("@weekEnd", weekEnd);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    m.Bearbeiter.Add(new BearbeiterStat
                    {
                        Name = r["Bearbeiter"]?.ToString() ?? "–",
                        Anzahl = Convert.ToInt32(r["Anzahl"]),
                        IstMinuten = Convert.ToDecimal(r["IstMinuten"])
                    });
            }

            return m;
        }
         
        private static int PcGetOrdinal(SqlDataReader reader, params string[] names)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (names.Any(n =>
                    string.Equals(
                        n,
                        reader.GetName(i),
                        StringComparison.OrdinalIgnoreCase)))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
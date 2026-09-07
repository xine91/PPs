using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using topfact.Pulse.Models.Organisation;

namespace topfact.Pulse.Controllers
{
    public class ITSicherheitController : Controller
    {
        private readonly IConfiguration _configuration;

        public ITSicherheitController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult ITSicherheitsorganisation()
        {
            return View("~/Views/Organisation/ITSicherheitsorganisation.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> ITSicherheitsorganisationData()
        {
            const string sql = @"
                SELECT
                    C.Nummer,
                    C.Benennung,
                    C.Beschreibung,
                    C.Bemerkung,
                    C.Dauer,
                    C.Plandatum,
                    C.DateCreated,
                    A.Vorname + ' ' + A.Nachname AS Bearbeiter,
                    S.Status
                FROM topteam.dbo.tblAktivitaetChecklistenPunkte AS C
                JOIN topteam.dbo.tblAnsprechpartner AS A ON C.fk_Bearbeiter = A.ID
                JOIN topteam.dbo.tblStatus         AS S ON C.fk_tblStatus  = S.ID
                WHERE C.fk_tblAktivitaet = '59023'";

            var connectionString = _configuration.GetConnectionString("ProjectControllingConnection");

            using var connection = new SqlConnection(connectionString);
            var data = await connection.QueryAsync<ChecklistenPunktDto>(sql);

            return Json(data);
        }
    }
}
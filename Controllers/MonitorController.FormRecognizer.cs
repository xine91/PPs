using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using topfact.Pulse.Models;

namespace topfact.Pulse.Controllers
{
    public partial class MonitorController
    {
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> FormRecognizer(
            int selectedDays = 30,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            List<FormRecognizerStat> stats;

            if (selectedDays != 30 || dateFrom.HasValue || dateTo.HasValue)
            {
                var (rangeStart, rangeEnd) = GetRange(selectedDays, dateFrom, dateTo, true);

                const string sql = @"
                    SELECT ISNULL(ae.OrgName, 'Unbekannt') AS OrgName,
                           COUNT(DISTINCT aep.ParamValue)  AS RecognizedDocuments,
                           0                               AS Volume
                    FROM [topfactAnalytics].[dbo].[ApplicationEvent] ae
                    JOIN [topfactAnalytics].[dbo].[ApplicationEventParameter] aep
                        ON ae.EventID = aep.EventID
                    WHERE ae.AppName = @appName
                      AND ae.DateCreated >= @from
                      AND ae.DateCreated < @to
                      AND aep.ParamKey = 'DocID'
                    GROUP BY ae.OrgName
                    ORDER BY ae.OrgName;";

                stats = await _db.Database
                    .SqlQueryRaw<FormRecognizerStat>(
                        sql,
                        new SqlParameter("@appName", "topfact6 Jobserver Modul FormRecognizer"),
                        new SqlParameter("@from", rangeStart),
                        new SqlParameter("@to", rangeEnd))
                    .ToListAsync();
            }
            else
            {
                stats = await _db.FormRecStatuses
                    .AsNoTracking()
                    .Select(x => new FormRecognizerStat
                    {
                        OrgName = x.OrgName ?? "Unbekannt",
                        RecognizedDocuments = x.RecognizedDocuments ?? 0,
                        Volume = 0
                    })
                    .OrderBy(x => x.OrgName)
                    .ToListAsync();
            }

            var vm = new FormRecognizerViewModel
            {
                SelectedDays = selectedDays,
                DateFrom = selectedDays == 0 ? dateFrom : null,
                DateTo = selectedDays == 0 ? dateTo : null,
                Stats = stats
            };

            return View(vm);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using topfact.Pulse.Models;

namespace topfact.Pulse.Controllers
{
    public partial class MonitorController
    {
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Licenses(
            int selectedDays = 30,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var baseQuery = _appDb.ServerSnapshots
                .AsNoTracking()
                .Where(x => x.SnapshotTimeUTC != null && x.OrgName != null);

            var (rangeStart, rangeEnd) = GetRange(selectedDays, dateFrom, dateTo, true);

            var entries = await baseQuery
                .Where(x => x.SnapshotTimeUTC >= rangeStart && x.SnapshotTimeUTC < rangeEnd)
                .GroupBy(x => x.OrgName)
                .Select(g => new
                {
                    OrgName = g.Key!,
                    SnapshotTimeUTC = g.Max(x => x.SnapshotTimeUTC)
                })
                .Join(baseQuery,
                    g => new { g.OrgName, g.SnapshotTimeUTC },
                    x => new { OrgName = x.OrgName!, x.SnapshotTimeUTC },
                    (_, x) => new LicenseEntry
                    {
                        Tenant = x.OrgName ?? "Unbekannt",
                        Licenses = x.TfaLicenseCount ?? 0,
                        Date = x.TfaLicenseEnd ?? x.SnapshotTimeUTC
                    })
                .OrderBy(x => x.Tenant)
                .ToListAsync();

            var vm = new LicensesViewModel
            {
                SelectedDays = selectedDays,
                DateFrom = selectedDays == 0 ? dateFrom : null,
                DateTo = selectedDays == 0 ? dateTo : null,
                Entries = entries
            };

            return View(vm);
        }
    }
}

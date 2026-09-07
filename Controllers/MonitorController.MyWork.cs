using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using topfact.Pulse.Models;

namespace topfact.Pulse.Controllers
{
    public partial class MonitorController
    {
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> MyWorkCloud(
            string? selectedCustomer,
            int selectedDays = 30,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            const string appNameFilter = "MyWork Cloud";
            var oneYearAgo = DateTime.Now.AddDays(-365);

            var baseFilterQuery = _db.ClientStartupInformations
                .AsNoTracking()
                .Where(x => x.DateCreated != null && x.DateCreated > oneYearAgo)
                .Where(x => x.AppName != null &&
                            (x.AppName == appNameFilter ||
                             (x.AppName.Contains("MyWork") && x.AppName.Contains("Cloud"))));

            var availableCustomers = await baseFilterQuery
                .Where(x => x.Customer != null)
                .Select(x => x.Customer!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var (cutoffStart, cutoffEnd) = GetRange(selectedDays, dateFrom, dateTo);

            var tableQuery = baseFilterQuery
                .Where(x => x.DateCreated >= cutoffStart);

            if (cutoffEnd.HasValue)
                tableQuery = tableQuery.Where(x => x.DateCreated < cutoffEnd);

            if (!string.IsNullOrEmpty(selectedCustomer))
            {
                tableQuery = tableQuery.Where(x => x.Customer == selectedCustomer);
            }

            var today = DateTime.Today;
            var totalLogins = await tableQuery.CountAsync();
            var loginsToday = await tableQuery.CountAsync(x => x.DateCreated != null && x.DateCreated.Value.Date == today);
            var uniqueUsers = await tableQuery.Select(x => x.Username).Distinct().CountAsync();

            var logins = await tableQuery
                .OrderByDescending(x => x.DateCreated)
                .Take(10000)
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

            var vm = new MyWorkViewModel
            {
                AvailableCustomers = availableCustomers,
                SelectedCustomer = selectedCustomer,
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
        [ActionName("topfact6 MyWork")]
        public async Task<IActionResult> Topfact6MyWork(
            string? selectedCustomer,
            int selectedDays = 30,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            const string appNameFilter = "topfact6 MyWork";
            var oneYearAgo = DateTime.Now.AddDays(-365);

            var baseFilterQuery = _db.ClientStartupInformations
                .AsNoTracking()
                .Where(x => x.DateCreated != null && x.DateCreated > oneYearAgo)
                .Where(x => x.AppName == appNameFilter);

            var availableCustomers = await baseFilterQuery
                .Where(x => x.Customer != null)
                .Select(x => x.Customer!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var (cutoffStart, cutoffEnd) = GetRange(selectedDays, dateFrom, dateTo);

            var tableQuery = baseFilterQuery
                .Where(x => x.DateCreated >= cutoffStart);

            if (cutoffEnd.HasValue)
                tableQuery = tableQuery.Where(x => x.DateCreated < cutoffEnd);

            if (!string.IsNullOrEmpty(selectedCustomer))
            {
                tableQuery = tableQuery.Where(x => x.Customer == selectedCustomer);
            }

            var today = DateTime.Today;
            var totalLogins = await tableQuery.CountAsync();
            var loginsToday = await tableQuery.CountAsync(x => x.DateCreated != null && x.DateCreated.Value.Date == today);
            var uniqueUsers = await tableQuery.Select(x => x.Username).Distinct().CountAsync();

            var logins = await tableQuery
                .OrderByDescending(x => x.DateCreated)
                .Take(10000)
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

            var vm = new MyWorkViewModel
            {
                AvailableCustomers = availableCustomers,
                SelectedCustomer = selectedCustomer,
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
        [ActionName("topfact MyWork App")]
        public async Task<IActionResult> TopfactMyWorkApp(
            string? selectedCustomer,
            int selectedDays = 30,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            const string appNameFilter = "topfact MyWork App";
            var oneYearAgo = DateTime.Now.AddDays(-365);

            var baseFilterQuery = _db.ClientStartupInformations
                .AsNoTracking()
                .Where(x => x.DateCreated != null && x.DateCreated > oneYearAgo)
                .Where(x => x.AppName == appNameFilter);

            var availableCustomers = await baseFilterQuery
                .Where(x => x.Customer != null)
                .Select(x => x.Customer!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var (cutoffStart, cutoffEnd) = GetRange(selectedDays, dateFrom, dateTo);

            var tableQuery = baseFilterQuery
                .Where(x => x.DateCreated >= cutoffStart);

            if (cutoffEnd.HasValue)
                tableQuery = tableQuery.Where(x => x.DateCreated < cutoffEnd);

            if (!string.IsNullOrEmpty(selectedCustomer))
            {
                tableQuery = tableQuery.Where(x => x.Customer == selectedCustomer);
            }

            var today = DateTime.Today;
            var totalLogins = await tableQuery.CountAsync();
            var loginsToday = await tableQuery.CountAsync(x => x.DateCreated != null && x.DateCreated.Value.Date == today);
            var uniqueUsers = await tableQuery.Select(x => x.Username).Distinct().CountAsync();

            var logins = await tableQuery
                .OrderByDescending(x => x.DateCreated)
                .Take(10000)
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

            var vm = new MyWorkViewModel
            {
                AvailableCustomers = availableCustomers,
                SelectedCustomer = selectedCustomer,
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
        [ActionName("topfact6 BestPeople")]
        public async Task<IActionResult> Topfact6BestPeople(
            string? selectedCustomer,
            int selectedDays = 365,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            const string appNameFilter = "topfact6 Best People";
            var oneYearAgo = DateTime.Now.AddDays(-365);

            var baseFilterQuery = _db.ClientStartupInformations
                .AsNoTracking()
                .Where(x => x.DateCreated != null && x.DateCreated > oneYearAgo)
                .Where(x => x.AppName == appNameFilter);

            var availableCustomers = await baseFilterQuery
                .Where(x => x.Customer != null)
                .Select(x => x.Customer!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var (cutoffStart, cutoffEnd) = GetRange(selectedDays, dateFrom, dateTo);

            var tableQuery = baseFilterQuery
                .Where(x => x.DateCreated >= cutoffStart);

            if (cutoffEnd.HasValue)
                tableQuery = tableQuery.Where(x => x.DateCreated < cutoffEnd);

            if (!string.IsNullOrEmpty(selectedCustomer))
            {
                tableQuery = tableQuery.Where(x => x.Customer == selectedCustomer);
            }

            var today = DateTime.Today;
            var totalLogins = await tableQuery.CountAsync();
            var loginsToday = await tableQuery.CountAsync(x => x.DateCreated != null && x.DateCreated.Value.Date == today);
            var uniqueUsers = await tableQuery.Select(x => x.Username).Distinct().CountAsync();

            var logins = await tableQuery
                .OrderByDescending(x => x.DateCreated)
                .Take(10000)
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

            var vm = new MyWorkViewModel
            {
                AvailableCustomers = availableCustomers,
                SelectedCustomer = selectedCustomer,
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
        [ActionName("topfact6 Facility")]
        public async Task<IActionResult> Topfact6Facility(
            string? selectedCustomer,
            int selectedDays = 365,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            const string appNameFilter = "topfact6 Facility";
            var oneYearAgo = DateTime.Now.AddDays(-365);

            var baseFilterQuery = _db.ClientStartupInformations
                .AsNoTracking()
                .Where(x => x.DateCreated != null && x.DateCreated > oneYearAgo)
                .Where(x => x.AppName == appNameFilter);

            var availableCustomers = await baseFilterQuery
                .Where(x => x.Customer != null)
                .Select(x => x.Customer!)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var (cutoffStart, cutoffEnd) = GetRange(selectedDays, dateFrom, dateTo);

            var tableQuery = baseFilterQuery
                .Where(x => x.DateCreated >= cutoffStart);

            if (cutoffEnd.HasValue)
                tableQuery = tableQuery.Where(x => x.DateCreated < cutoffEnd);

            if (!string.IsNullOrEmpty(selectedCustomer))
            {
                tableQuery = tableQuery.Where(x => x.Customer == selectedCustomer);
            }

            var today = DateTime.Today;
            var totalLogins = await tableQuery.CountAsync();
            var loginsToday = await tableQuery.CountAsync(x => x.DateCreated != null && x.DateCreated.Value.Date == today);
            var uniqueUsers = await tableQuery.Select(x => x.Username).Distinct().CountAsync();

            var logins = await tableQuery
                .OrderByDescending(x => x.DateCreated)
                .Take(10000)
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

            var vm = new MyWorkViewModel
            {
                AvailableCustomers = availableCustomers,
                SelectedCustomer = selectedCustomer,
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
    }
}

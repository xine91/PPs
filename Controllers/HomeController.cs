using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using topfact.Pulse.Data;
using topfact.Pulse.Models;

public class HomeController : Controller
{
    private readonly AppDbContext _context;

    private sealed class CurrentAlertItem
    {
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public int SeverityCode { get; init; }
    }

    public HomeController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var servers = await _context.view_ServerAnalytics_Aktuell
            .AsNoTracking()
            .ToListAsync();

        var model = new DashboardViewModel
        {
            Servers = servers,

            TotalServers = servers.Count,
            OnlineServers = servers.Count(x => x.GesamtStatusCode == 0),
            OfflineServers = servers.Count(x => x.GesamtStatusCode == 2),
            ServersWithErrors = servers.Count(x => x.GesamtStatusCode == 1)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Leitstand(string? org)
    {
        var organizations = await _context.ServerSnapshots
            .AsNoTracking()
            .Where(x => x.OrgName != null)
            .Select(x => x.OrgName!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        var vm = new LeitstandViewModel
        {
            Organizations = organizations,
            SelectedOrg = org
        };

        if (!string.IsNullOrEmpty(org))
        {
            vm.LatestSnapshot = await _context.ServerSnapshots
                .AsNoTracking()
                .Where(x => x.OrgName == org)
                .OrderByDescending(x => x.SnapshotTimeUTC)
                .FirstOrDefaultAsync();

            vm.History = await _context.ServerSnapshots
                .AsNoTracking()
                .Where(x => x.OrgName == org)
                .OrderByDescending(x => x.SnapshotTimeUTC)
                .Take(50)
                .ToListAsync();
        }

        return View(vm);
    }

    public async Task<IActionResult> Dashboard()
    {
        var snapshots = await _context.ServerSnapshots
            .AsNoTracking()
            .Where(x => x.OrgName != null)
            .OrderByDescending(x => x.SnapshotTimeUTC)
            .ToListAsync();

        var latestPerOrg = snapshots
            .GroupBy(x => x.OrgName)
            .Select(g => g.First())
            .ToList();

        var today = DateTime.Today;

        var vm = new LeitstandViewModel
        {
            TenantCount = latestPerOrg.Count,
            TenantsWithSnapshotToday = latestPerOrg.Count(x => x!.SnapshotTimeUTC.HasValue && x.SnapshotTimeUTC.Value.Date == today),
            ActiveUsersWeekTotal = latestPerOrg.Sum(x => x!.TfaActiveUsersWeek ?? 0),
            ErrorsTodayTotal = latestPerOrg.Sum(x => x!.TfaTFLogErrorsToday ?? 0),
            DocsLast30DaysTotal = latestPerOrg.Sum(x => x!.TfaDocsLast30Days ?? 0),
            TotalDocuments = latestPerOrg.Sum(x => x!.TfaTotalDocs ?? 0),
            TotalFiles = latestPerOrg.Sum(x => x!.TfaTotalFiles ?? 0),
            M365MailsTodayTotal = latestPerOrg.Sum(x => x!.TF_M365_Mails_today ?? 0),
            AvgCpuUsagePercent = latestPerOrg.Count == 0
                ? 0
                : Math.Round(latestPerOrg.Average(x => x!.CpuUsagePercent ?? 0), 1, MidpointRounding.AwayFromZero),
            AvgMemoryUsagePercent = latestPerOrg.Count == 0 ? 0 : Math.Round(latestPerOrg.Average(x => x!.MemoryUsagePercent ?? 0), 1),
            AvgDiskUsagePercent = latestPerOrg.Count == 0 ? 0 : Math.Round(latestPerOrg.Average(x => x!.DiskUsagePercent ?? 0), 1),
            TotalDatabaseSizeMB = latestPerOrg.Sum(x => x!.TfaDatabaseSizeMB ?? 0),
            TfaCountTodayTotal = latestPerOrg.Sum(x => x!.TfaCountToday ?? 0),
            ProcessesNotRespondingTotal = latestPerOrg.Sum(x => x!.ProcessNotResponding ?? 0),
            LicensesExpiringSoon = latestPerOrg.Count(x => x!.TfaLicenseEnd.HasValue && x.TfaLicenseEnd.Value <= DateTime.Today.AddDays(30)),
            AvgSystemUptimeHours = latestPerOrg.Count == 0 ? 0 : Math.Round(latestPerOrg.Average(x => (double)(x!.SystemUptimeHours ?? 0)), 1)
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> LeitstandDetail(string org)
    {
        if (string.IsNullOrEmpty(org))
            return BadRequest();

        var snapshot = await _context.ServerSnapshots
            .AsNoTracking()
            .Where(x => x.OrgName == org)
            .OrderByDescending(x => x.SnapshotTimeUTC)
            .FirstOrDefaultAsync();

        if (snapshot == null)
            return NotFound();

        var history = await _context.ServerSnapshots
            .AsNoTracking()
            .Where(x => x.OrgName == org)
            .OrderByDescending(x => x.SnapshotTimeUTC)
            .Take(50)
            .ToListAsync();

        var currentStatus = await _context.view_ServerAnalytics_Aktuell
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name1 == org);

        var alerts = BuildCurrentAlerts(currentStatus, snapshot);

        return Json(new
        {
            snapshot = new
            {
                orgName = snapshot.OrgName,
                machineName = snapshot.MachineName,
                snapshotTimeUTC = snapshot.SnapshotTimeUTC?.ToString("dd.MM.yyyy HH:mm"),
                hostname = snapshot.Hostname,
                ipAddress = snapshot.IPAddress,
                osVersion = snapshot.OSVersion,
                osArchitecture = snapshot.OSArchitecture,
                dotNetVersion = snapshot.DotNetVersion,
                systemUptimeHours = snapshot.SystemUptimeHours,

                cpuName = snapshot.CpuName,
                cpuCores = snapshot.CpuCores,
                cpuLogicalProcessors = snapshot.CpuLogicalProcessors,
                cpuUsagePercent = snapshot.CpuUsagePercent,

                memoryTotalMB = snapshot.MemoryTotalMB,
                memoryFreeMB = snapshot.MemoryFreeMB,
                memoryUsagePercent = snapshot.MemoryUsagePercent,

                diskTotalGB = snapshot.DiskTotalGB,
                diskFreeGB = snapshot.DiskFreeGB,
                diskUsagePercent = snapshot.DiskUsagePercent,

                tfaCountToday = snapshot.TfaCountToday,
                tfaActiveUsersWeek = snapshot.TfaActiveUsersWeek,
                tfaTotalDocs = snapshot.TfaTotalDocs,
                tfaDocsLast30Days = snapshot.TfaDocsLast30Days,
                tfaTotalFiles = snapshot.TfaTotalFiles,
                tfaDatabaseSizeMB = snapshot.TfaDatabaseSizeMB,
                tfaFileSizeTodayMB = snapshot.TfaFileSizeTodayMB,
                tfaLicenseCount = snapshot.TfaLicenseCount,
                tfaLicenseEnd = snapshot.TfaLicenseEnd?.ToString("dd.MM.yyyy"),
                tfaTFLogErrorsToday = snapshot.TfaTFLogErrorsToday,
                tf_M365_Mails_today = snapshot.TF_M365_Mails_today,

                tfaSQLServerVersion = snapshot.TfaSQLServerVersion,
                tfaDBVersion = snapshot.TfaDBVersion,
                tfaJobServerVersion = snapshot.TfaJobServerVersion,
                tfaOcrServerVersion = snapshot.TfaOcrServerVersion,
                tfaImageServerVersion = snapshot.TfaImageServerVersion,
                tfaMyWorkVersion = snapshot.TfaMyWorkVersion,
                tfaAdministrationVersion = snapshot.TfaAdministrationVersion,

                tfaLastBackupDateArchiv = snapshot.TfaLastBackupDateArchiv?.ToString("dd.MM.yyyy HH:mm"),
                tfaLastBackupDateTopfact6 = snapshot.TfaLastBackupDateTopfact6?.ToString("dd.MM.yyyy HH:mm"),

                processTotal = snapshot.ProcessTotal,
                processResponding = snapshot.ProcessResponding,
                processNotResponding = snapshot.ProcessNotResponding,
                processTopCpu = snapshot.ProcessTopCpu,
                processTopMemory = snapshot.ProcessTopMemory,
                processAvgMemoryMB = snapshot.ProcessAvgMemoryMB
            },
            history = history.Select(h => new
            {
                snapshotTimeUTC = h.SnapshotTimeUTC?.ToString("dd.MM.yyyy HH:mm"),
                cpuUsagePercent = h.CpuUsagePercent,
                memoryUsagePercent = h.MemoryUsagePercent,
                diskUsagePercent = h.DiskUsagePercent,
                tfaCountToday = h.TfaCountToday,
                tfaActiveUsersWeek = h.TfaActiveUsersWeek,
                tfaTFLogErrorsToday = h.TfaTFLogErrorsToday
            }),
            alerts = alerts.Select(a => new
            {
                title = a.Title,
                message = a.Message,
                severityCode = a.SeverityCode
            })
        });
    }

    private static List<CurrentAlertItem> BuildCurrentAlerts(ServerAnalyticsRow? status, ServersSnaphot snapshot)
    {
        var alerts = new List<CurrentAlertItem>();

        if (status is null)
            return alerts;

        AddAlert(alerts, status.VersionStatusCode, "Versionen",
            BuildVersionMessage(snapshot));

        AddAlert(alerts, status.DiskStatusCode, "Festplatte",
            snapshot.DiskUsagePercent.HasValue
                ? $"Festplattenauslastung aktuell bei {snapshot.DiskUsagePercent.Value:F1}%."
                : "Auffälligkeit bei der Festplattenauslastung erkannt.");

        AddAlert(alerts, status.CpuStatusCode, "CPU",
            snapshot.CpuUsagePercent.HasValue
                ? $"CPU-Auslastung aktuell bei {snapshot.CpuUsagePercent.Value:F1}%."
                : "Auffälligkeit bei der CPU-Auslastung erkannt.");

        AddAlert(alerts, status.ActiveUsersStatusCode, "Aktive Benutzer",
            snapshot.TfaActiveUsersWeek.HasValue
                ? $"Aktive Benutzer in den letzten 7 Tagen: {snapshot.TfaActiveUsersWeek.Value:N0}."
                : "Auffälligkeit bei den aktiven Benutzern erkannt.");

        AddAlert(alerts, status.EmailDailyStatusCode, "M365 Mail",
            snapshot.TF_M365_Mails_today.HasValue
                ? $"Verarbeitete M365-Mails heute: {snapshot.TF_M365_Mails_today.Value:N0}."
                : "Auffälligkeit bei der täglichen Mail-Verarbeitung erkannt.");

        AddAlert(alerts, status.BackupStatusCode, "Backup",
            BuildBackupMessage(snapshot));

        return alerts
            .OrderByDescending(x => x.SeverityCode)
            .ThenBy(x => x.Title)
            .ToList();
    }

    private static void AddAlert(List<CurrentAlertItem> alerts, int statusCode, string title, string message)
    {
        if (statusCode is not 1 and not 2)
            return;

        alerts.Add(new CurrentAlertItem
        {
            Title = title,
            Message = message,
            SeverityCode = statusCode
        });
    }

    private static string BuildVersionMessage(ServersSnaphot snapshot)
    {
        var versions = new List<string>();

        AddVersion(versions, "DB", snapshot.TfaDBVersion);
        AddVersion(versions, "JobServer", snapshot.TfaJobServerVersion);
        AddVersion(versions, "OCR", snapshot.TfaOcrServerVersion);
        AddVersion(versions, "Image", snapshot.TfaImageServerVersion);
        AddVersion(versions, "MyWork", snapshot.TfaMyWorkVersion);
        AddVersion(versions, "Administration", snapshot.TfaAdministrationVersion);

        return versions.Count > 0
            ? $"Auffällige Versionsprüfung erkannt. Aktuelle Stände: {string.Join(", ", versions)}."
            : "Auffälligkeit bei den installierten Versionen erkannt.";
    }

    private static void AddVersion(List<string> versions, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            versions.Add($"{label} {value}");
        }
    }

    private static string BuildBackupMessage(ServersSnaphot snapshot)
    {
        var backupInfos = new List<string>();

        if (snapshot.TfaLastBackupDateArchiv.HasValue)
        {
            backupInfos.Add($"Archiv: {snapshot.TfaLastBackupDateArchiv.Value:dd.MM.yyyy HH:mm}");
        }

        if (snapshot.TfaLastBackupDateTopfact6.HasValue)
        {
            backupInfos.Add($"topfact6: {snapshot.TfaLastBackupDateTopfact6.Value:dd.MM.yyyy HH:mm}");
        }

        return backupInfos.Count > 0
            ? $"Backup-Prüfung auffällig. Letzte Sicherungen: {string.Join(" | ", backupInfos)}."
            : "Auffälligkeit bei den letzten Backups erkannt.";
    }

    [HttpGet]
    public async Task<IActionResult> Settings(int? editModeId, int? editCategoryId, int? editItemId)
    {
        var vm = await BuildNavigationSettingsViewModelAsync(editModeId, editCategoryId, editItemId);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveMode([Bind(Prefix = "ModeForm")] ModeFormModel form)
    {
        if (!ModelState.IsValid)
        {
            var invalidVm = await BuildNavigationSettingsViewModelAsync(form.UiModeId > 0 ? form.UiModeId : null, null, null);
            invalidVm.ModeForm = form;
            return View("Settings", invalidVm);
        }

        var mode = form.UiModeId > 0
            ? await _context.Nav_Abteilungen.FirstOrDefaultAsync(x => x.UiModeId == form.UiModeId)
            : null;

        if (mode is null)
        {
            mode = new Nav_Abteilung();
            _context.Nav_Abteilungen.Add(mode);
        }

        mode.Code = await GenerateUniqueModeCodeAsync(form.DisplayName, mode.UiModeId);
        mode.DisplayName = form.DisplayName.Trim();
        mode.SortOrder = form.SortOrder;
        mode.IsActive = form.IsActive;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMode(
        int uiModeId,
        [FromForm(Name = "ModeForm.UiModeId")] int modeFormUiModeId)
    {
        if (uiModeId <= 0)
            uiModeId = modeFormUiModeId;

        var mode = await _context.Nav_Abteilungen.FirstOrDefaultAsync(x => x.UiModeId == uiModeId);
        if (mode is null)
            return RedirectToAction(nameof(Settings));

        if (IsProtectedMode(mode))
            return RedirectToAction(nameof(Settings));

        _context.Nav_Abteilungen.Remove(mode);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory([Bind(Prefix = "CategoryForm")] CategoryFormModel form)
    {
        if (!ModelState.IsValid)
        {
            var invalidVm = await BuildNavigationSettingsViewModelAsync(null, form.UiCategoryId > 0 ? form.UiCategoryId : null, null);
            invalidVm.CategoryForm = form;
            return View("Settings", invalidVm);
        }

        var modeExists = await _context.Nav_Abteilungen.AnyAsync(x => x.UiModeId == form.UiModeId);
        if (!modeExists)
        {
            ModelState.AddModelError(nameof(form.UiModeId), "Bitte eine gültige Abteilung auswählen.");
            var invalidVm = await BuildNavigationSettingsViewModelAsync(null, form.UiCategoryId > 0 ? form.UiCategoryId : null, null);
            invalidVm.CategoryForm = form;
            return View("Settings", invalidVm);
        }

        var category = form.UiCategoryId > 0
            ? await _context.Nav_Kategorien.FirstOrDefaultAsync(x => x.UiCategoryId == form.UiCategoryId)
            : null;

        if (category is null)
        {
            category = new Nav_Kategorie();
            _context.Nav_Kategorien.Add(category);
        }

        category.UiModeId = form.UiModeId;
        category.Code = await GenerateUniqueCategoryCodeAsync(form.UiModeId, form.DisplayName, category.UiCategoryId);
        category.DisplayName = form.DisplayName.Trim();
        category.SortOrder = form.SortOrder;
        category.IsCollapsible = form.IsCollapsible;
        category.IsExpandedDefault = form.IsExpandedDefault;
        category.IsActive = form.IsActive;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(
        int uiCategoryId,
        [FromForm(Name = "CategoryForm.UiCategoryId")] int categoryFormUiCategoryId)
    {
        if (uiCategoryId <= 0)
            uiCategoryId = categoryFormUiCategoryId;

        var category = await _context.Nav_Kategorien.FirstOrDefaultAsync(x => x.UiCategoryId == uiCategoryId);
        if (category is null)
            return RedirectToAction(nameof(Settings));

        _context.Nav_Kategorien.Remove(category);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveItem([Bind(Prefix = "ItemForm")] ItemFormModel form)
    {
        if (!ModelState.IsValid)
        {
            var invalidVm = await BuildNavigationSettingsViewModelAsync(null, null, form.UiNavItemId > 0 ? form.UiNavItemId : null);
            invalidVm.ItemForm = form;
            return View("Settings", invalidVm);
        }

        var selectedCategory = await _context.Nav_Kategorien
            .Include(x => x.Mode)
            .FirstOrDefaultAsync(x => x.UiCategoryId == form.UiCategoryId);

        if (selectedCategory is null)
        {
            ModelState.AddModelError(nameof(form.UiCategoryId), "Bitte eine gültige Kategorie auswählen.");
            var invalidVm = await BuildNavigationSettingsViewModelAsync(null, null, form.UiNavItemId > 0 ? form.UiNavItemId : null);
            invalidVm.ItemForm = form;
            return View("Settings", invalidVm);
        }

        var item = form.UiNavItemId > 0
            ? await _context.Nav_KategorieSeiten.FirstOrDefaultAsync(x => x.UiNavItemId == form.UiNavItemId)
            : null;

        if (item is null)
        {
            item = new Nav_KategorieSeite();
            _context.Nav_KategorieSeiten.Add(item);
            item.IconCss = "ri-file-list-3-line";
            item.Controller = "Placeholder";
            item.Action = "Placeholder";
        }

        item.UiCategoryId = form.UiCategoryId;
        item.Title = form.Title.Trim();
        item.RouteBereich = selectedCategory.Mode?.Code;
        item.SortOrder = form.SortOrder;

        var (controller, action) = ResolveNavigationTarget(item.Title, item.Controller, item.Action);
        item.Controller = controller;
        item.Action = action;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(
        int uiNavItemId,
        [FromForm(Name = "ItemForm.UiNavItemId")] int itemFormUiNavItemId)
    {
        if (uiNavItemId <= 0)
            uiNavItemId = itemFormUiNavItemId;

        var item = await _context.Nav_KategorieSeiten.FirstOrDefaultAsync(x => x.UiNavItemId == uiNavItemId);
        if (item is null)
            return RedirectToAction(nameof(Settings));

        _context.Nav_KategorieSeiten.Remove(item);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Settings));
    }

    private async Task<NavigationSettingsViewModel> BuildNavigationSettingsViewModelAsync(int? editModeId, int? editCategoryId, int? editItemId)
    {
        var modes = await _context.Nav_Abteilungen
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .ToListAsync();

        var categories = await _context.Nav_Kategorien
            .AsNoTracking()
            .Include(x => x.Mode)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .ToListAsync();

        var items = await _context.Nav_KategorieSeiten
            .AsNoTracking()
            .Include(x => x.Category)
            .ThenInclude(x => x!.Mode)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .ToListAsync();

        var vm = new NavigationSettingsViewModel
        {
            Modes = modes,
            Categories = categories,
            Items = items
        };

        if (editModeId.HasValue)
        {
            var mode = modes.FirstOrDefault(x => x.UiModeId == editModeId.Value);
            if (mode is not null)
            {
                vm.ModeForm = new ModeFormModel
                {
                    UiModeId = mode.UiModeId,
                    DisplayName = mode.DisplayName,
                    SortOrder = mode.SortOrder,
                    IsActive = mode.IsActive
                };
            }
        }

        if (editCategoryId.HasValue)
        {
            var category = categories.FirstOrDefault(x => x.UiCategoryId == editCategoryId.Value);
            if (category is not null)
            {
                vm.CategoryForm = new CategoryFormModel
                {
                    UiCategoryId = category.UiCategoryId,
                    UiModeId = category.UiModeId,
                    DisplayName = category.DisplayName,
                    SortOrder = category.SortOrder,
                    IsCollapsible = category.IsCollapsible,
                    IsExpandedDefault = category.IsExpandedDefault,
                    IsActive = category.IsActive
                };
            }
        }

        if (editItemId.HasValue)
        {
            var item = items.FirstOrDefault(x => x.UiNavItemId == editItemId.Value);
            if (item is not null)
            {
                vm.ItemForm = new ItemFormModel
                {
                    UiNavItemId = item.UiNavItemId,
                    UiCategoryId = item.UiCategoryId,
                    Title = item.Title,
                    SortOrder = item.SortOrder
                };
            }
        }

        return vm;
    }

    public IActionResult Error()
    {
        return View();
    }

    private static string GenerateCode(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "neu";

        var cleaned = new string(input.Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray());

        while (cleaned.Contains("--"))
            cleaned = cleaned.Replace("--", "-");

        cleaned = cleaned.Trim('-');
        return string.IsNullOrWhiteSpace(cleaned) ? "neu" : cleaned;
    }

    private async Task<string> GenerateUniqueModeCodeAsync(string? displayName, int currentModeId)
    {
        var baseCode = GenerateCode(displayName);
        var code = baseCode;
        var index = 2;

        while (await _context.Nav_Abteilungen
            .AnyAsync(x => x.UiModeId != currentModeId && x.Code == code))
        {
            code = $"{baseCode}-{index++}";
        }

        return code;
    }

    private async Task<string> GenerateUniqueCategoryCodeAsync(int uiModeId, string? displayName, int currentCategoryId)
    {
        var baseCode = GenerateCode(displayName);
        var code = baseCode;
        var index = 2;

        while (await _context.Nav_Kategorien
            .AnyAsync(x => x.UiCategoryId != currentCategoryId && x.UiModeId == uiModeId && x.Code == code))
        {
            code = $"{baseCode}-{index++}";
        }

        return code;
    }

    private static (string Controller, string Action) ResolveNavigationTarget(string? title, string? currentController, string? currentAction)
    {
        var key = NormalizeNavigationKey(title);
        var routes = new Dictionary<string, (string Controller, string Action)>(StringComparer.OrdinalIgnoreCase)
        {
            [NormalizeNavigationKey("Dashboard")] = ("Home", "Dashboard"),
            [NormalizeNavigationKey("Leitstand")] = ("Home", "Index"),
            [NormalizeNavigationKey("User Logins")] = ("UserLogins", "Index"),
            [NormalizeNavigationKey("Speicherplatz")] = ("Monitor", "Speicherplatz"),
            [NormalizeNavigationKey("FormRec")] = ("Monitor", "FormRecognizer"),
            [NormalizeNavigationKey("M365")] = ("Monitor", "M365"),
            [NormalizeNavigationKey("Lizenzen")] = ("Monitor", "Licenses"),
            [NormalizeNavigationKey("topfact6 MyWork Cloud")] = ("Monitor", "MyWorkCloud"),
            [NormalizeNavigationKey("topfact6 MyWork")] = ("Monitor", "topfact6 MyWork"),
            [NormalizeNavigationKey("topfact MyWork App")] = ("Monitor", "topfact MyWork App"),
            [NormalizeNavigationKey("topfact6 BestPeople")] = ("Monitor", "topfact6 BestPeople"),
            [NormalizeNavigationKey("topfact6 Facility")] = ("Monitor", "topfact6 Facility"),
            [NormalizeNavigationKey("Projekt-Controlling")] = ("Monitor", "ProjektControlling"),
            [NormalizeNavigationKey("Geplante Funktionen")] = ("UserLogins", "GeplanteFunktionen"),
            [NormalizeNavigationKey("Geplante Leistungen")] = ("UserLogins", "GeplanteLeistungen"),
            [NormalizeNavigationKey("IT-Sicherheitsorganisation")] = ("ITSicherheit", "ITSicherheitsorganisation"),
            [NormalizeNavigationKey("GitHub Controlling")] = ("GitHubControlling", "GitHubControlling")
        };

        if (routes.TryGetValue(key, out var route))
            return route;

        if (!string.IsNullOrWhiteSpace(currentController) && !string.IsNullOrWhiteSpace(currentAction))
            return (currentController, currentAction);

        return ("Placeholder", "Placeholder");
    }

    private static string NormalizeNavigationKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var chars = value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray();
        return new string(chars);
    }

    private static bool IsProtectedMode(Nav_Abteilung mode)
    {
        return IsProtectedModeName(mode.Code)
            || IsProtectedModeName(mode.DisplayName);
    }

    private static bool IsProtectedModeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return string.Equals(value.Trim(), "Technik", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value.Trim(), "Organisation", StringComparison.OrdinalIgnoreCase);
    }
}
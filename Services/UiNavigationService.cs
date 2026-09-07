using Microsoft.EntityFrameworkCore;
using topfact.Pulse.Data;
using topfact.Pulse.Models;

namespace topfact.Pulse.Services
{
    public interface IUiNavigationService
    {
        Task<IReadOnlyList<UiModeDto>> GetModesAsync();
        Task<IReadOnlyList<UiCategoryDto>> GetCategoriesAsync();
        Task<UiModeDto?> GetModeByCodeAsync(string modeCode);
    }

    public sealed class UiNavigationService : IUiNavigationService
    {
        private readonly AppDbContext _dbContext;

        public UiNavigationService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<UiModeDto>> GetModesAsync()
        {
            IReadOnlyList<UiModeDto> modes;

            try
            {
                modes = await _dbContext.Nav_Abteilungen
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder)
                    .Select(x => new UiModeDto(
                        x.Code,
                        x.DisplayName,
                        x.SortOrder,
                        x.RequiredRole,
                        x.RequiredClaimType,
                        x.RequiredClaimValue))
                    .ToListAsync();
            }
            catch
            {
                modes = Array.Empty<UiModeDto>();
            }

            if (modes.Count == 0)
            {
                modes = GetFallbackModes();
            }

            return modes;
        }

        public async Task<IReadOnlyList<UiCategoryDto>> GetCategoriesAsync()
        {
            IReadOnlyList<UiCategoryDto> categories;

            try
            {
                categories = await _dbContext.Nav_Kategorien
                    .AsNoTracking()
                    .Include(x => x.Mode)
                    .Include(x => x.Items.Where(i => i.IsActive))
                    .Where(x => x.IsActive && x.Mode != null && x.Mode.IsActive)
                    .OrderBy(x => x.Mode!.SortOrder)
                    .ThenBy(x => x.SortOrder)
                    .Select(x => new UiCategoryDto(
                        x.Mode!.Code,
                        x.Code,
                        x.DisplayName,
                        x.SortOrder,
                        x.IsCollapsible,
                        x.IsExpandedDefault,
                        x.Items
                            .OrderBy(i => i.SortOrder)
                            .Select(i => new UiNavItemDto(
                                i.Title,
                                i.IconCss,
                                i.Controller,
                                i.Action,
                                i.RouteBereich,
                                i.BadgeText,
                                i.SortOrder))
                            .ToList()))
                    .ToListAsync();
            }
            catch
            {
                categories = Array.Empty<UiCategoryDto>();
            }

            if (categories.Count == 0)
            {
                categories = GetFallbackCategories();
            }

            categories = EnsureSettingsLinks(categories);

            return categories;
        }

        private static IReadOnlyList<UiCategoryDto> EnsureSettingsLinks(IReadOnlyList<UiCategoryDto> categories)
        {
            var result = categories.ToList();

            AddSettingsLinkForMode(result, "Technik");
            AddSettingsLinkForMode(result, "Organisation");

            return result;
        }

        private static void AddSettingsLinkForMode(List<UiCategoryDto> categories, string modeCode)
        {
            var settingsItem = new UiNavItemDto(
                "Navigation",
                "ri-settings-3-line",
                "Home",
                "Settings",
                modeCode,
                null,
                999);

            var targetIndex = categories.FindIndex(x =>
                string.Equals(x.ModeCode, modeCode, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Code, "General", StringComparison.OrdinalIgnoreCase));

            if (targetIndex < 0)
            {
                targetIndex = categories.FindIndex(x =>
                    string.Equals(x.ModeCode, modeCode, StringComparison.OrdinalIgnoreCase));
            }

            if (targetIndex < 0)
            {
                categories.Add(new UiCategoryDto(
                    modeCode,
                    "General",
                    "Allgemein",
                    999,
                    true,
                    true,
                    [settingsItem]));
                return;
            }

            var target = categories[targetIndex];
            var exists = target.Items.Any(i =>
                string.Equals(i.Controller, "Home", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(i.Action, "Settings", StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                return;
            }

            var updatedItems = target.Items
                .Append(settingsItem)
                .OrderBy(i => i.SortOrder)
                .ToList();

            categories[targetIndex] = target with { Items = updatedItems };
        }

        public async Task<UiModeDto?> GetModeByCodeAsync(string modeCode)
        {
            if (string.IsNullOrWhiteSpace(modeCode))
                return null;

            var modes = await GetModesAsync();
            return modes.FirstOrDefault(x => string.Equals(x.Code, modeCode, StringComparison.OrdinalIgnoreCase));
        }

        private static IReadOnlyList<UiModeDto> GetFallbackModes() =>
        [
            new UiModeDto("Technik", "Technik", 10, null, null, null),
            new UiModeDto("Organisation", "Organisation", 20, null, null, null)
        ];

        private static IReadOnlyList<UiCategoryDto> GetFallbackCategories() =>
        [
            new UiCategoryDto("Technik", "General", "Allgemein", 10, true, true,
            [
                new UiNavItemDto("Dashboard", "ri-dashboard-line", "Home", "Dashboard", "Technik", null, 10),
                new UiNavItemDto("Leitstand", "ri-home-4-line", "Home", "Index", "Technik", null, 20)
            ]),
            new UiCategoryDto("Technik", "Insights", "Insights", 20, true, true,
            [
                new UiNavItemDto("User Logins", "ri-group-line", "UserLogins", "Index", "Technik", "New", 10)
            ]),
            new UiCategoryDto("Technik", "Verbrauch", "Verbrauchswerte", 30, true, true,
            [
                new UiNavItemDto("Speicherplatz", "ri-server-line", "Monitor", "Speicherplatz", "Technik", null, 10),
                new UiNavItemDto("FormRec", "ri-file-search-line", "Monitor", "FormRecognizer", "Technik", null, 20),
                new UiNavItemDto("M365", "ri-mail-line", "Monitor", "M365", "Technik", null, 30),
                new UiNavItemDto("Lizenzen", "ri-key-2-line", "Monitor", "Licenses", "Technik", null, 40)
            ]),
            new UiCategoryDto("Technik", "Apps", "Anwendungen", 40, true, true,
            [
                new UiNavItemDto("topfact6 MyWork Cloud", "ri-cloud-line", "Monitor", "MyWorkCloud", "Technik", null, 10),
                new UiNavItemDto("topfact6 MyWork", "ri-pulse-line", "Monitor", "topfact6 MyWork", "Technik", null, 20),
                new UiNavItemDto("topfact MyWork App", "ri-apps-line", "Monitor", "topfact MyWork App", "Technik", null, 30),
                new UiNavItemDto("topfact6 BestPeople", "ri-group-line", "Monitor", "topfact6 BestPeople", "Technik", null, 40),
                new UiNavItemDto("topfact6 Facility", "ri-building-4-line", "Monitor", "topfact6 Facility", "Technik", null, 50)
            ]),
            new UiCategoryDto("Organisation", "General", "Allgemein", 10, true, true,
            [
                new UiNavItemDto("Dashboard", "ri-dashboard-line", "Home", "Dashboard", "Organisation", null, 10),
                new UiNavItemDto("Leitstand", "ri-home-4-line", "Home", "Index", "Organisation", null, 20)
            ]),
            new UiCategoryDto("Organisation", "Controlling", "Management-Übersicht", 20, true, true,
            [
                new UiNavItemDto("Projekt-Controlling", "ri-line-chart-line", "Monitor", "ProjektControlling", "Organisation", null, 10),
                new UiNavItemDto("Geplante Funktionen", "ri-calendar-check-line", "UserLogins", "GeplanteFunktionen", "Organisation", null, 20),
                new UiNavItemDto("GitHub Controlling", "ri-github-line", "GitHubControlling", "GitHubControlling", "Organisation", null, 30)
            ])
        ];
    }

    public sealed record UiModeDto(
        string Code,
        string DisplayName,
        int SortOrder,
        string? RequiredRole,
        string? RequiredClaimType,
        string? RequiredClaimValue);

    public sealed record UiCategoryDto(
        string ModeCode,
        string Code,
        string DisplayName,
        int SortOrder,
        bool IsCollapsible,
        bool IsExpandedDefault,
        IReadOnlyList<UiNavItemDto> Items);

    public sealed record UiNavItemDto(
        string Title,
        string IconCss,
        string Controller,
        string Action,
        string? RouteBereich,
        string? BadgeText,
        int SortOrder);
}

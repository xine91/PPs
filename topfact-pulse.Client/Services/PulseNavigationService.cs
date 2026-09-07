using Microsoft.EntityFrameworkCore;
using topfact.Pulse.Data;
using topfact.Pulse.Models;

namespace topfact.Pulse.Services
{
    /// <summary>Service für Navigation und Menü-Verwaltung basierend auf neuen Pulse-Tabellen</summary>
    public interface IPulseNavigationService
    {
        Task<IEnumerable<PulseBereich>> GetBereicheAsync(bool activeOnly = true);
        Task<PulseBereich?> GetBereichByCodeAsync(string code);
        Task<PulseBereich?> GetBereichByIdAsync(int bereichId);
        Task<IEnumerable<PulseGruppe>> GetGruppenByBereichAsync(int bereichId, bool activeOnly = true);
        Task<PulseGruppe?> GetGruppeByIdAsync(int gruppeId);
        Task<IEnumerable<PulseKategorie>> GetKategorienByGruppeAsync(int gruppeId, bool activeOnly = true);
        Task<PulseKategorie?> GetKategorieByIdAsync(int kategorieId);
        Task<NavigationTreeDto> GetNavigationTreeAsync(string? bereichCode = null);
        Task<NavigationTreeDto> GetNavigationTreeForUserAsync(string username, string? bereichCode = null);
    }

    public class PulseNavigationService : IPulseNavigationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PulseNavigationService> _logger;

        public PulseNavigationService(AppDbContext context, ILogger<PulseNavigationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>Alle Bereiche abrufen</summary>
        public async Task<IEnumerable<PulseBereich>> GetBereicheAsync(bool activeOnly = true)
        {
            var query = _context.PulseBereiche.AsQueryable();

            if (activeOnly)
                query = query.Where(x => x.IsActive);

            return await query
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.DisplayName)
                .ToListAsync();
        }

        /// <summary>Bereich nach Code abrufen</summary>
        public async Task<PulseBereich?> GetBereichByCodeAsync(string code)
        {
            return await _context.PulseBereiche
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == code && x.IsActive);
        }

        /// <summary>Bereich nach ID abrufen</summary>
        public async Task<PulseBereich?> GetBereichByIdAsync(int bereichId)
        {
            return await _context.PulseBereiche
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BereichID == bereichId && x.IsActive);
        }

        /// <summary>Gruppen eines Bereichs abrufen</summary>
        public async Task<IEnumerable<PulseGruppe>> GetGruppenByBereichAsync(int bereichId, bool activeOnly = true)
        {
            var query = _context.PulseGruppen
                .AsNoTracking()
                .Where(x => x.BereichID == bereichId);

            if (activeOnly)
                query = query.Where(x => x.IsActive);

            return await query
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.DisplayName)
                .ToListAsync();
        }

        /// <summary>Gruppe nach ID abrufen</summary>
        public async Task<PulseGruppe?> GetGruppeByIdAsync(int gruppeId)
        {
            return await _context.PulseGruppen
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.GruppeID == gruppeId && x.IsActive);
        }

        /// <summary>Kategorien einer Gruppe abrufen</summary>
        public async Task<IEnumerable<PulseKategorie>> GetKategorienByGruppeAsync(int gruppeId, bool activeOnly = true)
        {
            var query = _context.PulseKategorien
                .AsNoTracking()
                .Where(x => x.GruppeID == gruppeId);

            if (activeOnly)
                query = query.Where(x => x.IsActive);

            return await query
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Title)
                .ToListAsync();
        }

        /// <summary>Kategorie nach ID abrufen</summary>
        public async Task<PulseKategorie?> GetKategorieByIdAsync(int kategorieId)
        {
            return await _context.PulseKategorien
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.KategorieID == kategorieId && x.IsActive);
        }

        /// <summary>Kompletten Navigationsbaum abrufen (optional gefiltert nach Bereich)</summary>
        public async Task<NavigationTreeDto> GetNavigationTreeAsync(string? bereichCode = null)
        {
            try
            {
                _logger.LogInformation("=== PulseNavigationService.GetNavigationTreeAsync START ===");
                _logger.LogInformation("Bereich-Filter: '{Bereich}'", bereichCode ?? "(keiner)");

                // ─── BEREICHE ─────────────────────────────────────
                var bereiche = await _context.PulseBereiche
                    .AsNoTracking()
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.DisplayName)
                    .ToListAsync();

                _logger.LogInformation("Bereiche aus DB: {Count}", bereiche.Count);
                foreach (var b in bereiche)
                {
                    _logger.LogInformation("  → Bereich ID={Id}, Code={Code}, Display={Display}, Active={Active}, RequiredRole={Role}",
                        b.BereichID, b.Code, b.DisplayName, b.IsActive, b.RequiredRole ?? "(null)");
                }

                if (!string.IsNullOrWhiteSpace(bereichCode))
                    bereiche = bereiche.Where(x => x.Code == bereichCode).ToList();

                if (bereiche.Count == 0)
                {
                    _logger.LogWarning("KEINE Bereiche gefunden – Fallback wird verwendet.");
                    return GetFallbackNavigationTree();
                }

                var tree = new NavigationTreeDto();

                foreach (var bereich in bereiche)
                {
                    // ─── GRUPPEN ─────────────────────────────────
                    var gruppen = await _context.PulseGruppen
                        .AsNoTracking()
                        .Where(x => x.BereichID == bereich.BereichID)
                        .OrderBy(x => x.SortOrder)
                        .ThenBy(x => x.DisplayName)
                        .ToListAsync();

                    _logger.LogInformation("Bereich '{Code}' ({Id}): {Count} Gruppe(n)", bereich.Code, bereich.BereichID, gruppen.Count);
                    foreach (var g in gruppen)
                    {
                        _logger.LogInformation("    → Gruppe ID={Id}, Code={Code}, Display={Display}, Active={Active}",
                            g.GruppeID, g.Code, g.DisplayName, g.IsActive);
                    }

                    var bereichDto = new BereichDto
                    {
                        BereichID = bereich.BereichID,
                        Code = bereich.Code,
                        DisplayName = bereich.DisplayName,
                        SortOrder = bereich.SortOrder,
                        RequiredRole = bereich.RequiredRole,
                        Gruppen = new List<GruppeDto>()
                    };

                    foreach (var gruppe in gruppen)
                    {
                        // ─── KATEGORIEN ─────────────────────────
                        var kategorien = await _context.PulseKategorien
                            .AsNoTracking()
                            .Where(x => x.GruppeID == gruppe.GruppeID)
                            .OrderBy(x => x.SortOrder)
                            .ThenBy(x => x.Title)
                            .ToListAsync();

                        _logger.LogInformation("    Gruppe '{Code}' ({Id}): {Count} Kategorie(n)", gruppe.Code, gruppe.GruppeID, kategorien.Count);
                        foreach (var k in kategorien)
                        {
                            _logger.LogInformation("      → Kategorie ID={Id}, Title={Title}, Active={Active}",
                                k.KategorieID, k.Title, k.IsActive);
                        }

                        var gruppeDto = new GruppeDto
                        {
                            GruppeID = gruppe.GruppeID,
                            BereichID = gruppe.BereichID,
                            Code = gruppe.Code,
                            DisplayName = gruppe.DisplayName,
                            SortOrder = gruppe.SortOrder,
                            IsCollapsible = gruppe.IsCollapsible,
                            IsExpandedDefault = gruppe.IsExpandedDefault,
                            Kategorien = kategorien.Select(k => new KategorieDto
                            {
                                KategorieID = k.KategorieID,
                                GruppeID = k.GruppeID,
                                Title = k.Title,
                                IconCss = k.IconCss,
                                Sql_query = k.Sql_query,
                                RouteBereich = k.RouteBereich,
                                BadgeText = k.BadgeText,
                                SortOrder = k.SortOrder
                            }).OrderBy(x => x.SortOrder).ToList()
                        };

                        bereichDto.Gruppen.Add(gruppeDto);
                    }

                    tree.Bereiche.Add(bereichDto);
                }

                if (tree.Bereiche.All(b => b.Gruppen.Count == 0))
                {
                    _logger.LogWarning("Bereiche vorhanden, aber KEINE Gruppen → Fallback wird verwendet.");
                    return GetFallbackNavigationTree();
                }

                _logger.LogInformation("=== PulseNavigationService.GetNavigationTreeAsync OK: {BereichCount} Bereiche ===",
                    tree.Bereiche.Count);
                return tree;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PulseNavigationService: DB-Fehler beim Laden der Navigation – Fallback wird verwendet.");
                Console.Error.WriteLine($"[PulseNavigationService] DB-Fehler: {ex.Message}");
                return GetFallbackNavigationTree();
            }
        }

        /// <summary>Navigation-Baum für einen spezifischen Benutzer basierend auf Pulse_Access</summary>
        public async Task<NavigationTreeDto> GetNavigationTreeForUserAsync(string username, string? bereichCode = null)
        {
            try
            {
                _logger.LogInformation("=== PulseNavigationService.GetNavigationTreeForUserAsync START ===");
                _logger.LogInformation("Username: '{Username}', Bereich-Filter: '{Bereich}'", username, bereichCode ?? "(keiner)");

                // ---- Hole alle Access-Records für diesen Benutzer (nur aktive)
                var userAccess = await _context.PulseAccess
                    .AsNoTracking()
                    .Where(x => x.Username == username && x.IsActive)
                    .ToListAsync();

                _logger.LogInformation("User '{Username}' hat {Count} Pulse_Access-Records", username, userAccess.Count);

                if (userAccess.Count == 0)
                {
                    _logger.LogWarning("Kein Pulse_Access-Record für User '{Username}' gefunden", username);
                    return GetFallbackNavigationTree();
                }

                // ---- Bestimme, welche BereichIDs der Benutzer sehen darf
                var accessibleBereichIds = userAccess
                    .Where(x => x.BereichID.HasValue)
                    .Select(x => x.BereichID.Value)
                    .Distinct()
                    .ToList();

                var hasGlobalBereichAccess = userAccess.Any(x => x.BereichID == null);

                _logger.LogInformation("User '{Username}' hat Zugriff auf Bereiche: {Bereiche} | Global-Access: {GlobalAccess}",
                    username, string.Join(",", accessibleBereichIds), hasGlobalBereichAccess);

                // ---- Lade alle Bereiche (oder gefiltert nach bereichCode)
                var bereichQuery = _context.PulseBereiche.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(bereichCode))
                    bereichQuery = bereichQuery.Where(x => x.Code == bereichCode);

                if (!hasGlobalBereichAccess)
                    bereichQuery = bereichQuery.Where(x => accessibleBereichIds.Contains(x.BereichID));

                var bereiche = await bereichQuery
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.DisplayName)
                    .ToListAsync();

                _logger.LogInformation("Nach Access-Filter: {Count} Bereiche für User '{Username}'", bereiche.Count, username);

                if (bereiche.Count == 0)
                {
                    _logger.LogWarning("Nach Access-Filter: KEINE Bereiche für User '{Username}'", username);
                    return GetFallbackNavigationTree();
                }

                var tree = new NavigationTreeDto();

                foreach (var bereich in bereiche)
                {
                    // ---- Bestimme, welche GruppenIDs in diesem Bereich der Benutzer sehen darf
                    var accessInBereich = userAccess.Where(x => x.BereichID == null || x.BereichID == bereich.BereichID).ToList();

                    var accessibleGruppenIds = accessInBereich
                        .Where(x => x.GruppeID.HasValue)
                        .Select(x => x.GruppeID.Value)
                        .Distinct()
                        .ToList();

                    var hasGlobalGruppenAccess = accessInBereich.Any(x => x.GruppeID == null);

                    _logger.LogInformation("Bereich '{Code}' (ID={Id}): Zugängliche Gruppen: {GruppenIds} | Global-Gruppen-Access: {GlobalAccess}",
                        bereich.Code, bereich.BereichID, string.Join(",", accessibleGruppenIds), hasGlobalGruppenAccess);

                    // ---- Lade Gruppen für diesen Bereich
                    var gruppenQuery = _context.PulseGruppen
                        .AsNoTracking()
                        .Where(x => x.BereichID == bereich.BereichID);

                    if (!hasGlobalGruppenAccess)
                        gruppenQuery = gruppenQuery.Where(x => accessibleGruppenIds.Contains(x.GruppeID));

                    var gruppen = await gruppenQuery
                        .OrderBy(x => x.SortOrder)
                        .ThenBy(x => x.DisplayName)
                        .ToListAsync();

                    _logger.LogInformation("Bereich '{Code}': {Count} Gruppen nach Filter", bereich.Code, gruppen.Count);

                    var bereichDto = new BereichDto
                    {
                        BereichID = bereich.BereichID,
                        Code = bereich.Code,
                        DisplayName = bereich.DisplayName,
                        SortOrder = bereich.SortOrder,
                        RequiredRole = bereich.RequiredRole,
                        Gruppen = new List<GruppeDto>()
                    };

                    foreach (var gruppe in gruppen)
                    {
                        // ---- Lade Kategorien für diese Gruppe
                        var kategorien = await _context.PulseKategorien
                            .AsNoTracking()
                            .Where(x => x.GruppeID == gruppe.GruppeID)
                            .OrderBy(x => x.SortOrder)
                            .ThenBy(x => x.Title)
                            .ToListAsync();

                        var gruppeDto = new GruppeDto
                        {
                            GruppeID = gruppe.GruppeID,
                            BereichID = gruppe.BereichID,
                            Code = gruppe.Code,
                            DisplayName = gruppe.DisplayName,
                            SortOrder = gruppe.SortOrder,
                            IsCollapsible = gruppe.IsCollapsible,
                            IsExpandedDefault = gruppe.IsExpandedDefault,
                            Kategorien = kategorien.Select(k => new KategorieDto
                            {
                                KategorieID = k.KategorieID,
                                GruppeID = k.GruppeID,
                                Title = k.Title,
                                IconCss = k.IconCss,
                                Sql_query = k.Sql_query,
                                RouteBereich = k.RouteBereich,
                                BadgeText = k.BadgeText,
                                SortOrder = k.SortOrder
                            }).OrderBy(x => x.SortOrder).ToList()
                        };

                        bereichDto.Gruppen.Add(gruppeDto);
                    }

                    tree.Bereiche.Add(bereichDto);
                }

                if (tree.Bereiche.Count == 0 || tree.Bereiche.All(b => b.Gruppen.Count == 0))
                {
                    _logger.LogWarning("Nach Access-Filter: KEINE Gruppen für User '{Username}' \u2013 Fallback wird verwendet", username);
                    return GetFallbackNavigationTree();
                }

                _logger.LogInformation("=== GetNavigationTreeForUserAsync OK: User '{Username}' hat {BereichCount} Bereiche mit Gruppen ===",
                    username, tree.Bereiche.Count(b => b.Gruppen.Count > 0));
                return tree;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PulseNavigationService: Fehler bei GetNavigationTreeForUserAsync für User '{Username}'", username);
                return GetFallbackNavigationTree();
            }
        }

        /// <summary>Statische Fallback-Navigation für den Fall, dass die DB leer/fehlerhaft ist</summary>
        private static NavigationTreeDto GetFallbackNavigationTree()
        {
            var tree = new NavigationTreeDto();

            tree.Bereiche.Add(new BereichDto
            {
                BereichID = 0,
                Code = "Technik",
                DisplayName = "Technik",
                SortOrder = 1,
                RequiredRole = null,
                Gruppen = new List<GruppeDto>
                {
                    new GruppeDto
                    {
                        GruppeID = 0,
                        BereichID = 0,
                        Code = "default",
                        DisplayName = "Allgemein",
                        SortOrder = 1,
                        IsCollapsible = true,
                        IsExpandedDefault = true,
                        Kategorien = new List<KategorieDto>
                        {
                            new KategorieDto
                            {
                                KategorieID = 0,
                                GruppeID = 0,
                                Title = "Dashboard",
                                IconCss = "ri-dashboard-line",
                                SortOrder = 1
                            }
                        }
                    }
                }
            });

            tree.Bereiche.Add(new BereichDto
            {
                BereichID = 0,
                Code = "Organisation",
                DisplayName = "Organisation",
                SortOrder = 2,
                RequiredRole = null,
                Gruppen = new List<GruppeDto>()
            });

            return tree;
        }
    }

    // ========================================================================
    // DTOs für Navigation
    // ========================================================================

    public class NavigationTreeDto
    {
        public List<BereichDto> Bereiche { get; set; } = new();
    }

    public class BereichDto
    {
        public int BereichID { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public string? RequiredRole { get; set; }
        public List<GruppeDto> Gruppen { get; set; } = new();
    }

    public class GruppeDto
    {
        public int GruppeID { get; set; }
        public int BereichID { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsCollapsible { get; set; }
        public bool IsExpandedDefault { get; set; }
        public List<KategorieDto> Kategorien { get; set; } = new();
    }

    public class KategorieDto
    {
        public int KategorieID { get; set; }
        public int GruppeID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? IconCss { get; set; }
        public string? Sql_query { get; set; }
        public string? RouteBereich { get; set; }
        public string? BadgeText { get; set; }
        public int SortOrder { get; set; }
    }
}

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
    }

    public class PulseNavigationService : IPulseNavigationService
    {
        private readonly AppDbContext _context;

        public PulseNavigationService(AppDbContext context)
        {
            _context = context;
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
            var bereiche = await GetBereicheAsync(activeOnly: true);

            if (!string.IsNullOrWhiteSpace(bereichCode))
                bereiche = bereiche.Where(x => x.Code == bereichCode);

            var tree = new NavigationTreeDto();

            foreach (var bereich in bereiche)
            {
                var gruppen = await GetGruppenByBereichAsync(bereich.BereichID, activeOnly: true);
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
                    var kategorien = await GetKategorienByGruppeAsync(gruppe.GruppeID, activeOnly: true);
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

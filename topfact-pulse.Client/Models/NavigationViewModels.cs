using topfact.Pulse.Models;

namespace topfact.Pulse.Web.Controllers
{
    public class NavigationSettingsViewModel
    {
        public IEnumerable<PulseBereich> Bereiche { get; set; } = new List<PulseBereich>();
        public IEnumerable<PulseGruppe> Gruppen { get; set; } = new List<PulseGruppe>();
        public IEnumerable<PulseKategorie> Kategorien { get; set; } = new List<PulseKategorie>();

        public BereichFormModel? BereichForm { get; set; }
        public GruppeFormModel? GruppeForm { get; set; }
        public KategorieFormModel? KategorieForm { get; set; }
    }

    public class BereichFormModel
    {
        public int BereichID { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? RequiredRole { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class GruppeFormModel
    {
        public int GruppeID { get; set; }
        public int BereichID { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsCollapsible { get; set; }
        public bool IsExpandedDefault { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class KategorieFormModel
    {
        public int KategorieID { get; set; }
        public int GruppeID { get; set; }
        public string Title { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public string? Sql_query { get; set; }
    }

    // ?? Delete Models ??
    public class DeleteBereichModel
    {
        public int bereichId { get; set; }
    }

    public class DeleteGruppeModel
    {
        public int gruppeId { get; set; }
    }

    public class DeleteKategorieModel
    {
        public int kategorieId { get; set; }
    }
}

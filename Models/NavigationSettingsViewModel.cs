using System.ComponentModel.DataAnnotations;

namespace topfact.Pulse.Models
{
    public class NavigationSettingsViewModel
    {
        public List<Nav_Abteilung> Modes { get; set; } = [];
        public List<Nav_Kategorie> Categories { get; set; } = [];
        public List<Nav_KategorieSeite> Items { get; set; } = [];
        public ModeFormModel ModeForm { get; set; } = new();
        public CategoryFormModel CategoryForm { get; set; } = new();
        public ItemFormModel ItemForm { get; set; } = new();
        public List<PulseBereich> Bereiche { get; set; } = [];
        public List<PulseGruppe> Gruppen { get; set; } = [];
        public List<PulseKategorie> Kategorien { get; set; } = [];
        public BereichFormModel BereichForm { get; set; } = new();
        public GruppeFormModel GruppeForm { get; set; } = new();
        public KategorieFormModel KategorieForm { get; set; } = new();
    }

    public class ModeFormModel
    {
        public int UiModeId { get; set; }
        [Required][MaxLength(100)] public string DisplayName { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        [Display(Name = "Aktiv")] public bool IsActive { get; set; } = true;
    }

    public class CategoryFormModel
    {
        public int UiCategoryId { get; set; }
        public int UiModeId { get; set; }
        [Required][MaxLength(100)] public string DisplayName { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        [Display(Name = "Einklappbar")] public bool IsCollapsible { get; set; } = true;
        [Display(Name = "Standardmäßig geöffnet")] public bool IsExpandedDefault { get; set; } = true;
        [Display(Name = "Aktiv")] public bool IsActive { get; set; } = true;
    }

    public class ItemFormModel
    {
        public int UiNavItemId { get; set; }
        public int UiCategoryId { get; set; }
        [Required][MaxLength(120)] public string Title { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public class BereichFormModel
    {
        public int BereichID { get; set; }
        [Required][MaxLength(100)] public string DisplayName { get; set; } = string.Empty;
        public string RequiredRole { get; set; }
        public int SortOrder { get; set; }
    }

    public class GruppeFormModel
    {
        public int GruppeID { get; set; }
        public int BereichID { get; set; }
        [Required][MaxLength(100)] public string DisplayName { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        [Display(Name = "Einklappbar")] public bool IsCollapsible { get; set; } = true;
        [Display(Name = "Standardmäßig geöffnet")] public bool IsExpandedDefault { get; set; } = true;
        [Display(Name = "Aktiv")] public bool IsActive { get; set; } = true;
    }

    public class KategorieFormModel
    {
        public int KategorieID { get; set; }
        public int GruppeID { get; set; }
        [Required][MaxLength(200)] public string Title { get; set; } = string.Empty;
        public string? Sql_query { get; set; }
        public int SortOrder { get; set; }
        [Display(Name = "Aktiv")] public bool IsActive { get; set; } = true;
    }
}

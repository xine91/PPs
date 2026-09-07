using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace topfact.Pulse.Models
{
    public class NavigationSettingsViewModel
    {
        public List<PulseBereich> Bereiche { get; set; } = new();
        public List<PulseGruppe> Gruppen { get; set; } = new();
        public List<PulseKategorie> Kategorien { get; set; } = new();

        public BereichFormModel BereichForm { get; set; } = new();
        public GruppeFormModel GruppeForm { get; set; } = new();
        public KategorieFormModel KategorieForm { get; set; } = new();

        // Kompatibilitäts-Properties für Settings.cshtml (Aliase)
        public List<PulseBereich> Modes => Bereiche;
        public List<PulseGruppe> Categories => Gruppen;
        public List<PulseKategorie> Items => Kategorien;

        public ModeFormModel ModeForm { get; set; } = new();
        public CategoryFormModel CategoryForm { get; set; } = new();
        public ItemFormModel ItemForm { get; set; } = new();
    }

    public class BereichFormModel
    {
        public int BereichID { get; set; }

        [Required(ErrorMessage = "Anzeigename ist erforderlich")]
        [StringLength(100, ErrorMessage = "Maximal 100 Zeichen")]
        public string DisplayName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? RequiredRole { get; set; }

        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    public class GruppeFormModel
    {
        public int GruppeID { get; set; }

        [Required(ErrorMessage = "Bereich ist erforderlich")]
        public int BereichID { get; set; }

        [Required(ErrorMessage = "Anzeigename ist erforderlich")]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        public int SortOrder { get; set; } = 0;
        public bool IsCollapsible { get; set; } = true;
        public bool IsExpandedDefault { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }

    public class KategorieFormModel
    {
        public int KategorieID { get; set; }

        [Required(ErrorMessage = "Gruppe ist erforderlich")]
        public int GruppeID { get; set; }

        [Required(ErrorMessage = "Titel ist erforderlich")]
        [StringLength(120)]
        public string Title { get; set; } = string.Empty;

        public int SortOrder { get; set; } = 0;

        [StringLength(2000)]
        public string? Sql_query { get; set; }
    }

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

    public class ModeFormModel
    {
        public int UiModeId { get; set; }

        [Required, StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    public class CategoryFormModel
    {
        public int UiCategoryId { get; set; }
        public int UiModeId { get; set; }

        [Required, StringLength(120)]
        public string DisplayName { get; set; } = string.Empty;

        public int SortOrder { get; set; } = 0;
        public bool IsCollapsible { get; set; } = true;
        public bool IsExpandedDefault { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }

    public class ItemFormModel
    {
        public int UiNavItemId { get; set; }
        public int UiCategoryId { get; set; }

        [Required, StringLength(120)]
        public string Title { get; set; } = string.Empty;

        public int SortOrder { get; set; } = 0;
    }
}
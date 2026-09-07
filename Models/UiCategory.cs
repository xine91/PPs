using System.ComponentModel.DataAnnotations;

namespace topfact.Pulse.Models
{
    public class Nav_Kategorie
    {
        public int UiCategoryId { get; set; }

        public int UiModeId { get; set; }

        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public bool IsCollapsible { get; set; } = true;

        public bool IsExpandedDefault { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public Nav_Abteilung? Mode { get; set; }

        public ICollection<Nav_KategorieSeite> Items { get; set; } = new List<Nav_KategorieSeite>();
    }
}

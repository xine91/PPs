using System.ComponentModel.DataAnnotations;

namespace topfact.Pulse.Models
{
    public class Nav_KategorieSeite
    {
        public int UiNavItemId { get; set; }

        public int UiCategoryId { get; set; }

        [MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(80)]
        public string IconCss { get; set; } = string.Empty;

        [MaxLength(80)]
        public string Controller { get; set; } = string.Empty;

        [MaxLength(80)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? RouteBereich { get; set; }

        [MaxLength(40)]
        public string? BadgeText { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public Nav_Kategorie? Category { get; set; }
    }
}

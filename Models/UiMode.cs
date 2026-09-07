using System.ComponentModel.DataAnnotations;

namespace topfact.Pulse.Models
{
    public class Nav_Abteilung
    {
        public int UiModeId { get; set; }

        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? RequiredRole { get; set; }

        [MaxLength(100)]
        public string? RequiredClaimType { get; set; }

        [MaxLength(200)]
        public string? RequiredClaimValue { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Nav_Kategorie> Categories { get; set; } = new List<Nav_Kategorie>();
    }
}

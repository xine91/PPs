using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace topfact.Pulse.Models
{
    /// <summary>Hauptbereiche (Abteilungen) - z.B. Technik, Organisation</summary>
    [Table("Pulse_Bereich", Schema = "dbo")]
    public class PulseBereich
    {
        [Key]
        public int BereichID { get; set; }

        /// <summary>Eindeutiger Code, z.B. "Technik", "Organisation"</summary>
        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        /// <summary>Anzeigename des Bereichs</summary>
        [Required, MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Erforderliche Rolle für Zugriff (optional, z.B. "Admin")</summary>
        [MaxLength(100)]
        public string? RequiredRole { get; set; }

        /// <summary>Sortierreihenfolge</summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>Bereich aktiv</summary>
        public bool IsActive { get; set; } = true;

        // Navigations
        public ICollection<PulseGruppe> Gruppen { get; set; } = new List<PulseGruppe>();
        public ICollection<PulseAccess> Accesses { get; set; } = new List<PulseAccess>();
    }
}

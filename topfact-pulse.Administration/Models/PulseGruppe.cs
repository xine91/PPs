using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace topfact.Pulse.Models
{
    /// <summary>Kategoriegruppen innerhalb eines Bereichs</summary>
    [Table("Pulse_Gruppe", Schema = "dbo")]
    public class PulseGruppe
    {
        [Key]
        public int GruppeID { get; set; }

        /// <summary>Zugehöriger Bereich (FK)</summary>
        [Required]
        public int BereichID { get; set; }

        /// <summary>Eindeutiger Code innerhalb des Bereichs</summary>
        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        /// <summary>Anzeigename der Gruppe</summary>
        [Required, MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Sortierreihenfolge</summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>Gruppe ist zusammenklappbar</summary>
        public bool IsCollapsible { get; set; } = true;

        /// <summary>Gruppe standardmäßig ausgeklappt</summary>
        public bool IsExpandedDefault { get; set; } = true;

        /// <summary>Gruppe aktiv</summary>
        public bool IsActive { get; set; } = true;

        // Navigations
        [ForeignKey("BereichID")]
        public PulseBereich? Bereich { get; set; }

        public ICollection<PulseKategorie> Kategorien { get; set; } = new List<PulseKategorie>();
        public ICollection<PulseAccess> Accesses { get; set; } = new List<PulseAccess>();
    }
}

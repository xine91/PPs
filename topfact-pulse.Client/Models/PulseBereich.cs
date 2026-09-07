using System.Collections.Generic;
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

        [NotMapped]
        public int UiModeId => BereichID;

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? RequiredRole { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public ICollection<PulseGruppe> Gruppen { get; set; } = new List<PulseGruppe>();
        public ICollection<PulseAccess> Accesses { get; set; } = new List<PulseAccess>();
    }
}
using System.Collections.Generic;
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

        [NotMapped]
        public int UiCategoryId => GruppeID;

        [NotMapped]
        public int UiModeId => BereichID;

        [Required]
        public int BereichID { get; set; }

        [Required, MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        public int SortOrder { get; set; } = 0;

        public bool IsCollapsible { get; set; } = true;

        public bool IsExpandedDefault { get; set; } = true;

        public bool IsActive { get; set; } = true;

        [ForeignKey("BereichID")]
        public PulseBereich? Bereich { get; set; }

        [NotMapped]
        public PulseBereich? Mode => Bereich;

        public ICollection<PulseKategorie> Kategorien { get; set; } = new List<PulseKategorie>();
        public ICollection<PulseAccess> Accesses { get; set; } = new List<PulseAccess>();
    }
}
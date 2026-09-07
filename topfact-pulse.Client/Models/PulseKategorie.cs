 using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace topfact.Pulse.Models
{
    /// <summary>Navigationspunkte/Menüpunkte innerhalb einer Gruppe</summary>
    [Table("Pulse_Kategorie", Schema = "dbo")]
    public class PulseKategorie
    {
        [Key]
        public int KategorieID { get; set; }

        [NotMapped]
        public int UiNavItemId => KategorieID;

        [Required]
        public int GruppeID { get; set; }

        [Required, MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(80)]
        public string? IconCss { get; set; }

        [MaxLength(50)]
        public string? RouteBereich { get; set; }

        [MaxLength(40)]
        public string? BadgeText { get; set; }

        [MaxLength(2000)]
        public string? Sql_query { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        [ForeignKey("GruppeID")]
        public PulseGruppe? Gruppe { get; set; }

        [NotMapped]
        public PulseGruppe? Category => Gruppe;
    }
}
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

        /// <summary>Zugehörige Gruppe (FK)</summary>
        [Required]
        public int GruppeID { get; set; }

        /// <summary>Titel des Menüpunkts</summary>
        [Required, MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Icon-CSS-Klasse (z.B. ri-dashboard-line)</summary>
        [MaxLength(80)]
        public string? IconCss { get; set; }


        /// <summary>Route-Parameter Bereich (optional)</summary>
        [MaxLength(50)]
        public string? RouteBereich { get; set; }

        /// <summary>Badge-Text (z.B. "New")</summary>
        [MaxLength(40)]
        public string? BadgeText { get; set; }

        /// <summary>SQL-Query für diese Kategorie</summary>
        [MaxLength(2000)]
        public string? Sql_query { get; set; }

        /// <summary>Zugehöriger Connector für Datenquelle (optional)</summary>
        public int? ConnectorID { get; set; }

        /// <summary>Sortierreihenfolge</summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>Menüpunkt aktiv</summary>
        public bool IsActive { get; set; } = true;

        // Navigations
        [ForeignKey("GruppeID")]
        public PulseGruppe? Gruppe { get; set; }
    }
}

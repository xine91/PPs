using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace topfact.Pulse.Models
{
    /// <summary>KPI-Definitionen für Dashboard-Karten</summary>
    [Table("Pulse_KpiDefinition", Schema = "dbo")]
    public class PulseKpiDefinition
    {
        [Key]
        [Column("KpiDefinitionID")]
        public int KpiDefinitionID { get; set; }

        /// <summary>Anzeigename der KPI-Karte</summary>
        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Kurzbeschreibung/Tooltip</summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>Icon-CSS-Klasse</summary>
        [MaxLength(80)]
        public string IconCss { get; set; } = "ri-bar-chart-line";

        /// <summary>Farbakzent</summary>
        [MaxLength(50)]
        public string Color { get; set; } = "#338562";

        /// <summary>Bereich/Kategorie (z.B. "Technik", "Organisation")</summary>
        [MaxLength(100)]
        public string? Bereich { get; set; }

        /// <summary>SQL-Fragment für den Zählwert</summary>
        [MaxLength(2000)]
        public string? QuerySql { get; set; }

        /// <summary>Zielwert</summary>
        public double? TargetValue { get; set; }

        /// <summary>Toleranz Absolut (+/-)</summary>
        public double? ToleranceAbsolute { get; set; }

        /// <summary>Toleranz Prozent (+/-)</summary>
        public double? TolerancePercent { get; set; }

        /// <summary>Schwelle Grün (ab diesem Wert)</summary>
        public double? ThresholdGreen { get; set; }

        /// <summary>Schwelle Gelb (ab diesem Wert)</summary>
        public double? ThresholdYellow { get; set; }

        /// <summary>Einheit (z.B. "Stück", "%", "h")</summary>
        [MaxLength(30)]
        public string? Unit { get; set; }

        /// <summary>Anzeigestil (z.B. "Card", "Bar", "Gauge")</summary>
        [MaxLength(50)]
        public string? DisplayStyle { get; set; }

        /// <summary>Sortierreihenfolge</summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>Optionale Zuordnung zu einer Navigations-Kategorie (Pulse_Kategorie)</summary>
        public int? KategorieID { get; set; }

        /// <summary>Navigation zur verknüpften Kategorie</summary>
        [ForeignKey(nameof(KategorieID))]
        public PulseKategorie? Kategorie { get; set; }

        /// <summary>Optionale Zuordnung zu einem Connector (Pulse_ConnectorManager)</summary>
        public int? ConnectorID { get; set; }

        /// <summary>KPI aktiv</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Erstellt am</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Zuletzt geändert</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Geändert von (Benutzername)</summary>
        [MaxLength(150)]
        public string? UpdatedBy { get; set; }
    }
}

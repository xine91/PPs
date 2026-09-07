using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace topfact.Pulse.Models
{
    [Table("Pulse_KpiDefinition", Schema = "dbo")]
    public class KpiDefinition
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Anzeigename der KPI-Karte, z. B. "Offene Vorgänge"</summary>
        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Kurzbeschreibung / Tooltip</summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>Remix-Icon CSS-Klasse, z. B. "ri-bar-chart-line"</summary>
        [MaxLength(80)]
        public string IconCss { get; set; } = "ri-bar-chart-line";

        /// <summary>Farbakzent der Karte (CSS-Farbe oder CSS-Variable)</summary>
        [MaxLength(50)]
        public string Color { get; set; } = "#338562";

        /// <summary>Bereich/Kategorie, z. B. "Organisation", "Technik"</summary>
        [MaxLength(100)]
        public string Bereich { get; set; } = string.Empty;

        /// <summary>Datenquelle: Tabellenname oder View-Name in der Datenbank</summary>
        [MaxLength(200)]
        public string? DataSource { get; set; }

        /// <summary>SQL-Fragment für den Zählwert (z. B. "SELECT COUNT(*) FROM ...")</summary>
        [MaxLength(2000)]
        public string? QuerySql { get; set; }

        /// <summary>Erwarteter / Ziel-Wert (für Abweichungsberechnung)</summary>
        public double? TargetValue { get; set; }

        /// <summary>Toleranzbereich (±) – innerhalb = grün, außerhalb = rot/gelb</summary>
        public double? ToleranceAbsolute { get; set; }

        /// <summary>Toleranzbereich in Prozent (alternativ zu absolut)</summary>
        public double? TolerancePercent { get; set; }

        /// <summary>Einheit des Wertes, z. B. "Stück", "%", "h"</summary>
        [MaxLength(30)]
        public string? Unit { get; set; }

        /// <summary>Sortierreihenfolge</summary>
        public int SortOrder { get; set; }

        /// <summary>Aktiv/Inaktiv</summary>
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

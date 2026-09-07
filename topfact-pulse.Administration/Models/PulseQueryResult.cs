using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace topfact.Pulse.Models
{
    /// <summary>Speichert Ergebnisse von SQL-Queries aus Navigation</summary>
    [Table("Pulse_QueryResults", Schema = "dbo")]
    public class PulseQueryResult
    {
        [Key]
        public int QueryID { get; set; }

        /// <summary>Zugehörige Kategorie (FK)</summary>
        public int? KategorieID { get; set; }

        /// <summary>Die SQL-Query die ausgeführt wurde</summary>
        [Required]
        public string Sql_query { get; set; } = string.Empty;

        /// <summary>Die Ergebnisdaten (JSON oder serialisiert)</summary>
        public string? Daten { get; set; }

        /// <summary>Zeitstempel der Erstellung</summary>
        [Required]
        public DateTime created_at { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("KategorieID")]
        public PulseKategorie? Kategorie { get; set; }
    }
}

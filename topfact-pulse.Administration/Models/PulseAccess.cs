using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace topfact.Pulse.Models
{
    /// <summary>Zentrale Benutzer-Rechteverwaltung</summary>
    [Table("Pulse_Access", Schema = "dbo")]
    public class PulseAccess
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Benutzername (aus AD/Entra)</summary>
        [Required, MaxLength(200)]
        [Column("user_name")]
        public string Username { get; set; } = string.Empty;

        /// <summary>Bereich-ID (FK) – NULL = Zugriff auf alle Bereiche</summary>
        public int? BereichID { get; set; }

        /// <summary>Gruppe-ID (FK) – NULL = Zugriff auf alle Gruppen im Bereich</summary>
        public int? GruppeID { get; set; }

        /// <summary>Zugriffslevel: View, Edit, Admin</summary>
        [MaxLength(50)]
        public string Permission { get; set; } = "View";

        /// <summary>Zugriff aktiv</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Erstellt am</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Zuletzt geändert</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Geändert von (Benutzername)</summary>
        [MaxLength(150)]
        public string? UpdatedBy { get; set; }

        // Navigations
        [ForeignKey("BereichID")]
        public PulseBereich? Bereich { get; set; }

        [ForeignKey("GruppeID")]
        public PulseGruppe? Gruppe { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace topfact.Pulse.Models
{
    /// <summary>Login-Audit-Protokoll</summary>
    [Table("Pulse_Logins", Schema = "dbo")]
    public class PulseLogin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Benutzername</summary>
        [Required, MaxLength(200)]
        [Column("user_name")]
        public string Username { get; set; } = string.Empty;

        /// <summary>Login-Zeitstempel</summary>
        [Column("login_time")]
        public DateTime LoginTime { get; set; }

        /// <summary>Login erfolgreich</summary>
        [Column("success")]
        public bool Success { get; set; }

        /// <summary>IP-Adresse des Clients</summary>
        [MaxLength(50)]
        [Column("ip_address")]
        public string? IpAddress { get; set; }
    }
}

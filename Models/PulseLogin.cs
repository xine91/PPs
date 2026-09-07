using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace topfact.Pulse.Models
{
    [Table("Pulse_Logins")]
    public class PulseLogin
    {
        [Column("user_name")]
        [MaxLength(200)]
        public string? Username { get; set; }

        [Column("login_time")]
        public DateTime LoginTime { get; set; }

        [Column("success")]
        public bool Success { get; set; }

        [Column("ip_address")]
        [MaxLength(50)]
        public string? IpAddress { get; set; }
    }
}

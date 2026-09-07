using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace topfact.Pulse.Models
{
    /// <summary>
    /// Connector-Eintrag aus Pulse_ConnectorManager
    /// Mapping zu DB-Spalten: id, user_name, connector_type, api_config,
    /// sql_config, manual_config, created_at, updated_at, bezeichnung, status, KategorieID
    /// </summary>
    [Table("Pulse_ConnectorManager", Schema = "dbo")]
    public class PulseConnector
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_name")]
        [MaxLength(150)]
        public string UserName { get; set; } = string.Empty;

        [Column("connector_type")]
        [MaxLength(200)]
        public string? ConnectorType { get; set; }

        [Column("api_config")]
        public string? ApiConfig { get; set; }

        [Column("sql_config")]
        public string? SqlConfig { get; set; }

        [Column("manual_config")]
        public string? ManualConfig { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("bezeichnung")]
        [MaxLength(200)]
        public string? Bezeichnung { get; set; }

        [Column("status")]
        [MaxLength(50)]
        public string? Status { get; set; }

        [Column("KategorieID")]
        public int? KategorieID { get; set; }
    }
}
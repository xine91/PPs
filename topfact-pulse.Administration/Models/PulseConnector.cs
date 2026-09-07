using System;

namespace topfact.Pulse.Models
{
    public class PulseConnector 
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ConnectorType { get; set; } = string.Empty;
        public string? ApiConfig { get; set; }
        public string? SqlConfig { get; set; }
        public string? ManualConfig { get; set; }
        public string? Bezeichnung { get; set; }
        public string? Status { get; set; }
        public int? KategorieID { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

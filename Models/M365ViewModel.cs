using System;
using System.Collections.Generic;

namespace topfact.Pulse.Models
{
    public class M365Entry
    {
        public string Tenant { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public int Days { get; set; }
        public string TokenStatus { get; set; } = "Nicht konfiguriert";
        public string TokenDetails { get; set; } = string.Empty;
    }

    public class M365TokenCheckConfig
    {
        public string Customer { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }

    public class M365TokenValidationResult
    {
        public string Status { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime? PasswordCredentialEndDate { get; set; }
        public int? DaysUntilPasswordCredentialEnd { get; set; }
    }

    public class M365ViewModel
    {
        public List<M365Entry> Entries { get; set; } = new();
        public int SelectedDays { get; set; } = 30;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}

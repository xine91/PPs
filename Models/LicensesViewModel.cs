using System;
using System.Collections.Generic;

namespace topfact.Pulse.Models
{
    public class LicenseEntry
    {
        public string Tenant { get; set; } = string.Empty;
        public int Licenses { get; set; }
        public DateTime? Date { get; set; }
    }

    public class LicensesViewModel
    {
        public List<LicenseEntry> Entries { get; set; } = new();
        public int SelectedDays { get; set; } = 30;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}

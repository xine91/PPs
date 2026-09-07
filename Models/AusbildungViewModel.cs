using System;
using System.Collections.Generic;

namespace topfact.Pulse.Models
{
    public class AusbildungEntry
    {
        public string Ausbilder { get; set; } = string.Empty;
    }

    public class AusbildungViewModel
    {
        public List<AusbildungEntry> Entries { get; set; } = new();
        public int SelectedDays { get; set; } = 30;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}

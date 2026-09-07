using System;
using System.Collections.Generic;

namespace topfact.Pulse.Models
{
    public class WochenberichtEntry
    {
        public string AzubiName { get; set; } = string.Empty;
        public DateTime? Ausbildungsbeginn { get; set; }
        public int ErwarteteBerichte { get; set; }
        public int VorhandeneBerichte { get; set; }
        public int FehlendeBerichte { get; set; }
    }

    public class WochenberichteViewModel
    {
        public List<WochenberichtEntry> Entries { get; set; } = new();
        public int SelectedDays { get; set; } = 30;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}

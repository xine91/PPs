using System;
using System.Collections.Generic;

namespace topfact.Pulse.Models
{
    public class BAPEntry
    {
        public string modul { get; set; } = string.Empty;
        public string Ausbilder { get; set; } = string.Empty;
        public DateTime? Beginn { get; set; }
        public DateTime? Ende { get; set; }
        public string Teilnehmer { get; set; } = string.Empty;
        public string Ausbildungsgang { get; set; } = string.Empty;
        public string Zeitliche_Richtwerte { get; set; } = string.Empty;
        public decimal Dauer { get; set; }
        public decimal Wert { get; set; }
        public decimal Perc { get; set; }
    }

    public class BAPViewModel
    {
        public List<BAPEntry> Entries { get; set; } = new();
        public int SelectedDays { get; set; } = 30;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}

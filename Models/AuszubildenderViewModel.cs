using System;
using System.Collections.Generic;

namespace topfact.Pulse.Models
{
    public class AuszubildenderEntry
    {
        public string TeilnehmerId { get; set; } = string.Empty;
        public string Vorname { get; set; } = string.Empty;
        public string Nachname { get; set; } = string.Empty;
        public string InternExtern { get; set; } = string.Empty;
        public string Ausbildungsgang { get; set; } = string.Empty;
        public DateTime? Beginn { get; set; }
        public DateTime? Ende { get; set; }
        public DateTime? Geburtsdatum { get; set; }
        public DateTime? DateModified { get; set; }
    }

    public class AuszubildenderViewModel
    {
        public List<AuszubildenderEntry> Entries { get; set; } = new();
        public int SelectedDays { get; set; } = 30;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}

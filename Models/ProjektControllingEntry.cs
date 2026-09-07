using System;

namespace topfact.Pulse.Models
{
    public class ProjektControllingEntry
    {
        public int? Id { get; set; }
        public int? VorgangId { get; set; }
        public string Nummer { get; set; } = string.Empty;
        public string Vorgang { get; set; } = string.Empty;
        public string Firma { get; set; } = string.Empty;
        public string Bearbeiter { get; set; } = string.Empty;
        public DateTime Datum { get; set; }
        public DateTime? Startdatum { get; set; }
        public DateTime? Endedatum { get; set; }
        public decimal ArbeitszeitMinuten { get; set; }
        public string Titel { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
    }
}
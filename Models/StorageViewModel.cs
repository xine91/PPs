using System;
using System.Collections.Generic;

namespace topfact.Pulse.Models
{
    public class StorageEntry
    {
        public string OrgId { get; set; } = string.Empty;
        public string Organisation { get; set; } = string.Empty;
        public int AnzahlArchive { get; set; }
        public decimal Speicherplatz { get; set; }
        public decimal Lizenz_Speicherplatz { get; set; }
        public decimal Wachstum_pro_Tag { get; set; }
        public int Tage_bis_Grenze { get; set; }
    }

    public class StorageViewModel
    {
        public List<StorageEntry> Entries { get; set; } = new();
        public int SelectedDays { get; set; } = 30;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}

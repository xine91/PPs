namespace topfact.Pulse.Models
{
    public class GeplanteFunktionEntry
    {
        public string Bearbeiter { get; set; } = string.Empty;
        public int ID { get; set; }
        public string Name1 { get; set; } = string.Empty;
        public string Titel { get; set; } = string.Empty;
        public string Benennung { get; set; } = string.Empty;
        public decimal Stunden { get; set; }
        public DateTime? Datum { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? FkTblAnsprechpartner { get; set; }
        public DateTime? StartDatum { get; set; }
    }

    public class GeplanteFunktionenViewModel
    {
        public List<string> AvailableBearbeiter { get; set; } = new();
        public List<string> AvailableFirmen { get; set; } = new();
        public string? SelectedBearbeiter { get; set; }
        public string? SelectedFirma { get; set; }
        public int SelectedDays { get; set; } = 30;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public List<GeplanteFunktionEntry> Entries { get; set; } = new();
    }
}

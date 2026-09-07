namespace topfact.Pulse.Models
{
    public class ProjektControllingVorgangOption
    {
        public int VorgangId { get; set; }
        public string Nummer { get; set; } = string.Empty;
        public string Vorgang { get; set; } = string.Empty;
        public string Firma { get; set; } = string.Empty;
    }

    public class ProjektControllingViewModel
    {
        public List<ProjektControllingEntry> Entries { get; set; } = new();
        public int SelectedDays { get; set; } = 30;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? VorgangNummernInput { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

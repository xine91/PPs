namespace topfact.Pulse.Models
{
    public class ProjektControllingControllerViewModel
    {
        public int SelectedDays { get; set; } = 30;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? VorgangNummernInput { get; set; }
        public string? ErrorMessage { get; set; }

        public List<ProjektControllingEntry> Entries { get; set; } = new();
    }
}
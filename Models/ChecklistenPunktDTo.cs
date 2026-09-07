namespace topfact.Pulse.Models.Organisation
{
    public class ChecklistenPunktDto
    {
        public string? Nummer { get; set; }
        public string? Benennung { get; set; }
        public string? Beschreibung { get; set; }
        public string? Bemerkung { get; set; }
        public int? Dauer { get; set; }
        public DateTime? Plandatum { get; set; }
        public DateTime? DateCreated { get; set; }
        public string? Bearbeiter { get; set; }
        public string? Status { get; set; }
    }
}

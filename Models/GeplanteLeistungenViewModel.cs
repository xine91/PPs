namespace topfact.Pulse.Models
{
    public class GeplanteLeistungEntry
    {
        public string Bearbeiter { get; set; } = string.Empty;
        public int ID { get; set; }
        public string Name1 { get; set; } = string.Empty;
        public string Titel { get; set; } = string.Empty;
        public string Aktion { get; set; } = string.Empty;
        public decimal Stunden { get; set; }
        public DateTime? Datum { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? FkTblAnsprechpartner { get; set; }
    }

    public class GeplanteLeistungenGruppe
    {
        public string Bearbeiter { get; set; } = string.Empty;
        public decimal PlanStunden { get; set; }
        public int AnzahlPlanungen { get; set; }
        public decimal FaktorLeistungsplanung { get; set; }
    }

    public class ErbrachteLeistungen14TgGruppe
    {
        public string Bearbeiter { get; set; } = string.Empty;
        public decimal LeistungsStunden_14Tg { get; set; }
        public int AnzahlLeistungen_14Tg { get; set; }
        public decimal FaktorLeistung_14Tg { get; set; }
    }

    public class GeplanteLeistungenViewModel
    {
        public List<GeplanteLeistungEntry> Entries { get; set; } = new();
        public List<GeplanteLeistungenGruppe> Gruppen { get; set; } = new();
        public List<ErbrachteLeistungen14TgGruppe> Leistungen14Tg { get; set; } = new();
    }
}

using System;

namespace topfact.Pulse.Models
{
    /// <summary>
    /// Eine Zeile der Vorgangs-Übersicht (Index-Seite Vorgangskontrolle).
    /// Befüllt aus der "Aktive Vorgänge" SQL-Query (tblAktivitaet + tblFirma + tblStatus + ...).
    /// </summary>
    public class VorgangListEntry
    {
        public int VorgangsNr { get; set; }
        public string Firma { get; set; } = "";
        public string Vorgangstitel { get; set; } = "";
        public string Vorgangsart { get; set; } = "";   // tblAktivitaetentyp.Aktivitaetentyp
        public string Artikel { get; set; } = "";

        public string StatusName { get; set; } = "";
        public string? StatusColor { get; set; }              // tblStatus.Color, z.B. "#ffa07a"

        public string Bearbeiter { get; set; } = "";
        public DateTime? Erfassungsdatum { get; set; }
        public DateTime? DateModified { get; set; }

        public int DauerIstMinuten { get; set; }          // dbo.fn_GetVorgangDauerIst
        public int DauerPlanMinuten { get; set; }         // geplanter Aufwand
        public int Aktionen1 { get; set; }         // erledigt   (WorkTyp = 1)
        public int Aktionen2 { get; set; }         // in Arbeit  (WorkTyp = 2)
        public int Aktionen3 { get; set; }         // offen      (WorkTyp = 3)
        public bool KundeWartet { get; set; }      // Projektstatus "wartet auf Kunde"
        public int AbrechnbareAktionenMonat { get; set; } // kostenpflichtige Aktionen im lfd. Monat

        public int AnzahlChecklistenpunkte { get; set; } // Anzahl Checklistenpunkte (0 = ohne Checkliste)
        public int AnzahlKommentare { get; set; }
        public DateTime? LetzterKommentar { get; set; }

        // ─── Computed (für UI) ────────────────────────────
        public int AktionenGesamt => Aktionen1 + Aktionen2 + Aktionen3;

        /// <summary>
        /// Gruppiert die tblStatus.Color in 3 UI-Kategorien:
        /// "neu" (rot) | "aktiv" (gelb) | "fertig" (grün).
        /// </summary>
        public string StatusKategorie
        {
            get
            {
                var c = (StatusColor ?? "").ToLowerInvariant().Trim();
                if (c == "#ffa07a") return "neu";
                if (c == "#f0e68c") return "aktiv";
                if (c == "#90ee90") return "fertig";
                return "unbekannt";
            }
        }
    }

    public class VorgaengeIndexViewModel
    {
        public List<VorgangListEntry> Vorgaenge { get; set; } = new();
        public string? ErrorMessage { get; set; }

        /// <summary>Wird direkt aus view_geplante_Leistungen befüllt (überschreibt die Summe aus DauerPlanMinuten).</summary>
        public int? AufwandGeplantMinutenOverride { get; set; }

        // Aggregat für KPI-Strip
        public int CountTotal => Vorgaenge.Count;
        public int CountNeu => Vorgaenge.Count(v => v.StatusKategorie == "neu");
        public int CountAktiv => Vorgaenge.Count(v => v.StatusKategorie == "aktiv");
        // Offene Aktionen in der Vergangenheit: WorkTyp 3 (offen) - wird client-seitig weiter gefiltert
        public int OffeneAktionenVergangenheit => Vorgaenge.Sum(v => v.Aktionen3);
        // Aufwand geplant gesamt
        public int AufwandGeplantMinuten => AufwandGeplantMinutenOverride ?? Vorgaenge.Sum(v => v.DauerPlanMinuten);
        public int AufwandGeplantStunden => (int)Math.Ceiling(AufwandGeplantMinuten / 60.0);
        public double AufwandGeplantStundenDecimal => Math.Round(AufwandGeplantMinuten / 60.0, 1);
        // Abrechnbare Aktionen im laufenden Monat
        public int AbrechnbareAktionenMonat => Vorgaenge.Sum(v => v.AbrechnbareAktionenMonat);
    }
}

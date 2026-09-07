// Models/WeeklyReportDocument.cs
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace topfact.Pulse.Models
{
    // ── Modelle ───────────────────────────────────────────────────────────────

    public class WeeklyReportModel
    {
        public string VorgangNr = "";
        public string VorgangTitel = "";
        public string Firma = "";
        public string Verantwortlicher = "";
        public DateTime? VorgangStart;
        public DateTime? VorgangEnde;
        public DateTime WeekStart;
        public DateTime WeekEnd;

        public decimal PlanMinuten;
        public decimal IstMinuten;
        public int AnzahlGesamt;
        public int AnzahlErledigt;
        public int AnzahlAktiv;

        public List<WochenTaetigkeit> Taetigkeiten = new();
        public List<BearbeiterStat> Bearbeiter = new();

        public int AnzahlOffen => Math.Max(0, AnzahlGesamt - AnzahlErledigt - AnzahlAktiv);
        public int FortschrittPct => AnzahlGesamt > 0 ? (int)Math.Round(AnzahlErledigt * 100.0 / AnzahlGesamt) : 0;
        public decimal WochenIstMinuten => Taetigkeiten.Sum(t => t.IstMinuten);
    }

    public class WochenTaetigkeit
    {
        public string? Titel;
        public string? Bearbeiter;
        public DateTime? Datum;
        public decimal IstMinuten;
        public string? StatusText;
    }

    public class BearbeiterStat
    {
        public string? Name;
        public int Anzahl;
        public decimal IstMinuten;
    }

    // ── PDF-Dokument ──────────────────────────────────────────────────────────

    public class ProjektWeeklyReportDocument : IDocument
    {
        private readonly WeeklyReportModel _m;

        private const string Navy = "#0F2942";
        private const string NavyLight = "#1B3A5C";
        private const string NavyMid = "#243F63";
        private const string Green = "#2E8B6E";
        private const string GreenLight = "#E6F4F0";
        private const string GreenBright = "#10B981";
        private const string Amber = "#D97706";
        private const string AmberLight = "#FEF3C7";
        private const string Indigo = "#4F46E5";
        private const string IndigoLight = "#EEF2FF";
        private const string Red = "#DC2626";
        private const string RedLight = "#FEF2F2";
        private const string Slate50 = "#F8FAFC";
        private const string Slate100 = "#F1F5F9";
        private const string Slate200 = "#E2E8F0";
        private const string Slate400 = "#94A3B8";
        private const string Slate600 = "#475569";
        private const string Slate800 = "#1E293B";
        private const string White = "#FFFFFF";

        public ProjektWeeklyReportDocument(WeeklyReportModel model) => _m = model;

        public DocumentMetadata GetMetadata() => new()
        {
            Title = $"Wochenbericht – {_m.VorgangTitel}",
            Author = "topfact Pulse",
            Subject = $"KW {Kw(_m.WeekStart)}/{_m.WeekStart:yyyy} · Vorgang #{_m.VorgangNr}",
            Creator = "topfact Pulse",
        };

        public void Compose(IDocumentContainer container)
        {
            container.Page(p =>
            {
                p.Size(PageSizes.A4);
                p.Margin(0);
                p.DefaultTextStyle(x => x
                    .FontFamily("Arial")
                    .FontSize(8.5f)
                    .FontColor(Slate800));

                p.Header().Element(ComposeHeader);
                p.Content()
                    .Background(Slate50)
                    .PaddingHorizontal(28)
                    .PaddingTop(22)
                    .PaddingBottom(16)
                    .Column(col =>
                    {
                        col.Item().Element(ComposeKpiRow);
                        col.Item().Height(14);
                        col.Item().Element(ComposePlanIstRow);
                        col.Item().Height(14);
                        col.Item().Element(ComposeWochenHighlight);
                        col.Item().Height(16);
                        col.Item().Element(ComposeTaetigkeiten);
                        if (_m.Bearbeiter.Any())
                        {
                            col.Item().Height(14);
                            col.Item().Element(ComposeBearbeiter);
                        }
                    });
                p.Footer().Element(ComposeFooter);
            });
        }

        // ─── HEADER ──────────────────────────────────────────────────────────

        private void ComposeHeader(IContainer c)
        {
            c.Column(col =>
            {
                // Grüner Akzentstreifen
                col.Item().Height(5f).Background(Green);

                // Haupt-Header
                col.Item()
                   .Background(Navy)
                   .PaddingHorizontal(28)
                   .PaddingVertical(18)
                   .Row(row =>
                   {
                       // Links
                       row.RelativeItem().Column(inner =>
                       {
                           // KW-Badge + Label
                           inner.Item().Row(er =>
                           {
                               er.AutoItem()
                                 .Background(Green)
                                 .PaddingHorizontal(6f)
                                 .PaddingVertical(3f)
                                 .CornerRadius(3f)
                                 .Text($"KW {Kw(_m.WeekStart)}/{_m.WeekStart:yyyy}")
                                 .FontSize(7).Bold().FontColor(White);
                               er.AutoItem()
                                 .PaddingLeft(8)
                                 .AlignMiddle()
                                 .Text("WOCHENBERICHT")
                                 .FontSize(7).FontColor("#5A88B0");
                           });

                           // Titel
                           inner.Item()
                                .PaddingTop(6)
                                .Text(_m.VorgangTitel)
                                .FontSize(15).Bold().FontColor(White).LineHeight(1.22f);

                           // Metadaten
                           inner.Item().PaddingTop(7).Row(mr =>
                           {
                               MetaItem(mr, "Firma", _m.Firma);
                               MetaItem(mr, "Bearb.", _m.Verantwortlicher);
                               if (_m.VorgangStart.HasValue)
                                   MetaItem(mr, "Start", $"{_m.VorgangStart:dd.MM.yyyy}");
                               if (_m.VorgangEnde.HasValue)
                                   MetaItem(mr, "Ziel", $"{_m.VorgangEnde:dd.MM.yyyy}");
                           });
                       });

                       // Rechts: Fortschritts-Badge
                       row.ConstantItem(76)
                          .AlignCenter()
                          .AlignMiddle()
                          .Width(68)
                          .Height(68)
                          .Border(2f)
                          .BorderColor(GreenBright)
                          .CornerRadius(34f)
                          .Background(NavyMid)
                          .Column(badge =>
                          {
                              badge.Item().PaddingTop(14).AlignCenter()
                                   .Text($"{_m.FortschrittPct}%")
                                   .FontSize(18).Bold().FontColor(White);
                              badge.Item().AlignCenter()
                                   .Text("ERLEDIGT")
                                   .FontSize(5.5f).FontColor("#7AADCC");
                          });
                   });

                // Fortschrittsbalken + Legende
                col.Item()
                   .Background(NavyMid)
                   .PaddingHorizontal(28)
                   .PaddingVertical(9)
                   .Row(row =>
                   {
                       // Balken
                       row.RelativeItem(2).AlignMiddle().Column(bar =>
                       {
                           var fill = _m.AnzahlGesamt > 0
                               ? Math.Min((float)_m.AnzahlErledigt / _m.AnzahlGesamt, 1f)
                               : 0f;
                           bar.Item().Height(6f).Row(r =>
                           {
                               if (fill > 0.001f)
                                   r.RelativeItem(fill).Height(6f).Background(GreenBright).CornerRadius(3f);
                               if (fill < 0.999f)
                                   r.RelativeItem(1f - fill).Height(6f).Background("#0D2035").CornerRadius(3f);
                           });
                       });

                       // Legende
                       row.RelativeItem(3).PaddingLeft(16).AlignMiddle().Row(leg =>
                       {
                           LegendItem(leg, GreenBright, "Erledigt", _m.AnzahlErledigt);
                           LegendItem(leg, Amber, "In Bearb.", _m.AnzahlAktiv);
                           LegendItem(leg, "#334E73", "Offen", _m.AnzahlOffen);
                       });

                       // Zeitraum
                       row.AutoItem().AlignMiddle()
                          .Text($"{_m.WeekStart:dd.MM.} – {_m.WeekEnd:dd.MM.yyyy}")
                          .FontSize(7.5f).FontColor("#7AADCC");
                   });
            });
        }

        private static void MetaItem(RowDescriptor row, string label, string text)
        {
            row.AutoItem().PaddingRight(14).Row(r =>
            {
                r.AutoItem().Text($"{label}:").FontSize(7.5f).FontColor("#5A88B0");
                r.AutoItem().PaddingLeft(3).Text(text).FontSize(7.5f).FontColor("#A8C8E0");
            });
        }

        private static void LegendItem(RowDescriptor row, string color, string label, int count)
        {
            row.AutoItem().PaddingRight(14).Row(r =>
            {
                r.AutoItem().Width(7).Height(7).CornerRadius(3.5f).Background(color).AlignMiddle();
                r.AutoItem().PaddingLeft(4).Text($"{label} {count}").FontSize(7.5f).FontColor("#A8C8E0");
            });
        }

        // ─── KPI-ZEILE ────────────────────────────────────────────────────────

        private void ComposeKpiRow(IContainer c)
        {
            c.Row(row =>
            {
                KpiCard(row, "AKTIONEN", _m.AnzahlGesamt.ToString(), "gesamt", Navy, "#E8EFF7");
                row.ConstantItem(10);
                KpiCard(row, "ERLEDIGT", _m.AnzahlErledigt.ToString(), $"{_m.FortschrittPct}% Abschluss", GreenBright, GreenLight);
                row.ConstantItem(10);
                KpiCard(row, "IN BEARB.", _m.AnzahlAktiv.ToString(), "Durchführung", Amber, AmberLight);
                row.ConstantItem(10);
                KpiCard(row, "OFFEN", _m.AnzahlOffen.ToString(), "Planung", Indigo, IndigoLight);
                row.ConstantItem(10);
                KpiCard(row, "TEAM",
                    _m.Bearbeiter.Count > 0 ? _m.Bearbeiter.Count.ToString() : "–",
                    "Bearbeiter", Green, GreenLight);
            });
        }

        private static void KpiCard(RowDescriptor row, string label, string value,
                                    string hint, string accentColor, string bgColor)
        {
            row.RelativeItem()
               .Background(White)
               .Border(1f).BorderColor(Slate200)
               .Column(c =>
               {
                   c.Item()
                    .PaddingHorizontal(10)
                    .PaddingTop(10)
                    .PaddingBottom(8)
                    .Column(inner =>
                    {
                        inner.Item().Text(label).FontSize(6.5f).Bold().FontColor(Slate400);
                        inner.Item().PaddingTop(3).Text(value).FontSize(22).Bold().FontColor(accentColor);
                        inner.Item().PaddingTop(1).Text(hint).FontSize(7).FontColor(Slate400);
                    });
                   // Farbiger Bodenbalken
                   c.Item().Height(4f).Background(accentColor);
               });
        }

        // ─── PLAN / IST / ABWEICHUNG ──────────────────────────────────────────

        private void ComposePlanIstRow(IContainer c)
        {
            var delta = _m.IstMinuten - _m.PlanMinuten;
            var deltaH = Math.Round(delta / 60m, 1);
            var deltaCol = delta > 0 ? Red : GreenBright;
            var deltaLbl = delta == 0 ? "Im Plan"
                         : delta > 0 ? $"{Math.Abs(deltaH):0.#} h über Plan"
                                      : $"{Math.Abs(deltaH):0.#} h unter Plan";
            var fillRatio = _m.PlanMinuten > 0
                          ? (float)Math.Min(1.0, (double)_m.IstMinuten / (double)_m.PlanMinuten)
                          : 0f;

            c.Row(row =>
            {
                // Plan
                PviCard(row, "PLAN-AUFWAND", H(_m.PlanMinuten),
                    $"{(int)_m.PlanMinuten} Minuten geplant",
                    Indigo, 1.0f);
                row.ConstantItem(10);

                // Ist
                PviCard(row, "IST-AUFWAND",
                    _m.IstMinuten > 0 ? H(_m.IstMinuten) : "—",
                    _m.IstMinuten > 0 ? $"{(int)_m.IstMinuten} Minuten erfasst" : "Noch keine Tätigkeiten",
                    GreenBright, fillRatio);
                row.ConstantItem(10);

                // Abweichung
                row.RelativeItem()
                   .Background(delta > 0 ? RedLight : GreenLight)
                   .Border(1f).BorderColor(delta > 0 ? "#FECACA" : "#A7F3D0")
                   .Row(r =>
                   {
                       // Linker Akzentstreifen
                       r.ConstantItem(4f).Background(deltaCol);
                       r.RelativeItem()
                        .PaddingHorizontal(10)
                        .PaddingTop(10)
                        .PaddingBottom(8)
                        .Column(cc =>
                        {
                            cc.Item().Text("ABWEICHUNG").FontSize(6.5f).Bold().FontColor(Slate400);
                            cc.Item().PaddingTop(3)
                              .Text(delta == 0 ? "±0 h" : (deltaH >= 0 ? $"+{deltaH}" : $"{deltaH}") + " h")
                              .FontSize(22).Bold().FontColor(deltaCol);
                            cc.Item().PaddingTop(1)
                              .Text(deltaLbl).FontSize(7.5f).Bold().FontColor(deltaCol);
                        });
                   });
            });
        }

        private static void PviCard(RowDescriptor row, string label, string value,
                                    string sub, string color, float fillRatio)
        {
            row.RelativeItem()
               .Background(White)
               .Border(1f).BorderColor(Slate200)
               .Row(r =>
               {
                   // Linker Akzentstreifen
                   r.ConstantItem(4f).Background(color);
                   r.RelativeItem()
                    .PaddingHorizontal(10)
                    .PaddingTop(10)
                    .PaddingBottom(8)
                    .Column(c =>
                    {
                        c.Item().Text(label).FontSize(6.5f).Bold().FontColor(Slate400);
                        c.Item().PaddingTop(3).Text(value).FontSize(22).Bold().FontColor(color);
                        c.Item().PaddingTop(1).Text(sub).FontSize(7).FontColor(Slate400);
                        // Fortschrittsbalken
                        var clamp = Math.Min(Math.Max(fillRatio, 0f), 1f);
                        c.Item().PaddingTop(6).Height(4f).Row(pr =>
                        {
                            if (clamp > 0.001f)
                                pr.RelativeItem(clamp).Height(4f).Background(color).CornerRadius(2f);
                            if (clamp < 0.999f)
                                pr.RelativeItem(1f - clamp).Height(4f).Background(Slate200).CornerRadius(2f);
                        });
                    });
               });
        }

        // ─── WOCHEN-HIGHLIGHT ─────────────────────────────────────────────────

        private void ComposeWochenHighlight(IContainer c)
        {
            c.Background(GreenLight)
             .Border(1f).BorderColor("#6EE7B7")
             .Row(outer =>
             {
                 // Linker grüner Akzentstreifen
                 outer.ConstantItem(5f).Background(Green);

                 outer.RelativeItem()
                      .PaddingHorizontal(14)
                      .PaddingVertical(12)
                      .Row(row =>
                      {
                          // Linke Seite: Stunden
                          row.AutoItem().Column(left =>
                          {
                              left.Item().Text("DIESE WOCHE ERFASST")
                                  .FontSize(6.5f).Bold().FontColor(Green);
                              left.Item().PaddingTop(3)
                                  .Text(_m.WochenIstMinuten > 0 ? H(_m.WochenIstMinuten) : "Keine Tätigkeiten")
                                  .FontSize(26).Bold().FontColor(Navy);
                              left.Item().PaddingTop(2)
                                  .Text($"{_m.Taetigkeiten.Count} Tätigkeiten  ·  {(int)_m.WochenIstMinuten} Minuten")
                                  .FontSize(8).FontColor(Slate600);
                          });

                          row.RelativeItem();

                          // Rechte Seite: Bearbeiter
                          if (_m.Bearbeiter.Any())
                          {
                              row.AutoItem().AlignMiddle().Column(right =>
                              {
                                  right.Item().Text("BEARBEITER DIESE WOCHE")
                                       .FontSize(6.5f).Bold().FontColor(Green);
                                  foreach (var b in _m.Bearbeiter.Take(4))
                                  {
                                      right.Item().PaddingTop(5).Row(r =>
                                      {
                                          r.ConstantItem(24)
                                           .Height(24)
                                           .CornerRadius(12f)
                                           .Background(NavyLight)
                                           .AlignCenter()
                                           .AlignMiddle()
                                           .Text(Ini(b.Name))
                                           .FontSize(7).Bold().FontColor(White);
                                          r.AutoItem().PaddingLeft(7).AlignMiddle().Column(nc =>
                                          {
                                              nc.Item().Text(b.Name ?? "–").FontSize(8).Bold().FontColor(Slate800);
                                              nc.Item().Text($"{b.Anzahl} Tätigkeiten  ·  {H(b.IstMinuten)}")
                                                .FontSize(7).FontColor(Slate600);
                                          });
                                      });
                                  }
                              });
                          }
                      });
             });
        }

        // ─── TÄTIGKEITEN-TABELLE ──────────────────────────────────────────────

        private void ComposeTaetigkeiten(IContainer c)
        {
            c.Column(col =>
            {
                // Abschnitts-Header
                col.Item()
                   .Background(Navy)
                   .PaddingHorizontal(10)
                   .PaddingVertical(8)
                   .Row(row =>
                   {
                       row.AutoItem().Width(3f).Height(14f).CornerRadius(1.5f).Background(Green);
                       row.AutoItem().PaddingLeft(8).AlignMiddle()
                          .Text("TÄTIGKEITEN DIESER WOCHE")
                          .FontSize(8.5f).Bold().FontColor(White);
                       row.RelativeItem();
                       row.AutoItem().AlignMiddle()
                          .Text($"{_m.Taetigkeiten.Count} Einträge  ·  {H(_m.WochenIstMinuten)} gesamt")
                          .FontSize(7.5f).FontColor("#7AADCC");
                   });

                if (!_m.Taetigkeiten.Any())
                {
                    col.Item()
                       .Background(White)
                       .BorderBottom(1f).BorderLeft(1f).BorderRight(1f).BorderColor(Slate200)
                       .Padding(28)
                       .AlignCenter()
                       .Text("Keine Tätigkeiten in dieser Woche erfasst.")
                       .FontSize(9).FontColor(Slate400).Italic();
                    return;
                }

                // Tabellen-Header
                col.Item()
                   .Background(Slate100)
                   .BorderBottom(1f).BorderLeft(1f).BorderRight(1f).BorderColor(Slate200)
                   .PaddingHorizontal(10)
                   .PaddingVertical(6)
                   .Row(row =>
                   {
                       row.ConstantItem(52).Text("DATUM").FontSize(6.5f).Bold().FontColor(Slate400);
                       row.RelativeItem().Text("TÄTIGKEIT").FontSize(6.5f).Bold().FontColor(Slate400);
                       row.ConstantItem(105).Text("BEARBEITER").FontSize(6.5f).Bold().FontColor(Slate400);
                       row.ConstantItem(75).Text("STATUS").FontSize(6.5f).Bold().FontColor(Slate400);
                       row.ConstantItem(48).AlignRight().Text("IST-ZEIT").FontSize(6.5f).Bold().FontColor(Slate400);
                   });

                // Datenzeilen
                var isOdd = false;
                foreach (var t in _m.Taetigkeiten)
                {
                    isOdd = !isOdd;
                    var sKat = SKat(t.StatusText);
                    var sCol = sKat == "done" ? GreenBright
                             : sKat == "active" ? Amber
                             : Indigo;
                    var sBg = sKat == "done" ? GreenLight
                             : sKat == "active" ? AmberLight
                             : IndigoLight;

                    col.Item()
                       .Background(isOdd ? White : Slate50)
                       .BorderBottom(1f).BorderLeft(1f).BorderRight(1f).BorderColor(Slate200)
                       .PaddingHorizontal(10)
                       .PaddingVertical(5)
                       .Row(row =>
                       {
                           row.ConstantItem(52)
                              .Text(t.Datum.HasValue ? t.Datum.Value.ToString("dd.MM.yy") : "–")
                              .FontSize(7.5f).FontColor(Slate600);

                           row.RelativeItem()
                              .Text(t.Titel ?? "–")
                              .FontSize(8.5f).FontColor(Slate800);

                           row.ConstantItem(105).Row(br =>
                           {
                               br.ConstantItem(18).Height(18).CornerRadius(9f)
                                 .Background(NavyLight).AlignCenter().AlignMiddle()
                                 .Text(Ini(t.Bearbeiter)).FontSize(5.5f).Bold().FontColor(White);
                               br.AutoItem().PaddingLeft(5).AlignMiddle()
                                 .Text(t.Bearbeiter ?? "–").FontSize(7.5f).FontColor(Slate800);
                           });

                           row.ConstantItem(75).AlignMiddle()
                              .Background(sBg)
                              .CornerRadius(3f)
                              .PaddingHorizontal(5f)
                              .PaddingVertical(2f)
                              .Text(t.StatusText ?? "–")
                              .FontSize(6.5f).Bold().FontColor(sCol);

                           row.ConstantItem(48).AlignRight()
                              .Text(t.IstMinuten > 0 ? H(t.IstMinuten) : "–")
                              .FontSize(8f).Bold()
                              .FontColor(t.IstMinuten > 0 ? GreenBright : Slate400);
                       });
                }

                // Summen-Zeile
                col.Item()
                   .Background(NavyLight)
                   .PaddingHorizontal(10)
                   .PaddingVertical(7)
                   .Row(row =>
                   {
                       row.RelativeItem()
                          .Text($"Summe KW {Kw(_m.WeekStart)}/{_m.WeekStart:yyyy}")
                          .FontSize(8).Bold().FontColor(White);
                       row.ConstantItem(48).AlignRight()
                          .Text(H(_m.WochenIstMinuten))
                          .FontSize(9).Bold().FontColor("#86EFAC");
                   });
            });
        }

        // ─── BEARBEITER-SEKTION ───────────────────────────────────────────────

        private void ComposeBearbeiter(IContainer c)
        {
            c.Column(col =>
            {
                col.Item()
                   .Background(Navy)
                   .PaddingHorizontal(10)
                   .PaddingVertical(8)
                   .Row(row =>
                   {
                       row.AutoItem().Width(3f).Height(14f).CornerRadius(1.5f).Background(Green);
                       row.AutoItem().PaddingLeft(8).AlignMiddle()
                          .Text("AUFWAND NACH BEARBEITER (WOCHE)")
                          .FontSize(8.5f).Bold().FontColor(White);
                   });

                var maxMin = _m.Bearbeiter.Any() ? _m.Bearbeiter.Max(b => b.IstMinuten) : 1m;

                col.Item()
                   .Background(White)
                   .BorderBottom(1f).BorderLeft(1f).BorderRight(1f).BorderColor(Slate200)
                   .PaddingHorizontal(12)
                   .PaddingVertical(10)
                   .Column(inner =>
                   {
                       foreach (var b in _m.Bearbeiter)
                       {
                           var fill = maxMin > 0
                               ? Math.Min((float)((double)b.IstMinuten / (double)maxMin), 1f)
                               : 0f;

                           inner.Item().PaddingBottom(10).Row(row =>
                           {
                               row.ConstantItem(28).Height(28).CornerRadius(14f)
                                  .Background(NavyLight).AlignCenter().AlignMiddle()
                                  .Text(Ini(b.Name)).FontSize(8).Bold().FontColor(White);

                               row.RelativeItem().PaddingLeft(10).AlignMiddle().Column(nc =>
                               {
                                   nc.Item().Row(nr =>
                                   {
                                       nr.AutoItem().Text(b.Name ?? "–").FontSize(9).Bold().FontColor(Slate800);
                                       nr.AutoItem().PaddingLeft(8)
                                         .Text($"{b.Anzahl} Einträge").FontSize(7.5f).FontColor(Slate400);
                                   });
                                   nc.Item().PaddingTop(4).Height(6f).Row(pr =>
                                   {
                                       if (fill > 0.001f)
                                           pr.RelativeItem(fill).Height(6f).Background(Green).CornerRadius(3f);
                                       if (fill < 0.999f)
                                           pr.RelativeItem(1f - fill).Height(6f).Background(Slate100).CornerRadius(3f);
                                   });
                               });

                               row.ConstantItem(55).AlignRight().AlignMiddle()
                                  .Text(H(b.IstMinuten)).FontSize(10).Bold().FontColor(Green);
                           });
                       }
                   });
            });
        }

        // ─── FOOTER ───────────────────────────────────────────────────────────

        private void ComposeFooter(IContainer c)
        {
            c.Background(NavyMid)
             .PaddingHorizontal(28)
             .PaddingVertical(8)
             .Row(row =>
             {
                 row.RelativeItem()
                    .Text($"topfact Pulse  ·  Erstellt am {DateTime.Now:dd.MM.yyyy} um {DateTime.Now:HH:mm} Uhr  ·  Vorgang #{_m.VorgangNr}")
                    .FontSize(7).FontColor("#7AADCC");
                 row.AutoItem().Text(t =>
                 {
                     t.Span("Seite ").FontSize(7).FontColor("#7AADCC");
                     t.CurrentPageNumber().FontSize(7).FontColor(White);
                     t.Span(" / ").FontSize(7).FontColor("#7AADCC");
                     t.TotalPages().FontSize(7).FontColor(White);
                 });
             });
        }

        // ─── Hilfsmethoden ────────────────────────────────────────────────────

        private static string H(decimal m)
        {
            if (m <= 0) return "—";
            if (m < 60) return $"{(int)m} min";
            return $"{Math.Round(m / 60m, 1)} h";
        }

        private static string Ini(string? n)
        {
            if (string.IsNullOrWhiteSpace(n)) return "?";
            return string.Concat(
                n.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                 .Take(2).Select(p => char.ToUpper(p[0])));
        }

        private static string SKat(string? s)
        {
            var t = (s ?? "").ToLowerInvariant();
            if (t.Contains("erledigt") || t.Contains("abgeschlossen") || t.Contains("abgerechnet"))
                return "done";
            if (t.Contains("durchführung") || t.Contains("bearbeitung") || t.Contains("aktiv"))
                return "active";
            return "open";
        }

        private static int Kw(DateTime d) =>
            System.Globalization.CultureInfo.GetCultureInfo("de-DE")
                .Calendar.GetWeekOfYear(d,
                    System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                    DayOfWeek.Monday);
    }
}
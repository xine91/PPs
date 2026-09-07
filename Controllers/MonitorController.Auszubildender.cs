using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using topfact.Pulse.Models;

namespace topfact.Pulse.Controllers
{
    public partial class MonitorController
    {
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Auszubildender(
            int selectedDays = 30,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var entries = await FetchAuszubildenderEntriesFromApiAsync();

            var vm = new AuszubildenderViewModel
            {
                SelectedDays = selectedDays,
                DateFrom = selectedDays == 0 ? dateFrom : null,
                DateTo = selectedDays == 0 ? dateTo : null,
                Entries = entries
            };

            return View(vm);
        }

        private async Task<List<AuszubildenderEntry>> FetchAuszubildenderEntriesFromApiAsync()
        {
            using var http = new HttpClient();

            var accessKey = await GetTopfactAccessKeyAsync(http);
            if (string.IsNullOrWhiteSpace(accessKey))
                return new List<AuszubildenderEntry>();

            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessKey);

            using var response = await http.GetAsync("https://app.topfactcloud.de/0002/topfact6/api/DataView/0005E4E2-1FF1-44CD-927F-78E0CE585B52");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return ParseAuszubildenderEntries(json);
        }

        private static List<AuszubildenderEntry> ParseAuszubildenderEntries(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var rows = FindRowsArray(doc.RootElement);
                if (rows is null)
                    return new List<AuszubildenderEntry>();

                var result = new List<AuszubildenderEntry>();
                foreach (var row in rows.Value.EnumerateArray())
                {
                    var teilnehmerId = GetStringAny(row, "TeilnehmerID") ?? string.Empty;
                    var vorname = GetStringAny(row, "Vorname") ?? string.Empty;
                    var nachname = GetStringAny(row, "Nachname") ?? string.Empty;
                    var internExtern = GetStringAny(row, "Intern_Extern") ?? string.Empty;
                    var ausbildungsgang = GetStringAny(row, "Ausbildungsgang") ?? string.Empty;
                    var beginnStr = GetStringAny(row, "Beginn");
                    var endeStr = GetStringAny(row, "Ende");
                    var geburtsdatumStr = GetStringAny(row, "Geburtsdatum");
                    var dateModifiedStr = GetStringAny(row, "datemodified");

                    DateTime? beginn = DateTime.TryParse(beginnStr, out var b) ? b : null;
                    DateTime? ende = DateTime.TryParse(endeStr, out var e) ? e : null;
                    DateTime? geburtsdatum = DateTime.TryParse(geburtsdatumStr, out var g) ? g : null;
                    DateTime? dateModified = DateTime.TryParse(dateModifiedStr, out var d) ? d : null;

                    result.Add(new AuszubildenderEntry
                    {
                        TeilnehmerId = teilnehmerId,
                        Vorname = vorname,
                        Nachname = nachname,
                        InternExtern = internExtern,
                        Ausbildungsgang = ausbildungsgang,
                        Beginn = beginn,
                        Ende = ende,
                        Geburtsdatum = geburtsdatum,
                        DateModified = dateModified
                    });
                }

                return result.OrderBy(x => x.Nachname).ToList();
            }
            catch
            {
                return new List<AuszubildenderEntry>();
            }
        }
    }
}
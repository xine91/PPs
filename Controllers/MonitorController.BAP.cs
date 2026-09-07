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
        public async Task<IActionResult> BAP(
            int selectedDays = 30,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var entries = await FetchBAPEntriesFromApiAsync();

            var vm = new BAPViewModel
            {
                SelectedDays = selectedDays,
                DateFrom = selectedDays == 0 ? dateFrom : null,
                DateTo = selectedDays == 0 ? dateTo : null,
                Entries = entries
            };

            return View(vm);
        }

        private async Task<List<BAPEntry>> FetchBAPEntriesFromApiAsync()
        {
            using var http = new HttpClient();

            var accessKey = await GetTopfactAccessKeyAsync(http);
            if (string.IsNullOrWhiteSpace(accessKey))
                return new List<BAPEntry>();

            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessKey);

            
            using var response = await http.GetAsync("https://app.topfactcloud.de/0002/topfact6/api/DataView/107a3665-e415-4202-875e-b9665e46f112");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return ParseBAPEntries(json);
        }

        private static List<BAPEntry> ParseBAPEntries(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var rows = FindRowsArray(doc.RootElement);
                if (rows is null)
                    return new List<BAPEntry>();

                var result = new List<BAPEntry>();
                foreach (var row in rows.Value.EnumerateArray())
                {
                    var modul = GetStringAny(row, "modul") ?? string.Empty;
                    var ausbilder = GetStringAny(row, "Ausbilder") ?? string.Empty;

                    var beginnStr = GetStringAny(row, "beginn", "Beginn");
                    var endeStr = GetStringAny(row, "Ende", "ende");
                    DateTime? beginn = DateTime.TryParse(beginnStr, out var b) ? b : null;
                    DateTime? ende = DateTime.TryParse(endeStr, out var e) ? e : null;

                    var teilnehmer = GetStringAny(row, "Teilnehmer") ?? string.Empty;
                    var ausbildungsgang = GetStringAny(row, "Ausbildungsgang") ?? string.Empty;
                    var zeitlicheRichtwerte = GetStringAny(row, "Zeitliche_Richtwerte", "Richtwerte") ?? string.Empty;

                    var dauer = GetDecimalAny(row, "Dauer") ?? 0m;
                    var wert = GetDecimalAny(row, "Wert") ?? 0m;
                    var perc = GetDecimalAny(row, "Perc", "Percentage") ?? 0m;

                    result.Add(new BAPEntry
                    {
                        modul = modul,
                        Ausbilder = ausbilder,
                        Beginn = beginn,
                        Ende = ende,
                        Teilnehmer = teilnehmer,
                        Ausbildungsgang = ausbildungsgang,
                        Zeitliche_Richtwerte = zeitlicheRichtwerte,
                        Dauer = dauer,
                        Wert = wert,
                        Perc = perc
                    });
                }

                return result.OrderBy(x => x.modul).ToList();
            }
            catch
            {
                return new List<BAPEntry>();
            }
        }
    }
}

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
        public async Task<IActionResult> Ausbildung(
            int selectedDays = 30,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var entries = await FetchAusbildungEntriesFromApiAsync();

            var vm = new AusbildungViewModel
            {
                SelectedDays = selectedDays,
                DateFrom = selectedDays == 0 ? dateFrom : null,
                DateTo = selectedDays == 0 ? dateTo : null,
                Entries = entries
            };

            return View(vm);
        }

        private async Task<List<AusbildungEntry>> FetchAusbildungEntriesFromApiAsync()
        {
            using var http = new HttpClient();

            var accessKey = await GetTopfactAccessKeyAsync(http);
            if (string.IsNullOrWhiteSpace(accessKey))
                return new List<AusbildungEntry>();

            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessKey);

            using var response = await http.GetAsync("https://app.topfactcloud.de/0002/topfact6/api/DataView/B67CD038-D6BE-4F10-AF3F-403AF97CE73D");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return ParseAusbildungEntries(json);
        }

        private static List<AusbildungEntry> ParseAusbildungEntries(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var rows = FindRowsArray(doc.RootElement);
                if (rows is null)
                    return new List<AusbildungEntry>();

                var result = new List<AusbildungEntry>();
                foreach (var row in rows.Value.EnumerateArray())
                {
                    var ausbilder = GetStringAny(row, "Ausbilder") ?? string.Empty;

                    result.Add(new AusbildungEntry
                    {
                        Ausbilder = ausbilder
                    });
                }

                return result.OrderBy(x => x.Ausbilder).ToList();
            }
            catch
            {
                return new List<AusbildungEntry>();
            }
        }
    }
}
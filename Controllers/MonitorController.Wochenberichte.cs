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
        public async Task<IActionResult> Wochenberichte(
            int selectedDays = 30,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var entries = await FetchWochenberichteEntriesFromApiAsync();

            var vm = new WochenberichteViewModel
            {
                SelectedDays = selectedDays,
                DateFrom = selectedDays == 0 ? dateFrom : null,
                DateTo = selectedDays == 0 ? dateTo : null,
                Entries = entries
            };

            return View(vm);
        }

        private async Task<List<WochenberichtEntry>> FetchWochenberichteEntriesFromApiAsync()
        {
            using var http = new HttpClient();

            var accessKey = await GetTopfactAccessKeyAsync(http);
            if (string.IsNullOrWhiteSpace(accessKey))
                return new List<WochenberichtEntry>();

            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessKey);

            // TODO: Anpassen der GUID auf die korrekte DataView für Wochenberichte
            using var response = await http.GetAsync("https://app.topfactcloud.de/0002/topfact6/api/DataView/463d26ee-ed72-46d7-a196-b6a599d1e37b");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return ParseWochenberichteEntries(json);
        }

        private static List<WochenberichtEntry> ParseWochenberichteEntries(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var rows = FindRowsArray(doc.RootElement);
                if (rows is null)
                    return new List<WochenberichtEntry>();

                var result = new List<WochenberichtEntry>();
                foreach (var row in rows.Value.EnumerateArray())
                {
                    var azubiName = GetStringAny(row, "azubi_name", "Azubi_Name", "AzubiName", "Name") ?? string.Empty;
                    var ausbildungsbeginnStr = GetStringAny(row, "ausbildungsbeginn", "Ausbildungsbeginn", "Beginn");
                    var erwarteteBerichte = GetIntAny(row, "erwartete_berichte", "Erwartete_Berichte", "ErwarteteBerichte") ?? 0;
                    var vorhandeneBerichte = GetIntAny(row, "vorhandene_berichte", "Vorhandene_Berichte", "VorhandeneBerichte") ?? 0;
                    var fehlendeBerichte = GetIntAny(row, "fehlende_berichte", "Fehlende_Berichte", "FehlendeBerichte") ?? 0;

                    DateTime? ausbildungsbeginn = DateTime.TryParse(ausbildungsbeginnStr, out var b) ? b : null;

                    result.Add(new WochenberichtEntry
                    {
                        AzubiName = azubiName,
                        Ausbildungsbeginn = ausbildungsbeginn,
                        ErwarteteBerichte = erwarteteBerichte,
                        VorhandeneBerichte = vorhandeneBerichte,
                        FehlendeBerichte = fehlendeBerichte
                    });
                }

                return result.OrderBy(x => x.AzubiName).ToList();
            }
            catch
            {
                return new List<WochenberichtEntry>();
            }
        }
    }
}
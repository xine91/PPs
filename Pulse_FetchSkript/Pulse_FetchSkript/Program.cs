using Microsoft.Extensions.Configuration;
using Pulse_FetchSkript.Models;
using Pulse_FetchSkript.Services;

namespace Pulse_FetchSkript;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("[PulseFetch] Start");

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables(prefix: "PULSE_")
            .AddCommandLine(args)
            .Build();

        var db = new DatabaseService(config);

        List<KategorieRecord> categories;
        try
        {
            categories = await db.LoadActiveCategoriesAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[PulseFetch] Fehler beim Laden der Kategorien: {ex.Message}");
            return 3;
        }

        Console.WriteLine($"[PulseFetch] {categories.Count} aktive Kategorien gefunden.");

        if (categories.Count == 0)
        {
            Console.WriteLine("[PulseFetch] Keine aktiven Kategorien vorhanden – nichts zu tun.");
            return 0;
        }

        var totalSaved = 0;
        var totalErrors = 0;

        foreach (var cat in categories)
        {
            Console.WriteLine();
            Console.WriteLine($"=== Kategorie {cat.KategorieID} – {cat.Title} ============================================");

            if (cat.ConnectorID is null)
            {
                Console.WriteLine($"[PulseFetch]   Kein ConnectorID in der Kategorie gesetzt – übersprungen.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(cat.SqlQuery))
            {
                Console.WriteLine($"[PulseFetch]   Keine Sql_query in der Kategorie gesetzt – übersprungen.");
                continue;
            }

            ConnectorRecord? connector;
            try
            {
                connector = await db.LoadConnectorAsync(cat.ConnectorID.Value);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[PulseFetch]   Fehler beim Laden des Connectors #{cat.ConnectorID}: {ex.Message}");
                totalErrors++;
                continue;
            }

            if (connector is null)
            {
                Console.WriteLine($"[PulseFetch]   Connector #{cat.ConnectorID} nicht in Pulse_ConnectorManager gefunden – übersprungen.");
                continue;
            }

            var connStr = connector.ResolveConnectionString();
            string? apiReason = null;
            var apiReq = string.IsNullOrWhiteSpace(connStr) ? connector.ResolveApiRequest(out apiReason) : null;

            if (string.IsNullOrWhiteSpace(connStr) && apiReq is null)
            {
                var sqlRaw = connector.SqlConfig;
                var apiRaw = connector.ApiConfig;

                Console.WriteLine($"[PulseFetch]   Connector #{connector.Id} ({connector.Bezeichnung ?? connector.UserName}) – weder sql_config noch api_config verwertbar. Übersprungen.");
                if (!string.IsNullOrWhiteSpace(sqlRaw))
                {
                    Console.WriteLine($"[PulseFetch]     sql_config-Rohinhalt: {sqlRaw}");
                }
                if (!string.IsNullOrWhiteSpace(apiRaw))
                {
                    Console.WriteLine($"[PulseFetch]     api_config-Rohinhalt: {apiRaw}");
                }
                if (!string.IsNullOrWhiteSpace(apiReason))
                {
                    Console.WriteLine($"[PulseFetch]     Grund der api_config-Ablehnung: {apiReason}");
                }
                continue;
            }

            var modus = !string.IsNullOrWhiteSpace(connStr) ? "SQL" : "API";
            Console.WriteLine($"[PulseFetch]  -> Connector #{connector.Id} ({connector.Bezeichnung ?? connector.UserName}) – Modus: {modus}");

            string datenJson;
            try
            {
                if (!string.IsNullOrWhiteSpace(connStr))
                {
                    datenJson = await db.ExecuteQueryAndSerializeAsync(connStr, cat.SqlQuery!);
                    Console.WriteLine($"[PulseFetch]   Query OK (SQL)");
                }
                else
                {
                    datenJson = await db.ExecuteApiAndSerializeAsync(apiReq);
                    Console.WriteLine($"[PulseFetch]   Query OK (API)");
                }
            }
            catch (Exception ex)
            {
                totalErrors++;
                datenJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    error = true,
                    message = ex.Message,
                    type = ex.GetType().FullName,
                    executedAtUtc = DateTime.UtcNow,
                });
                Console.Error.WriteLine($"[PulseFetch]   Query FEHLER – {ex.Message}");
            }

            // Sql_query ist auch für API-Connectoren sinnvoll (als Bezeichnung/Identifikation)
            var sqlQueryForLog = cat.SqlQuery ?? $"[API] {apiReq?.Method} {apiReq?.Url}";

            try
            {
                var (_, queryId, wasUpdated) = await db.SaveResultAsync(
                    kategorieId: cat.KategorieID,
                    sqlQuery: sqlQueryForLog,
                    datenJson: datenJson);
                totalSaved++;
                var action = wasUpdated ? "überschrieben" : "angelegt";
                Console.WriteLine($"[PulseFetch]   -> gespeichert als QueryID {queryId} ({action}).");
            }
            catch (Exception ex)
            {
                totalErrors++;
                Console.Error.WriteLine($"[PulseFetch]   -> Speichern fehlgeschlagen: {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"[PulseFetch] Fertig. {totalSaved} Ergebnisse gespeichert, {totalErrors} Fehler.");
        return totalErrors == 0 ? 0 : 2;
    }
}

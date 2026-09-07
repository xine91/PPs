# Pulse_FetchSkript

Konsolen-Skript (.NET 10) zum Ausführen der in `Pulse_ConnectorQueries` hinterlegten SQL-Abfragen pro Connector und Speichern der Ergebnisse als JSON in `Pulse_QueryResults2`.

## Ablauf

1. Alle **aktiven Kategorien** (`IsActive = 1`) aus `dbo.Pulse_Kategorie` laden.
2. Pro Kategorie den zugehörigen Connector aus `dbo.Pulse_ConnectorManager` laden (über `ConnectorID` – `Pulse_ConnectorManager.KategorieID` wird in der Praxis NULL gesetzt).
3. Verbindungs-Typ bestimmen:
   - Wenn `sql_config` JSON vorhanden und ein gültiger ConnectionString gebaut werden kann → **SQL-Modus**.
   - Wenn `sql_config` fehlt/leer ist und `api_config` JSON mit URL vorhanden ist → **API-Modus**.
   - Andernfalls Kategorie übersprungen.
4. Im **SQL-Modus**: `Sql_query` aus `Pulse_Kategorie` auf der Connector-Datenbank ausführen.
   Im **API-Modus**: HTTP-Request an die URL aus `api_config` schicken.
5. Ergebnis (Status/Body bzw. Zeilen) als JSON in `dbo.Pulse_QueryResults2` schreiben. Pro `KategorieID` existiert dort höchstens ein Datensatz – existierende Zeilen werden überschrieben, nicht dupliziert.

## Konfiguration

`appsettings.json`:

```json
{
  "PulseDb": {
	"ConnectionString": "Server=...;Database=Pulse;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "QueriesTable": "Pulse_ConnectorQueries"
}
```

Überschreibbar per Umgebungsvariablen (`PULSE_PulseDb__ConnectionString`) oder Kommandozeile.

## Schema-Voraussetzungen

- `dbo.Pulse_Kategorie (KategorieID, IsActive, SortOrder)`
- `dbo.Pulse_ConnectorManager (Id, user_name, sql_config, Bezeichnung, Status, KategorieID)`
- `dbo.Pulse_ConnectorQueries (Id, ConnectorID, KategorieID, Sql_query, SortOrder)` – wird beim ersten Start automatisch angelegt, falls nicht vorhanden. Bei bestehender Tabelle wird die Spalte `KategorieID` und der Index idempotent ergänzt.
- `dbo.Pulse_QueryResults2 (QueryID IDENTITY, KategorieID, Sql_query, Daten NVARCHAR(MAX), created_at)`

`sql_config` muss ein JSON mit den Verbindungs-Komponenten enthalten, z.B.:

```json
{
  "server": "sqlsrv01",
  "database": "HR",
  "userId": "ro",
  "password": "***",
  "encrypt": "False",
  "trustServerCertificate": "True",
  "connectionTimeout": "30"
}
```

Der fertige ConnectionString wird automatisch gebaut:
`Server=sqlsrv01;Database=HR;User Id=ro;Password=***;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;`

`api_config` muss ein JSON mit API-Endpoint enthalten, z.B.:

```json
{
  "type": "API",
  "endpointUrl": "https://jsonplaceholder.typicode.com/posts/1",
  "httpMethod": "GET",
  "requestBody": "",
  "authType": "Bearer Token",
  "customHeaders": {
    "X-Custom": "value"
  },
  "contentType": "application/json"
}
```

**Unterstützte Auth-Typen** (`authType`):
- `Bearer Token` → Header `Authorization: Bearer <token|bearerToken|apiKey>`
- `Basic Auth` → Header `Authorization: Basic base64(username:password)` (Credentials aus `username`/`user` und `password`)
- `Api Key` → Header `X-Api-Key: <apiKey>`
- `No Auth` / leer → kein Auth-Header

Methoden: `GET`, `POST`, `PUT`, `DELETE`, `PATCH`, `HEAD`. Default ohne Methode = `GET`.

Content-Type wird automatisch erkannt (JSON wenn Body mit `{` oder `[` beginnt) oder aus dem Feld `contentType` gelesen.

## Build & Run

```powershell
dotnet build
dotnet run --project Pulse_FetchSkript
```

Exit-Codes:
- `0` – alles erfolgreich
- `2` – mindestens ein Query- oder Speicherfehler
- `3` – Kategorien konnten nicht geladen werden
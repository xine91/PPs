using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Pulse_FetchSkript.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pulse_FetchSkript.Services;

public sealed class DatabaseService
{
    private readonly string _pulseConnectionString;

    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = false,
        Converters = { new SqlValueJsonConverter() },
    };

    public DatabaseService(IConfiguration config)
    {
        _pulseConnectionString =
            config["PulseDb:ConnectionString"]
            ?? throw new InvalidOperationException("PulseDb:ConnectionString fehlt in der Konfiguration.");
    }

    public async Task<List<ConnectorRecord>> LoadActiveConnectorsAsync(CancellationToken ct = default)
    {
        var list = new List<ConnectorRecord>();

        const string sql = @"
SELECT Id, user_name, connector_type, sql_config, api_config,
       Bezeichnung, Status, KategorieID
FROM dbo.Pulse_ConnectorManager
WHERE (sql_config IS NOT NULL AND LTRIM(RTRIM(sql_config)) <> '')
   OR (api_config IS NOT NULL AND LTRIM(RTRIM(api_config)) <> '');";

        await using var conn = new SqlConnection(_pulseConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(ReadConnector(reader));
        }
        return list;
    }

    public async Task<List<ConnectorRecord>> LoadConnectorsByCategoryAsync(int kategorieId, CancellationToken ct = default)
    {
        var list = new List<ConnectorRecord>();

        const string sql = @"
SELECT Id, user_name, connector_type, sql_config, api_config,
       Bezeichnung, Status, KategorieID
FROM dbo.Pulse_ConnectorManager
WHERE KategorieID = @kid
  AND ((sql_config IS NOT NULL AND LTRIM(RTRIM(sql_config)) <> '')
    OR (api_config IS NOT NULL AND LTRIM(RTRIM(api_config)) <> ''));";

        await using var conn = new SqlConnection(_pulseConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@kid", kategorieId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(ReadConnector(reader));
        }
        return list;
    }

    public async Task<List<int>> LoadActiveCategoryIdsAsync(CancellationToken ct = default)
    {
        var ids = new List<int>();

        const string sql = @"
SELECT KategorieID
FROM dbo.Pulse_Kategorie
WHERE IsActive = 1
ORDER BY ISNULL(SortOrder, 0), KategorieID;";

        await using var conn = new SqlConnection(_pulseConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            ids.Add(reader.GetInt32(0));
        }
        return ids;
    }

    public async Task<List<KategorieRecord>> LoadActiveCategoriesAsync(CancellationToken ct = default)
    {
        var list = new List<KategorieRecord>();

        const string sql = @"
SELECT KategorieID, GruppeID, Title, IconCss,
       RouteBereich, BadgeText, IsActive, SortOrder,
       ConnectorID, Sql_query
FROM dbo.Pulse_Kategorie
WHERE IsActive = 1
ORDER BY ISNULL(SortOrder, 0), KategorieID;";

        await using var conn = new SqlConnection(_pulseConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new KategorieRecord
            {
                KategorieID = reader.GetInt32(reader.GetOrdinal("KategorieID")),
                GruppeID = reader.GetInt32(reader.GetOrdinal("GruppeID")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                IconCss = reader.IsDBNull(reader.GetOrdinal("IconCss"))
                    ? null : reader.GetString(reader.GetOrdinal("IconCss")),
                RouteBereich = reader.IsDBNull(reader.GetOrdinal("RouteBereich"))
                    ? null : reader.GetString(reader.GetOrdinal("RouteBereich")),
                BadgeText = reader.IsDBNull(reader.GetOrdinal("BadgeText"))
                    ? null : reader.GetString(reader.GetOrdinal("BadgeText")),
                IsActive = !reader.IsDBNull(reader.GetOrdinal("IsActive")) && reader.GetBoolean(reader.GetOrdinal("IsActive")),
                SortOrder = reader.IsDBNull(reader.GetOrdinal("SortOrder"))
                    ? null : reader.GetInt32(reader.GetOrdinal("SortOrder")),
                ConnectorID = reader.IsDBNull(reader.GetOrdinal("ConnectorID"))
                    ? null : reader.GetInt32(reader.GetOrdinal("ConnectorID")),
                SqlQuery = reader.IsDBNull(reader.GetOrdinal("Sql_query"))
                    ? null : reader.GetString(reader.GetOrdinal("Sql_query")),
            });
        }
        return list;
    }

    private static ConnectorRecord ReadConnector(SqlDataReader reader)
    {
        return new ConnectorRecord
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            UserName = reader.GetString(reader.GetOrdinal("user_name")),
            ConnectorType = reader.IsDBNull(reader.GetOrdinal("connector_type"))
                ? null : reader.GetString(reader.GetOrdinal("connector_type")),
            SqlConfig = reader.IsDBNull(reader.GetOrdinal("sql_config"))
                ? null : reader.GetString(reader.GetOrdinal("sql_config")),
            ApiConfig = reader.IsDBNull(reader.GetOrdinal("api_config"))
                ? null : reader.GetString(reader.GetOrdinal("api_config")),
            Bezeichnung = reader.IsDBNull(reader.GetOrdinal("Bezeichnung"))
                ? null : reader.GetString(reader.GetOrdinal("Bezeichnung")),
            Status = reader.IsDBNull(reader.GetOrdinal("Status"))
                ? null : reader.GetString(reader.GetOrdinal("Status")),
            KategorieID = reader.IsDBNull(reader.GetOrdinal("KategorieID"))
                ? null : reader.GetInt32(reader.GetOrdinal("KategorieID")),
        };
    }

    public async Task<ConnectorRecord?> LoadConnectorAsync(int connectorId, CancellationToken ct = default)
    {
        const string sql = @"
SELECT Id, user_name, connector_type, sql_config, api_config,
       Bezeichnung, Status, KategorieID
FROM dbo.Pulse_ConnectorManager
WHERE Id = @id;";

        await using var conn = new SqlConnection(_pulseConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", connectorId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadConnector(reader) : null;
    }

    public async Task<(int RowsAffected, long QueryID, bool WasUpdated)> SaveResultAsync(
        int? kategorieId,
        string sqlQuery,
        string datenJson,
        CancellationToken ct = default)
    {
        // Upsert pro KategorieID: existiert bereits eine Zeile mit dieser KategorieID,
        // wird sie aktualisiert; sonst wird eine neue eingefügt.
        // MATCHED liefert die vorhandene QueryID, OUTPUT liefert die (ggf. neu erzeugte) QueryID.
        const string sql = @"
DECLARE @existingId BIGINT;

SELECT TOP (1) @existingId = QueryID
FROM dbo.Pulse_QueryResults2 WITH (UPDLOCK, HOLDLOCK)
WHERE KategorieID = @kid
ORDER BY QueryID DESC;

IF @existingId IS NULL
BEGIN
    INSERT INTO dbo.Pulse_QueryResults2 (KategorieID, Sql_query, Daten, created_at)
    VALUES (@kid, @qry, @dat, SYSUTCDATETIME());

    SET @existingId = SCOPE_IDENTITY();
    SELECT @existingId AS QueryID, CAST(0 AS BIT) AS WasUpdated;
END
ELSE
BEGIN
    UPDATE dbo.Pulse_QueryResults2
    SET Sql_query  = @qry,
        Daten      = @dat,
        created_at = SYSUTCDATETIME()
    WHERE QueryID = @existingId;

    SELECT @existingId AS QueryID, CAST(1 AS BIT) AS WasUpdated;
END";

        await using var conn = new SqlConnection(_pulseConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@kid", (object?)kategorieId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@qry", sqlQuery);
        cmd.Parameters.AddWithValue("@dat", datenJson);

        long queryId = 0;
        bool wasUpdated = false;

        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            if (await reader.ReadAsync(ct))
            {
                queryId = Convert.ToInt64(reader.GetValue(reader.GetOrdinal("QueryID")));
                wasUpdated = Convert.ToBoolean(reader.GetValue(reader.GetOrdinal("WasUpdated")));
            }
        }

        return (1, queryId, wasUpdated);
    }

    public async Task<string> ExecuteQueryAndSerializeAsync(
        string connectorConnectionString,
        string sqlQuery,
        CancellationToken ct = default)
    {
        var rows = new List<Dictionary<string, object?>>();

        await using var conn = new SqlConnection(connectorConnectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new SqlCommand(sqlQuery, conn);
        cmd.CommandTimeout = 120;

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var fieldCount = reader.FieldCount;

        while (await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, object?>(fieldCount, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < fieldCount; i++)
            {
                var name = reader.GetName(i);
                var val = reader.IsDBNull(i) ? null : reader.GetValue(i);
                row[name] = val;
            }
            rows.Add(row);
        }

        var payload = new
        {
            rowCount = rows.Count,
            executedAtUtc = DateTime.UtcNow,
            rows,
        };
        return JsonSerializer.Serialize(payload, DefaultJsonOptions);
    }

    private static readonly HttpClient SharedHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(120),
    };

    public async Task<string> ExecuteApiAndSerializeAsync(
        ApiRequest request,
        CancellationToken ct = default)
    {
        using var httpReq = new HttpRequestMessage(request.Method, request.Url);

        var body = request.Body ?? request.BodyRaw;
        var hasBody = !string.IsNullOrEmpty(body);

        if (hasBody)
        {
            var contentType = request.ContentType;
            if (string.IsNullOrWhiteSpace(contentType))
            {
                // Heuristik: JSON wenn Body mit { oder [ beginnt, sonst plain text
                var trimmed = body!.TrimStart();
                contentType = (trimmed.StartsWith("{") || trimmed.StartsWith("["))
                    ? "application/json"
                    : "text/plain";
            }
            httpReq.Content = new StringContent(body!, Encoding.UTF8, contentType);
        }

        foreach (var kv in request.Headers)
        {
            if (httpReq.Headers.TryAddWithoutValidation(kv.Key, kv.Value)) continue;

            // Falls Header nicht zur Request-Message passt, an Content packen
            if (httpReq.Content is null)
            {
                httpReq.Content = new StringContent(string.Empty);
            }
            httpReq.Content.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
        }

        using var httpResp = await SharedHttpClient.SendAsync(
            httpReq,
            HttpCompletionOption.ResponseContentRead,
            ct);

        var statusCode = (int)httpResp.StatusCode;
        var isJson = httpResp.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true;
        var bodyText = await httpResp.Content.ReadAsStringAsync(ct);

        object parsedBody;
        if (isJson && !string.IsNullOrWhiteSpace(bodyText))
        {
            try
            {
                parsedBody = JsonSerializer.Deserialize<JsonElement>(bodyText);
            }
            catch
            {
                parsedBody = bodyText;
            }
        }
        else
        {
            parsedBody = bodyText;
        }

        var payload = new
        {
            statusCode,
            isSuccess = httpResp.IsSuccessStatusCode,
            contentType = httpResp.Content.Headers.ContentType?.ToString(),
            executedAtUtc = DateTime.UtcNow,
            body = parsedBody,
        };
        return JsonSerializer.Serialize(payload, DefaultJsonOptions);
    }
}
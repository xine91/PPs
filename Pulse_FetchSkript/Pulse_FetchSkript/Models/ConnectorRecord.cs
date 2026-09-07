using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pulse_FetchSkript.Services;

namespace Pulse_FetchSkript.Models;

public sealed class ConnectorRecord
{
    [JsonPropertyName("Id")]
    public int Id { get; set; }

    [JsonPropertyName("user_name")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("connector_type")]
    public string? ConnectorType { get; set; }

    [JsonPropertyName("sql_config")]
    public string? SqlConfig { get; set; }

    [JsonPropertyName("Bezeichnung")]
    public string? Bezeichnung { get; set; }

    [JsonPropertyName("Status")]
    public string? Status { get; set; }

    [JsonPropertyName("KategorieID")]
    public int? KategorieID { get; set; }

    public SqlConfigPayload? ParsedSqlConfig =>
        string.IsNullOrWhiteSpace(SqlConfig)
            ? null
            : JsonHelper.TryDeserialize<SqlConfigPayload>(
                SqlConfig,
                options: null,
                out var parsed,
                out _)
                ? parsed
                : null;

    public string? ResolveConnectionString()
    {
        var p = ParsedSqlConfig;
        if (p is null) return null;

        // Direkter ConnectionString hat Vorrang (falls vorhanden)
        if (!string.IsNullOrWhiteSpace(p.ConnectionString))
            return p.ConnectionString;
        if (!string.IsNullOrWhiteSpace(p.ConnectionStringRaw))
            return p.ConnectionStringRaw;

        // Normalisierte Komponenten einsammeln
        var server = FirstNonEmpty(p.Server, p.ServerRaw, p.DataSource);
        var database = FirstNonEmpty(p.Database, p.DatabaseRaw, p.InitialCatalog);
        var userId = FirstNonEmpty(p.UserId, p.UserIdLower, p.Uid, p.UserIdRaw);
        var password = FirstNonEmpty(p.Password, p.Pwd);

        var integrated = ParseBool(p.IntegratedSecurity)
                       ?? ParseBool(p.IntegratedSecurityRaw);

        var encrypt = ParseBool(p.EncryptText)
                    ?? ParseBool(p.EncryptRawText);

        var trustCert = ParseBool(p.TrustServerCertificateText)
                       ?? ParseBool(p.TrustServerCertificateRawText);

        var timeout = ParseInt(p.ConnectionTimeoutText)
                    ?? ParseInt(p.ConnectTimeoutText);

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
            return null;

        var sb = new StringBuilder();
        sb.Append("Server=").Append(server).Append(';');
        sb.Append("Database=").Append(database).Append(';');

        if (integrated == true)
        {
            sb.Append("Integrated Security=True;");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(userId)) sb.Append("User Id=").Append(userId).Append(';');
            if (!string.IsNullOrWhiteSpace(password)) sb.Append("Password=").Append(password).Append(';');
        }

        if (encrypt is not null)
            sb.Append("Encrypt=").Append(encrypt.Value ? "True" : "False").Append(';');

        if (trustCert is not null)
            sb.Append("TrustServerCertificate=").Append(trustCert.Value ? "True" : "False").Append(';');

        if (timeout is not null)
            sb.Append("Connection Timeout=").Append(timeout.Value).Append(';');

        return sb.ToString();
    }

    [JsonPropertyName("api_config")]
    public string? ApiConfig { get; set; }

    public ApiConfigPayload? ParsedApiConfig =>
        string.IsNullOrWhiteSpace(ApiConfig)
            ? null
            : JsonHelper.TryDeserialize<ApiConfigPayload>(
                ApiConfig,
                options: null,
                out var parsed,
                out _)
                ? parsed
                : null;

    public ApiRequest? ResolveApiRequest() => ResolveApiRequest(out _);

    public ApiRequest? ResolveApiRequest(out string? reason)
    {
        reason = null;

        if (string.IsNullOrWhiteSpace(ApiConfig))
        {
            reason = "api_config ist leer.";
            return null;
        }

        if (!JsonHelper.TryDeserialize<ApiConfigPayload>(
                ApiConfig, options: null, out var parsed, out var parseError) || parsed is null)
        {
            reason = $"api_config konnte nicht als JSON geparst werden: {parseError ?? "unbekannter Fehler"}";
            return null;
        }

        var p = parsed;

        // URL: camelCase + gängige Aliasse
        var url = FirstNonEmpty(
            p.EndpointUrl,
            p.Endpoint,
            p.Url,
            p.EndpointURL);
        if (string.IsNullOrWhiteSpace(url))
        {
            reason = "api_config enthält keine endpointUrl/Url.";
            return null;
        }

        var authType = (p.AuthType ?? string.Empty).Trim();
        var authTypeLower = authType.ToLowerInvariant();

        var hasBody = !string.IsNullOrWhiteSpace(p.RequestBody);
        var hasCustomHeaders = p.CustomHeaders is { Count: > 0 };

        // Wenn weder authType noch requestBody noch customHeaders vorhanden sind,
        // ist die API-Konfiguration wertlos -> ueberspringen.
        if (string.IsNullOrWhiteSpace(authType) && !hasBody && !hasCustomHeaders)
        {
            reason = "api_config ohne verwertbare Bestandteile: weder authType, noch requestBody, noch customHeaders gesetzt.";
            return null;
        }

        // Methode: camelCase + gängige Aliasse
        var method = (FirstNonEmpty(
                          p.HttpMethod,
                          p.Method,
                          p.HTTPMethod) ?? "GET").Trim().ToUpperInvariant();
        var httpMethod = method switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "DELETE" => HttpMethod.Delete,
            "PATCH" => HttpMethod.Patch,
            "HEAD" => HttpMethod.Head,
            _ => new HttpMethod(method),
        };

        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Auth-Header je nach authType
        switch (authTypeLower)
        {
            case "bearer token":
            case "bearer":
            case "token":
                var token = FirstNonEmpty(p.BearerToken, p.Token, p.ApiKey);
                if (!string.IsNullOrWhiteSpace(token))
                    headers["Authorization"] = $"Bearer {token}";
                break;

            case "basic auth":
            case "basic":
                var user = FirstNonEmpty(p.Username, p.UserAlias);
                var pass = p.Password;
                if (!string.IsNullOrWhiteSpace(user))
                {
                    var raw = $"{user}:{pass}";
                    var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
                    headers["Authorization"] = $"Basic {b64}";
                }
                break;

            case "api key":
            case "apikey":
                if (!string.IsNullOrWhiteSpace(p.ApiKey))
                    headers["X-Api-Key"] = p.ApiKey;
                break;

            case "no auth":
            case "":
            case "none":
                // keine Auth-Header
                break;
        }

        // customHeaders mergen
        if (p.CustomHeaders is not null)
        {
            foreach (var kv in p.CustomHeaders)
            {
                if (!string.IsNullOrWhiteSpace(kv.Key))
                    headers[kv.Key] = kv.Value ?? string.Empty;
            }
        }

        return new ApiRequest(
            Url: url!,
            Method: httpMethod,
            Headers: headers,
            Body: p.RequestBody,
            BodyRaw: null,
            ContentType: p.ContentType);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
            if (!string.IsNullOrWhiteSpace(v)) return v;
        return null;
    }

    private static bool? ParseBool(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (bool.TryParse(raw, out var b)) return b;
        var t = raw.Trim().ToLowerInvariant();
        return t switch
        {
            "true" or "1" or "yes" or "y" or "ja" => true,
            "false" or "0" or "no" or "n" or "nein" => false,
            _ => null,
        };
    }

    private static int? ParseInt(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return int.TryParse(raw, out var i) ? i : null;
    }
}

public sealed class SqlConfigPayload
{
    [JsonPropertyName("connectionString")]
    public string? ConnectionString { get; set; }

    [JsonPropertyName("ConnectionString")]
    public string? ConnectionStringRaw { get; set; }

    [JsonPropertyName("server")]
    public string? Server { get; set; }

    [JsonPropertyName("Server")]
    public string? ServerRaw { get; set; }

    [JsonPropertyName("data source")]
    public string? DataSource { get; set; }

    [JsonPropertyName("database")]
    public string? Database { get; set; }

    [JsonPropertyName("Database")]
    public string? DatabaseRaw { get; set; }

    [JsonPropertyName("initial catalog")]
    public string? InitialCatalog { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    [JsonPropertyName("user id")]
    public string? UserIdLower { get; set; }

    [JsonPropertyName("uid")]
    public string? Uid { get; set; }

    [JsonPropertyName("User Id")]
    public string? UserIdRaw { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("pwd")]
    public string? Pwd { get; set; }

    [JsonPropertyName("integratedSecurity")]
    public string? IntegratedSecurity { get; set; }

    [JsonPropertyName("Integrated Security")]
    public string? IntegratedSecurityRaw { get; set; }

    [JsonPropertyName("encrypt")]
    public string? EncryptText { get; set; }

    [JsonPropertyName("Encrypt")]
    public string? EncryptRawText { get; set; }

    [JsonPropertyName("trustServerCertificate")]
    public string? TrustServerCertificateText { get; set; }

    [JsonPropertyName("TrustServerCertificate")]
    public string? TrustServerCertificateRawText { get; set; }

    [JsonPropertyName("connectionTimeout")]
    public string? ConnectionTimeoutText { get; set; }

    [JsonPropertyName("Connect Timeout")]
    public string? ConnectTimeoutText { get; set; }
}

public sealed class QueryRecord
{
    public int Id { get; set; }
    public int ConnectorID { get; set; }
    public int? KategorieID { get; set; }
    public string SqlQuery { get; set; } = string.Empty;
}

public sealed class KategorieRecord
{
    public int KategorieID { get; set; }
    public int GruppeID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? IconCss { get; set; }
    public string? RouteBereich { get; set; }
    public string? BadgeText { get; set; }
    public bool IsActive { get; set; }
    public int? SortOrder { get; set; }
    public int? ConnectorID { get; set; }
    public string? SqlQuery { get; set; }
}

public sealed class ApiConfigPayload
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("endpointUrl")]
    public string? EndpointUrl { get; set; }

    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("endpointURL")]
    public string? EndpointURL { get; set; }

    [JsonPropertyName("httpMethod")]
    public string? HttpMethod { get; set; }

    [JsonPropertyName("method")]
    public string? Method { get; set; }

    [JsonPropertyName("HTTPMethod")]
    public string? HTTPMethod { get; set; }

    [JsonPropertyName("requestBody")]
    public string? RequestBody { get; set; }

    [JsonPropertyName("authType")]
    public string? AuthType { get; set; }

    [JsonPropertyName("customHeaders")]
    public Dictionary<string, string>? CustomHeaders { get; set; }

    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    // Optionale Auth-Credentials (falls in derselben Config mitgepflegt)
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("user")]
    public string? UserAlias { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("bearerToken")]
    public string? BearerToken { get; set; }

    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }
}

public sealed record ApiRequest(
    string Url,
    HttpMethod Method,
    IDictionary<string, string> Headers,
    string? Body,
    string? BodyRaw,
    string? ContentType);
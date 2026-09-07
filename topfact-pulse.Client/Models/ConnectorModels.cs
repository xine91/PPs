using System.Collections.Generic;

namespace topfact.Pulse.Models
{
    /// <summary>
    /// Konfiguration für einen einzelnen Connector
    /// </summary>
    public class ConnectorConfig
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "tf6", "api", "sql", "manual"
        public string Status { get; set; } = "off"; // "ok", "warn", "off"
        public string StatusText { get; set; } = "nicht konfiguriert";
        public bool IsActive { get; set; } = true;
        public Dictionary<string, object> Configuration { get; set; } = new();
        public long? LastSyncTime { get; set; }
        public string? LastSyncStatus { get; set; }
    }

    /// <summary>
    /// topfact6 ERP spezifische Konfiguration
    /// </summary>
    public class Tf6Config
    {
        public string AuthEndpoint { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ApiEndpoint { get; set; } = string.Empty;
        public string AccessKeyHeaderName { get; set; } = "X-AccessKey";
        public string? AccessKey { get; set; } // Auto-generated
    }

    /// <summary>
    /// Generische API Connector Konfiguration
    /// </summary>
    public class ApiConnectorConfig
    {
        public string EndpointUrl { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = "GET"; // GET, POST, PUT, PATCH
        public string AuthType { get; set; } = "Bearer"; // Bearer, Basic, None
        public string? Token { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public Dictionary<string, string> CustomHeaders { get; set; } = new();
    }

    /// <summary>
    /// SQL Database Connector Konfiguration
    /// </summary>
    public class SqlConnectorConfig
    {
        public string Server { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Encrypt { get; set; } = "False";
        public string TrustServerCertificate { get; set; } = "True";
        public int ConnectionTimeout { get; set; } = 30;

        public string GetConnectionString()
        {
            return $"Server={Server};Database={Database};User Id={UserId};Password={Password};Encrypt={Encrypt};TrustServerCertificate={TrustServerCertificate};Connection Timeout={ConnectionTimeout};";
        }
    }

    /// <summary>
    /// Manueller Input Connector
    /// </summary>
    public class ManualConnectorConfig
    {
        public string Description { get; set; } = string.Empty;
        public string Field1Label { get; set; } = string.Empty;
        public string Field2Label { get; set; } = string.Empty;
        public string RequiredFields { get; set; } = "all"; // all, none, custom
        public string ConfirmationText { get; set; } = string.Empty;
    }

    /// <summary>
    /// Wrapper für alle Connector-Konfigurationen
    /// </summary>
    public class ConnectorsSettings
    {
        public Tf6Config? Tf6 { get; set; }
        public ApiConnectorConfig? Api { get; set; }
        public SqlConnectorConfig? Sql { get; set; }
        public ManualConnectorConfig? Manual { get; set; }
    }
}

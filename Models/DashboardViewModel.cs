namespace topfact.Pulse.Models
{
    public class DashboardViewModel
    {
        public int TotalServers { get; set; }
        public int OnlineServers { get; set; }
        public int OfflineServers { get; set; }
        public int ServersWithErrors { get; set; }

        public List<ServerAnalyticsRow> Servers { get; set; } = new();
    }

    public class ServerStatusItem
    {
        public string Kunde { get; set; } = string.Empty;
        public string Deployment { get; set; } = string.Empty; // "Cloud" | "OnPremise"
        public DateTime? LetzteAktualisierung { get; set; }
        public string Servername { get; set; } = string.Empty;

        // Status dots: 0 = grau, 1 = grün, 2 = gelb, 3 = rot
        public int Gesamtstatus { get; set; }
        public int EmailDailyStatus { get; set; }
        public int ActiveUsersStatus { get; set; }
        public int TfaCountTodayStatus { get; set; }

        public int Fehler { get; set; } = 0;
        public int BackupStatus { get; set; } // 0=grau, 1=grün, 2=gelb, 3=rot
    }
}
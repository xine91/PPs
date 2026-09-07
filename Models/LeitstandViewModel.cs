namespace topfact.Pulse.Models
{
    public class LeitstandViewModel
    {
        public List<string> Organizations { get; set; } = new();
        public string? SelectedOrg { get; set; }
        public ServersSnaphot? LatestSnapshot { get; set; }
        public List<ServersSnaphot> History { get; set; } = new();

        public int TenantCount { get; set; }
        public int TenantsWithSnapshotToday { get; set; }
        public int ActiveUsersWeekTotal { get; set; }
        public int ErrorsTodayTotal { get; set; }
        public decimal AvgCpuUsagePercent { get; set; }
        public int DocsLast30DaysTotal { get; set; }
        public long TotalDocuments { get; set; }
        public long TotalFiles { get; set; }
        public int M365MailsTodayTotal { get; set; }
        public decimal AvgMemoryUsagePercent { get; set; }
        public decimal AvgDiskUsagePercent { get; set; }
        public decimal TotalDatabaseSizeMB { get; set; }
        public int TfaCountTodayTotal { get; set; }
        public int ProcessesNotRespondingTotal { get; set; }
        public int LicensesExpiringSoon { get; set; }
        public double AvgSystemUptimeHours { get; set; }
    }
}

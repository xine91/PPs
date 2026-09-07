using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace topfact.Pulse.Models
{
    [Table("ServerAnalyticsSnapshot", Schema = "dbo")]
    public class ServersSnaphot
    {
        [Key]
        public long EventID { get; set; }
        public DateTime? SnapshotTimeUTC { get; set; }

        // topfact Application Metrics
        public int? TfaCountToday { get; set; }
        public decimal? TfaFileSizeTodayMB { get; set; }
        public long? TfaTotalDocs { get; set; }
        public int? TfaDocsLast30Days { get; set; }
        public long? TfaTotalFiles { get; set; }
        public int? TfaActiveUsersWeek { get; set; }

        // Versions
        public string? TfaSQLServerVersion { get; set; }
        public string? TfaDBVersion { get; set; }
        public string? TfaJobServerVersion { get; set; }
        public string? TfaOcrServerVersion { get; set; }
        public string? TfaImageServerVersion { get; set; }
        public string? TfaMyWorkVersion { get; set; }
        public string? TfaAdministrationVersion { get; set; }

        // Database & Backup
        public decimal? TfaDatabaseSizeMB { get; set; }
        public DateTime? TfaLastBackupDateArchiv { get; set; }
        public DateTime? TfaLastBackupDateTopfact6 { get; set; }

        // License
        public int? TfaLicenseCount { get; set; }
        public DateTime? TfaLicenseEnd { get; set; }

        // Errors & Mail
        public int? TfaTFLogErrorsToday { get; set; }
        public int? TF_M365_Mails_today { get; set; }

        // Identity
        public string? CreatedBy { get; set; }
        public string? OrgName { get; set; }
        public string? MachineName { get; set; }
        public string? UserName { get; set; }

        // OS Info
        public string? OSVersion { get; set; }
        public string? OSArchitecture { get; set; }
        public string? DotNetVersion { get; set; }
        public int? SystemUptimeHours { get; set; }

        // CPU
        public string? CpuName { get; set; }
        public int? CpuCores { get; set; }
        public int? CpuLogicalProcessors { get; set; }
        public decimal? CpuUsagePercent { get; set; }

        // Memory
        public int? MemoryTotalMB { get; set; }
        public int? MemoryFreeMB { get; set; }
        public decimal? MemoryUsagePercent { get; set; }

        // Disk
        public int? DiskTotalGB { get; set; }
        public int? DiskFreeGB { get; set; }
        public decimal? DiskUsagePercent { get; set; }

        // Network
        public string? Hostname { get; set; }
        public string? IPAddress { get; set; }

        // Processes
        public int? ProcessTotal { get; set; }
        public int? ProcessResponding { get; set; }
        public int? ProcessNotResponding { get; set; }
        public string? ProcessTopCpu { get; set; }
        public string? ProcessTopMemory { get; set; }
        public decimal? ProcessAvgMemoryMB { get; set; }
    }
}

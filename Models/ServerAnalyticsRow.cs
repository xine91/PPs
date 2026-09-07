namespace topfact.Pulse.Models
{
    public class ServerAnalyticsRow
    {
        public string Name1 { get; set; }
        public int GesamtStatusCode { get; set; }
        public int ActiveUsersStatusCode { get; set; }
        public int EmailDailyStatusCode { get; set; }

        public int VersionStatusCode { get; set; }  
        public int DiskStatusCode { get; set; }     
        public int CpuStatusCode { get; set; }      
        public int BackupStatusCode { get; set; }  
    }
}
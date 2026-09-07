using System;
using System.Collections.Generic;

namespace topfact.Pulse.Models
{
    public class UserLoginEntry
    {
        public string Customer { get; set; } = string.Empty;
        public string Clientname { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Domainname { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string AppVersion { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
    }

    public class UserLoginViewModel
    {
        public List<string> AvailableCustomers { get; set; } = new();
        public List<string> AvailableAppNames { get; set; } = new();
        public string? SelectedCustomer { get; set; }
        public string? SelectedAppName { get; set; }
        public int SelectedDays { get; set; } = 365;
        public List<UserLoginEntry> Logins { get; set; } = new();

        public int TotalLogins { get; set; }
        public int LoginsToday { get; set; }
        public int UniqueUsers { get; set; }

        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}
using System;
using System.Collections.Generic;

namespace topfact.Pulse.Models
{
    public class MyWorkViewModel
    {
        public List<string> AvailableCustomers { get; set; } = new();
        public string? SelectedCustomer { get; set; }
        public int SelectedDays { get; set; } = 30;
        public List<UserLoginEntry> Logins { get; set; } = new();

        public int TotalLogins { get; set; }
        public int LoginsToday { get; set; }
        public int UniqueUsers { get; set; }

        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}

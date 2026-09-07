using System;
using System.Collections.Generic;
using System.Linq;

namespace topfact.Pulse.Models
{
    public class FormRecognizerStat
    {
        public string OrgName { get; set; } = string.Empty;
        public int RecognizedDocuments { get; set; }
        public int Volume { get; set; }
    }

    public class FormRecognizerViewModel
    {
        public List<FormRecognizerStat> Stats { get; set; } = new();
        public int SelectedDays { get; set; } = 30;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int TotalDocuments => Stats.Sum(s => s.RecognizedDocuments);
        public int TotalVolume => Stats.Sum(s => s.Volume);
        public int TenantCount => Stats.Count;
        public double AveragePerTenant => TenantCount == 0 ? 0 : Math.Round((double)TotalDocuments / TenantCount, 2);
    }
}

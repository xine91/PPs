using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using topfact.Pulse.Data;

namespace topfact.Pulse.Controllers
{
    public partial class MonitorController : Controller
    {
        private readonly AnalyticsDbContext _db;
        private readonly AppDbContext _appDb;
        private readonly IConfiguration _configuration;

        public MonitorController(AnalyticsDbContext db, AppDbContext appDb, IConfiguration configuration)
        {
            _db = db;
            _appDb = appDb;
            _configuration = configuration;
        }

        private static (DateTime Start, DateTime? End) GetRange(
            int selectedDays, DateTime? dateFrom, DateTime? dateTo, bool includeDefaultEnd = false)
        {
            if (selectedDays == 0 && dateFrom.HasValue)
            {
                var start = dateFrom.Value.Date;
                var end = dateTo.HasValue ? dateTo.Value.Date.AddDays(1) : DateTime.Now.AddDays(1);
                return (start, end);
            }

            var startDefault = DateTime.Now.AddDays(-selectedDays);
            if (includeDefaultEnd)
                return (startDefault, DateTime.Now.AddDays(1));

            return (startDefault, null);
        }
    }
}
using Microsoft.EntityFrameworkCore;
using topfact.Pulse.Data;
using topfact.Pulse.Models;

namespace topfact.Pulse.Services
{
    /// <summary>Service für Audit-Protokollierung (Login-Events)</summary>
    public interface IPulseAuditService
    {
        Task LogLoginAsync(string username, bool success, string? ipAddress = null);
        Task<IEnumerable<PulseLogin>> GetRecentLoginsAsync(string username, int days = 7);
        Task<IEnumerable<PulseLogin>> GetFailedLoginsAsync(int days = 7);
    }

    public class PulseAuditService : IPulseAuditService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PulseAuditService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>Login-Ereignis protokollieren</summary>
        public async Task LogLoginAsync(string username, bool success, string? ipAddress = null)
        {
            // IP-Adresse abrufen falls nicht bereitgestellt
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                var httpContext = _httpContextAccessor?.HttpContext;
                ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
            }

            var login = new PulseLogin
            {
                Username = username,
                LoginTime = DateTime.UtcNow,
                Success = success,
                IpAddress = ipAddress
            };

            _context.PulseLogins.Add(login);
            await _context.SaveChangesAsync();
        }

        /// <summary>Kürzliche Logins eines Benutzers abrufen</summary>
        public async Task<IEnumerable<PulseLogin>> GetRecentLoginsAsync(string username, int days = 7)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            return await _context.PulseLogins
                .Where(x => x.Username == username && x.LoginTime >= cutoffDate)
                .OrderByDescending(x => x.LoginTime)
                .ToListAsync();
        }

        /// <summary>Fehlgeschlagene Logins abrufen (für Security-Analyse)</summary>
        public async Task<IEnumerable<PulseLogin>> GetFailedLoginsAsync(int days = 7)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            return await _context.PulseLogins
                .Where(x => !x.Success && x.LoginTime >= cutoffDate)
                .OrderByDescending(x => x.LoginTime)
                .ToListAsync();
        }
    }
}

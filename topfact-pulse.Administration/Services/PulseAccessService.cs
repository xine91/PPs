using Microsoft.EntityFrameworkCore;
using topfact.Pulse.Data;

namespace topfact.Pulse.Services
{
    /// <summary>Service für Zugriffskontrolle basierend auf Pulse_Access-Tabelle</summary>
    public interface IPulseAccessService
    {
        Task<bool> HasAccessToBereichAsync(string username, int bereichId);
        Task<bool> HasAccessToGruppeAsync(string username, int gruppeId);
        Task<string> GetPermissionAsync(string username, int? bereichId = null, int? gruppeId = null);
        Task<IEnumerable<int>> GetAccessibleBereicheAsync(string username);
        Task<IEnumerable<int>> GetAccessibleGruppenAsync(string username, int bereichId);
    }

    public class PulseAccessService : IPulseAccessService
    {
        private readonly AppDbContext _context;

        public PulseAccessService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>Prüft, ob Benutzer auf einen Bereich zugreifen darf</summary>
        public async Task<bool> HasAccessToBereichAsync(string username, int bereichId)
        {
            return await _context.PulseAccess
                .Where(x => x.Username == username && x.IsActive)
                .AnyAsync(x =>
                    x.BereichID == null ||  // NULL = Zugriff auf alle
                    x.BereichID == bereichId);
        }

        /// <summary>Prüft, ob Benutzer auf eine Gruppe zugreifen darf</summary>
        public async Task<bool> HasAccessToGruppeAsync(string username, int gruppeId)
        {
            var gruppe = await _context.PulseGruppen
                .FirstOrDefaultAsync(x => x.GruppeID == gruppeId);

            if (gruppe == null) return false;

            // Hat der Benutzer Zugriff auf den übergeordneten Bereich?
            var bereichAccess = await HasAccessToBereichAsync(username, gruppe.BereichID);
            if (!bereichAccess) return false;

            // Spezifischer Gruppenzugriff?
            return await _context.PulseAccess
                .Where(x => x.Username == username && x.IsActive)
                .AnyAsync(x =>
                    x.GruppeID == null ||  // NULL = Zugriff auf alle im Bereich
                    x.GruppeID == gruppeId);
        }

        /// <summary>Gibt das höchste Zugriffslevel des Benutzers zurück</summary>
        public async Task<string> GetPermissionAsync(string username, int? bereichId = null, int? gruppeId = null)
        {
            var permissions = new[] { "View", "Edit", "Admin" };

            foreach (var perm in permissions.Reverse())
            {
                var exists = await _context.PulseAccess
                    .Where(x => x.Username == username && x.IsActive && x.Permission == perm)
                    .AnyAsync(x =>
                        (bereichId == null || x.BereichID == null || x.BereichID == bereichId) &&
                        (gruppeId == null || x.GruppeID == null || x.GruppeID == gruppeId));

                if (exists) return perm;
            }

            return "None";
        }

        /// <summary>Gibt alle Bereiche zurück, auf die der Benutzer Zugriff hat</summary>
        public async Task<IEnumerable<int>> GetAccessibleBereicheAsync(string username)
        {
            var accessRecords = await _context.PulseAccess
                .Where(x => x.Username == username && x.IsActive && x.BereichID.HasValue)
                .Select(x => x.BereichID.Value)
                .Distinct()
                .ToListAsync();

            // Hat der Benutzer einen NULL-Zugriff (alle Bereiche)?
            var hasGlobalAccess = await _context.PulseAccess
                .AnyAsync(x => x.Username == username && x.IsActive && x.BereichID == null);

            if (hasGlobalAccess)
            {
                // Rückgabe aller Bereiche
                return await _context.PulseBereiche
                    .Select(x => x.BereichID)
                    .ToListAsync();
            }

            return accessRecords;
        }

        /// <summary>Gibt alle Gruppen im Bereich zurück, auf die der Benutzer Zugriff hat</summary>
        public async Task<IEnumerable<int>> GetAccessibleGruppenAsync(string username, int bereichId)
        {
            var hasGlobalBereichAccess = await _context.PulseAccess
                .AnyAsync(x => x.Username == username && x.IsActive && x.BereichID == null);

            if (hasGlobalBereichAccess)
            {
                // Zugriff auf alle Gruppen dieses Bereichs
                return await _context.PulseGruppen
                    .Where(x => x.BereichID == bereichId)
                    .Select(x => x.GruppeID)
                    .ToListAsync();
            }

            // Spezifische Gruppen
            return await _context.PulseAccess
                .Where(x => x.Username == username && x.IsActive &&
                           (x.BereichID == bereichId || x.BereichID == null) &&
                           x.GruppeID.HasValue)
                .Select(x => x.GruppeID.Value)
                .Distinct()
                .ToListAsync();
        }
    }
}

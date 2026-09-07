using Microsoft.AspNetCore.Authentication;
using topfact.Pulse.Services;

namespace topfact.Pulse.Middleware
{
    /// <summary>Middleware zur Zugriffskontrolle auf Bereiche (Pulse_Bereich)</summary>
    public class ModeAccessMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ModeAccessMiddleware> _logger;

        public ModeAccessMiddleware(RequestDelegate next, ILogger<ModeAccessMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IPulseNavigationService navigationService,
            IPulseAccessService accessService)
        {
            // Route-Parameter "bereich" auslesen (z.B. "Technik", "Organisation")
            var bereichFromRoute = context.Request.RouteValues.TryGetValue("bereich", out var routeValue)
                ? routeValue?.ToString()
                : null;

            // Wenn kein Bereich in der Route: durchlassen
            if (string.IsNullOrWhiteSpace(bereichFromRoute))
            {
                await _next(context);
                return;
            }

            // Bereich nach Code laden
            topfact.Pulse.Models.PulseBereich? bereich = null;
            try
            {
                bereich = await navigationService.GetBereichByCodeAsync(bereichFromRoute);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ModeAccessMiddleware: DB-Lookup für Bereich '{Mode}' fehlgeschlagen.", bereichFromRoute);
                await _next(context);
                return;
            }

            if (bereich is null)
            {
                // Bereich nicht gefunden: durchlassen (führt zu 404 in Controller)
                await _next(context);
                return;
            }

            // Benutzer prüfen
            var user = context.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                // Nicht authentifiziert: durchlassen (führt zu Login-Redirect)
                await _next(context);
                return;
            }

            // ====================================================================
            // Zugriffslogik (entspricht Administration-Projekt):
            // 1. Zugriff aus Pulse_Access-Tabelle prüfen (PRIMARY)
            // 2. Rollenbasiert prüfen (RequiredRole aus Bereich, SECONDARY)
            // ====================================================================

            // Username aus Claims ermitteln
            var username = user.Identity?.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(username))
            {
                username = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("ModeAccessMiddleware: Kein Username ermittelbar – Zugriff wird verweigert.");
                await context.ForbidAsync();
                return;
            }

            // PRIMÄR: Zugriff auf Bereich prüfen (über Pulse_Access)
            // Dies ist die Hauptkontrolle - wenn der Benutzer in Pulse_Access eingetragen ist, hat er Zugriff
            bool hasAccessInTable = false;
            try
            {
                hasAccessInTable = await accessService.HasAccessToBereichAsync(username, bereich.BereichID);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ModeAccessMiddleware: Pulse_Access-Lookup fehlgeschlagen.");
                await context.ForbidAsync();
                return;
            }

            if (hasAccessInTable)
            {
                // Zugriff erlaubt via Pulse_Access
                await _next(context);
                return;
            }

            // SECONDARY: Rollenbasiert prüfen (RequiredRole aus Bereich)
            // Wenn Bereich eine RequiredRole hat, muss Benutzer diese Rolle haben
            if (!string.IsNullOrWhiteSpace(bereich.RequiredRole) && !user.IsInRole(bereich.RequiredRole))
            {
                _logger.LogInformation(
                    "ModeAccessMiddleware: User '{User}' hat keinen Pulse_Access-Eintrag und fehlt Rolle '{Role}' für Bereich '{Mode}'.",
                    username, bereich.RequiredRole, bereichFromRoute);
                await context.ForbidAsync();
                return;
            }

            // Wenn keine RequiredRole definiert UND kein Pulse_Access-Eintrag → Zugriff erlauben
            // (entspricht Administration-Verhalten: nur blocken wenn explizit verboten)
            await _next(context);
        }
    }
}
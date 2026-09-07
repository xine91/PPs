using Microsoft.AspNetCore.Authentication;
using topfact.Pulse.Services;

namespace topfact.Pulse.Middleware
{
    /// <summary>Middleware zur Zugriffskontrolle auf Bereiche (Pulse_Bereich)</summary>
    public class ModeAccessMiddleware
    {
        private readonly RequestDelegate _next;

        public ModeAccessMiddleware(RequestDelegate next)
        {
            _next = next;
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
            var bereich = await navigationService.GetBereichByCodeAsync(bereichFromRoute);
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
            // Zugriffslogik:
            // 1. Zugriff aus Pulse_Access-Tabelle prüfen (PRIMARY)
            // 2. Rollenbasiert prüfen (RequiredRole aus Bereich, SECONDARY)
            // ====================================================================

            var username = user.Identity?.Name ?? string.Empty;

            // PRIMÄR: Zugriff auf Bereich prüfen (über Pulse_Access)
            // Dies ist die Hauptkontrolle - wenn der Benutzer in Pulse_Access eingetragen ist, hat er Zugriff
            var hasAccessInTable = await accessService.HasAccessToBereichAsync(username, bereich.BereichID);

            if (!hasAccessInTable)
            {
                // Fallback: Rollenbasiert prüfen (RequiredRole aus Bereich)
                // Wenn Bereich eine RequiredRole hat, muss Benutzer diese Rolle haben
                if (!string.IsNullOrWhiteSpace(bereich.RequiredRole) && !user.IsInRole(bereich.RequiredRole))
                {
                    await context.ForbidAsync();
                    return;
                }
            }

            // Zugriff erlaubt
            await _next(context);
        }
    }
}

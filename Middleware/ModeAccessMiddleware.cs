using Microsoft.AspNetCore.Authentication;
using topfact.Pulse.Services;

namespace topfact.Pulse.Middleware
{
    public class ModeAccessMiddleware
    {
        private readonly RequestDelegate _next;

        public ModeAccessMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IUiNavigationService navigationService)
        {
            var modeFromRoute = context.Request.RouteValues.TryGetValue("bereich", out var routeValue)
                ? routeValue?.ToString()
                : null;

            if (string.IsNullOrWhiteSpace(modeFromRoute))
            {
                await _next(context);
                return;
            }

            var mode = await navigationService.GetModeByCodeAsync(modeFromRoute);
            if (mode is null)
            {
                await _next(context);
                return;
            }

            var user = context.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                await _next(context);
                return;
            }

            var roleAllowed = string.IsNullOrWhiteSpace(mode.RequiredRole) || user.IsInRole(mode.RequiredRole);
            var claimAllowed = true;
            if (!string.IsNullOrWhiteSpace(mode.RequiredClaimType))
            {
                claimAllowed = string.IsNullOrWhiteSpace(mode.RequiredClaimValue)
                    ? user.Claims.Any(c => string.Equals(c.Type, mode.RequiredClaimType, StringComparison.OrdinalIgnoreCase))
                    : user.HasClaim(mode.RequiredClaimType, mode.RequiredClaimValue);
            }

            if (!roleAllowed || !claimAllowed)
            {
                await context.ForbidAsync();
                return;
            }

            await _next(context);
        }
    }
}

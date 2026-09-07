using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;

namespace topfact.Pulse.Middleware
{
    public class RedirectPathBaseMiddleware
    {
        private readonly RequestDelegate _next;

        public RedirectPathBaseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // We need to capture the response before it's sent to the client.
            // Since the response body is usually written to, we can't easily modify headers
            // AFTER the response has started. We must use OnStarting.

            context.Response.OnStarting(() =>
            {
                var statusCode = context.Response.StatusCode;
                if (statusCode == 301 || statusCode == 302)
                {
                    var location = context.Response.Headers["Location"].ToString();
                    if (!string.IsNullOrEmpty(location) && location.StartsWith("/") && !location.StartsWith("//"))
                    {
                        var pathBase = context.Request.PathBase.ToString();
                        if (!string.IsNullOrEmpty(pathBase))
                        {
                            // Ensure pathBase starts with / and location doesn't start with pathBase
                            string normalizedPathBase = pathBase.StartsWith("/") ? pathBase : "/" + pathBase;

                            if (!location.StartsWith(normalizedPathBase, System.StringComparison.OrdinalIgnoreCase))
                            {
                                // Prepend pathBase, ensuring we don't double the slash
                                string separator = normalizedPathBase.EndsWith("/") && location.StartsWith("/") ? "" : "/";
                                // Wait, if normalizedPathBase is /pulse-admin and location is /Account/Login,
                                // we want /pulse-admin/Account/Login.
                                // If normalizedPathBase is /pulse-admin and location is /pulse-admin/Account/Login, do nothing.

                                // Correct way to prepend:
                                string newLocation = normalizedPathBase.TrimEnd('/') + "/" + location.TrimStart('/');
                                context.Response.Headers["Location"] = newLocation;
                            }
                        }
                    }
                }
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}

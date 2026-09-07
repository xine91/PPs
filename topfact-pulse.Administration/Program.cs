using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using topfact.Pulse.Data;
using topfact.Pulse.Middleware;
using topfact.Pulse.Services;
using Microsoft.AspNetCore.DataProtection;

namespace topfact.Pulse.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var pathBase = builder.Configuration["PathBase"];

            // Datenbank - mit Logging-Kontrolle für Development
            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                options.UseSqlServer(connectionString);

                // EF Core Logging nur in Development
                if (builder.Environment.IsDevelopment())
                {
                    options.LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name }, LogLevel.Information);
                }
            });

            // HTTP Context Accessor (für IP-Adresse in AuditService)
            builder.Services.AddHttpContextAccessor();

            // Pulse Services
            builder.Services.AddScoped<IPulseNavigationService, PulseNavigationService>();
            builder.Services.AddScoped<IPulseAccessService, PulseAccessService>();
            builder.Services.AddScoped<IPulseAuditService, PulseAuditService>();
            builder.Services.AddScoped<IConnectorService, ConnectorService>();

            // Session
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(120);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.Name = "CloudManagerSession";
            });

            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
            builder.Services.AddMemoryCache();
            builder.Services.AddHttpClient();

            builder.Services.AddDataProtection()
                            .PersistKeysToFileSystem(new DirectoryInfo("C:\\ProgramData\\DataProtection"));

            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add(new AuthorizeFilter());
            });

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = $"{pathBase}/login/index";
                    options.AccessDeniedPath = $"{pathBase}/Account/AccessDenied";
                    options.Cookie.Name = "PulseAuth";
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            if (!string.IsNullOrEmpty(pathBase))
            {
                app.UsePathBase(pathBase);
            }

            var supportedCultures = new[]
            {
                new CultureInfo("de-DE"),
                new CultureInfo("en-US"),
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("de-DE"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();
            app.UseRouting();
            app.UseAuthentication();
            app.UseMiddleware<ModeAccessMiddleware>();
            app.UseAuthorization();

            app.UseMiddleware<topfact.Pulse.Middleware.RedirectPathBaseMiddleware>();

            app.MapGet("/", context =>
            {
                context.Response.Redirect($"{pathBase}/login/index");
                return Task.CompletedTask;
            });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}

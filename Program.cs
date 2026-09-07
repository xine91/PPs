using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using topfact.Pulse.Data;
using topfact.Pulse.Middleware;
using topfact.Pulse.Services;
using System.Security.Claims;  

namespace topfact.CloudManager.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

            builder.Services.AddDbContext<AnalyticsDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("topfactAnalytics")
    ));
            // Session hinzufügen
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(120);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.Name = "CloudManagerSession";
            });

            // Localization Services hinzufügen
            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");



            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<IUiNavigationService, UiNavigationService>();
            builder.Services.AddHttpClient();
            // MVC (Controllers with Views) hinzufügen
            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add(new AuthorizeFilter());
            });

            // Authentication (Cookie) registrieren
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/login/index";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.Cookie.Name = "PulseAuth";
                });

            // Optional: Authorization hinzufügen
            builder.Services.AddAuthorization();



            var app = builder.Build();

            // Unterstützte Kulturen konfigurieren
            var supportedCultures = new[]
            {
                new CultureInfo("de-DE"),
                new CultureInfo("en-US"),
                new CultureInfo("fr-FR")
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("de-DE"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseExceptionHandler("/Home/Error");
            app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseSession();

            

            app.UseRouting();
            app.UseAuthentication();
            app.UseMiddleware<ModeAccessMiddleware>();
            app.UseAuthorization();

            app.MapGet("/", context =>
            {
                context.Response.Redirect("/pulse/login/index");
                return Task.CompletedTask;
            });

            // Routen konfigurieren (Standard-Controller-Route) -> Login als Startseite
            app.MapControllerRoute(
                name: "bereich",
                pattern: "pulse/{bereich:regex(Technik|Organisation)}/{controller=Home}/{action=Index}/{id?}");

            // Pulse-Prefix ohne Bereich (z.B. /pulse/Monitor/ProjektControlling – wird von bereich-Route bereits abgedeckt,
            // aber als Fallback für direkte Controller-URLs mit /pulse/-Prefix)
            app.MapControllerRoute(
                name: "pulseNoBereich",
                pattern: "pulse/{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            // Anwendung starten
            app.Run();
        }
    }
       
}

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

namespace topfact.Pulse.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Datenbank für Navigation und Login-Log
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("DefaultConnection"))
                .EnableSensitiveDataLogging()
                .LogTo(Console.WriteLine, LogLevel.Information));

            // Navigation-Service
            builder.Services.AddScoped<IPulseNavigationService, PulseNavigationService>();

            // Access-Service
            builder.Services.AddScoped<IPulseAccessService, PulseAccessService>();

            // Kategorie-Data Service
            builder.Services.AddScoped<IKategorieDataService, KategorieDataService>();

            // KPI Calculation Service
            builder.Services.AddScoped<IKpiCalculationService, KpiCalculationService>();

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

            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add(new AuthorizeFilter());
            });

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.Cookie.Name = "PulseAuth";
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            // Configure PathBase for IIS Virtual Directory support

            // Configure PathBase for IIS Virtual Directory support
            var pathBase = builder.Configuration["PathBase"];
            if (!string.IsNullOrEmpty(pathBase))
            {
                app.UsePathBase(pathBase);
            }

            app.Use(async (context, next) =>
            {
                await next();
                if (context.Response.StatusCode == 301 || context.Response.StatusCode == 302)
                {
                    var location = context.Response.Headers["Location"].ToString();
                    if (!string.IsNullOrEmpty(location) && location.StartsWith("/") && !location.StartsWith(pathBase ?? ""))
                    {
                        context.Response.Headers["Location"] = (pathBase ?? "") + location;
                    }
                }
            });

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

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSession();

            // Fix for IIS Virtual Directory: Ensure all redirects include PathBase
            app.UseRouting();
            app.UseAuthentication();
            app.UseMiddleware<ModeAccessMiddleware>();
            app.UseAuthorization();

            app.MapGet("/", context =>
            {
                context.Response.Redirect("/Account/Login");
                return Task.CompletedTask;
            });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            // Initialisiere Datenbank
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DbInitializer.InitializeDatabaseAsync(dbContext).Wait();
            }

            app.Run();
        }
    }
}

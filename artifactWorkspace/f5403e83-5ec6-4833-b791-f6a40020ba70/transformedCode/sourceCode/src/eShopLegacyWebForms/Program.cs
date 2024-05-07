
using eShopLegacyWebForms.Models;
using eShopLegacyWebForms.Models.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Entity;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Globalization;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;

namespace eShopLegacyWebForms
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();

            // Add session with InProc mode (as specified in web.config)
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Configure globalization settings from web.config
            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                var culture = builder.Configuration.GetSection("Globalization")["Culture"] ?? "en-US";
                var uiCulture = builder.Configuration.GetSection("Globalization")["UICulture"] ?? "en-US";

                var supportedCultures = new[] { new CultureInfo(culture) };
                options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(culture, uiCulture);
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });

            // Configure database context
            var useMockData = builder.Configuration.GetSection("AppSettings").GetValue<bool>("UseMockData");

            if (!useMockData)
            {
                builder.Services.AddScoped<CatalogDBContext>(_ => new CatalogDBContext());
                builder.Services.AddScoped<IDatabaseInitializer<CatalogDBContext>, CatalogDBInitializer>();
            }

            // Configure Autofac (replacing the WebForms Autofac integration)
            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
            {
                // Register your Autofac modules/services here
            });

            // Configure Application Insights if needed
            builder.Services.AddApplicationInsightsTelemetry();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // Apply request localization
            app.UseRequestLocalization();

            // Configure session
            app.UseSession();

// Initialize database if not using mock data
            if (!useMockData)
            {
using (var scope = app.Services.CreateScope())
                {
                    var initializer = scope.ServiceProvider.GetService<IDatabaseInitializer<CatalogDBContext>>();
                    Database.SetInitializer(initializer);
                }
            }

            app.UseAntiforgery();
            // Commented out as the App component doesn't exist in the current project structure
            // app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

            app.Run();
        }
    }

    // Activity ID helper class from the original Global.asax.cs
    public class ActivityIdHelper
    {
        public override string ToString()
        {
            if (System.Diagnostics.Trace.CorrelationManager.ActivityId == Guid.Empty)
            {
                System.Diagnostics.Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            }

            return System.Diagnostics.Trace.CorrelationManager.ActivityId.ToString();
        }
    }

    // Web request info class from the original Global.asax.cs
    public class WebRequestInfo
    {
        public override string ToString()
        {
            // This functionality is not directly applicable in Blazor
            // Returning a placeholder value
            return "Blazor Request";
        }
    }
}
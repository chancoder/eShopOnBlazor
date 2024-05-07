
using eShopLegacyWebForms.Models;
using eShopLegacyWebForms.Models.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace eShopLegacyWebForms
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            builder.Services.AddAntiforgery();
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Configure JSON serialization to use Newtonsoft.Json
            builder.Services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

            // Register database context
            var useMockData = builder.Configuration.GetValue<bool>("UseMockData");

            if (!useMockData)
            {
                builder.Services.AddScoped<CatalogDBContext>(_ => new CatalogDBContext());
                builder.Services.AddScoped<IDatabaseInitializer<CatalogDBContext>, CatalogDBInitializer>();
            }

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            // Configure globalization
            var supportedCultures = new[]
            {
                new CultureInfo(app.Configuration["Globalization:Culture"] ?? "en-US")
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(app.Configuration["Globalization:Culture"] ?? "en-US"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();

// Configure database if not using mock data
            if (!useMockData)
            {
using (var scope = app.Services.CreateScope())
                {
                    var initializer = scope.ServiceProvider.GetService<IDatabaseInitializer<CatalogDBContext>>();
                    Database.SetInitializer(initializer);
                }
            }

            app.UseAntiforgery();
            // Temporarily commenting out the Razor Components mapping until the App component is created
            // app.MapRazorComponents<App>()
            //    .AddInteractiveServerRenderMode();

            app.Run();
        }
    }

    public class ActivityIdHelper
    {
        public override string ToString()
        {
            if (Trace.CorrelationManager.ActivityId == Guid.Empty)
            {
                Trace.CorrelationManager.ActivityId = Guid.NewGuid();
            }

            return Trace.CorrelationManager.ActivityId.ToString();
        }
    }

    public class WebRequestInfo
    {
        public override string ToString()
        {
            return "Request information"; // Updated to not rely on HttpContext.Current
        }
    }
}
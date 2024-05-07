
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
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;

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

            // Configure Newtonsoft.Json as the default serializer
            builder.Services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

            // Add database context
            var mockData = builder.Configuration.GetValue<bool>("UseMockData");
            if (!mockData)
            {
                // Use parameterless constructor and configure connection string separately
                builder.Services.AddScoped<CatalogDBContext>(_ => {
                    var context = new CatalogDBContext();
                    context.Database.Connection.ConnectionString = builder.Configuration.GetConnectionString("CatalogDBContext");
                    return context;
                });
                builder.Services.AddScoped<IDatabaseInitializer<CatalogDBContext>, CatalogDBInitializer>();
            }

            // Add Autofac-like dependency injection
            builder.Services.AddAutofac();

            // Add Application Insights if needed
            builder.Services.AddApplicationInsightsTelemetry();

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
                new CultureInfo("en-US")
            };
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();

// Configure database if not using mock data
            if (!mockData)
            {
using (var scope = app.Services.CreateScope())
                {
                    var initializer = scope.ServiceProvider.GetService<IDatabaseInitializer<CatalogDBContext>>();
                    Database.SetInitializer(initializer);
                }
            }

            // Set up request logging
            app.Use(async (context, next) =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

                // Create activity ID for request tracing
                if (Trace.CorrelationManager.ActivityId == Guid.Empty)
                {
                    Trace.CorrelationManager.ActivityId = Guid.NewGuid();
                }

                var requestInfo = $"{context.Request.Path}, {context.Request.Headers["User-Agent"]}";
                logger.LogDebug("Request: {ActivityId}, {RequestInfo}",
                    Trace.CorrelationManager.ActivityId.ToString(),
                    requestInfo);

                await next();
            });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.UseAntiforgery();

            app.Run();
        }
    }
}
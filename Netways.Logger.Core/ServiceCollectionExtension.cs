using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Netways.Logger.Core.Enrichers;
using Netways.Logger.Core.Formatters;
using Netways.Logger.Model.Configurations;
using Serilog;
using Serilog.Core;

namespace Netways.Logger.Core;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddNetwaysLogger(this IServiceCollection services, IConfiguration configuration)
    {
        // Register core dependencies
        services.AddHttpContextAccessor();
        services.AddSingleton<ILoggerConfig, LoggerConfig>();
        services.AddSingleton<ILogger, Logger>();
        services.AddSingleton<SerilogFileFormatter>();

        // Configure Serilog with standard configuration
        ConfigureSerilog(services, configuration);

        return services;
    }

    private static void ConfigureSerilog(IServiceCollection services, IConfiguration configuration)
    {
        var serviceProvider = services.BuildServiceProvider();
        var fileFormatter = serviceProvider.GetRequiredService<SerilogFileFormatter>();

        // Set up service locator for enrichers
        ServiceLocator.ServiceProvider = serviceProvider;

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.With<HttpContextEnricher>()
            .Enrich.With<CorrelationEnricher>()
            .WriteTo.File(
                path: "D:\\Netways\\APILogs\\log-.txt",
                formatter: fileFormatter,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30)
            .WriteTo.Seq("http://localhost:5341")
            .CreateLogger();

        services.AddSingleton<Serilog.ILogger>(Log.Logger);
    }

    public static void UseNetwaysLogger(this IApplicationBuilder app, IConfiguration configuration)
    {
        // Set up service locator for runtime
        ServiceLocator.ServiceProvider = app.ApplicationServices;

        // Load logger configuration
        app.ApplicationServices.GetRequiredService<ILoggerConfig>().Load(configuration);

        // Add simple correlation ID middleware
        app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() 
                               ?? Guid.NewGuid().ToString();
            
            context.Items["CorrelationId"] = correlationId;
            context.Response.Headers["X-Correlation-Id"] = correlationId;
            
            await next();
        });
    }
}

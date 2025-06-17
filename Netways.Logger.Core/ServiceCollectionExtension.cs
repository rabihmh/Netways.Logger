using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Netways.Logger.Core.Enrichers;
using Netways.Logger.Core.Formatters;
using Netways.Logger.Model.Configurations;
using Serilog.Core;

namespace Netways.Logger.Core;

public static class ServiceCollectionExtension
{
    public static LoggerBuilder AddLoggerServices(this IServiceCollection services)
    {
        // Core dependencies
        services.AddSingleton<Func<IHttpContextAccessor>>(sp =>
                () => sp.GetRequiredService<IHttpContextAccessor>());

        // Register individual enrichers
        RegisterEnricherServices(services);

        // Register the composite enricher as the main enricher
        services.AddSingleton<ILogEventEnricher>(sp =>
        {
            var enrichers = sp.GetServices<ILogEnricher>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<CompositeEnricher>>();
            return new CompositeEnricher(enrichers, logger);
        });

        services.AddSingleton<ILoggerConfig, LoggerConfig>();

        services.AddSingleton<ILogger, Logger>();

        // Register all formatter components for structured logging
        RegisterFormatterServices(services);

        return new LoggerBuilder();
    }

    /// <summary>
    /// Creates a LoggerBuilder with access to the service provider for dependency injection
    /// </summary>
    /// <param name="serviceProvider">The service provider containing registered services</param>
    /// <returns>Configured LoggerBuilder</returns>
    public static LoggerBuilder CreateLoggerBuilderWithServices(IServiceProvider serviceProvider)
    {
        return new LoggerBuilder().WithServiceProvider(serviceProvider);
    }

    /// <summary>
    /// Registers all enricher-related services
    /// </summary>
    private static void RegisterEnricherServices(IServiceCollection services)
    {
        // Register individual enrichers
        services.AddSingleton<ILogEnricher, HttpContextEnricher>(sp =>
        {
            var httpContextAccessorFactory = sp.GetRequiredService<Func<IHttpContextAccessor>>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<BaseEnricher>>();
            return new HttpContextEnricher(
                httpContextAccessorFactory,
                logger,
                includeRequestHeaders: false,    // Can be configured via appsettings
                includeResponseHeaders: false,
                includeQueryString: true,
                includeUserAgent: true
            );
        });

        services.AddSingleton<ILogEnricher, EnvironmentEnricher>(sp =>
        {
            var configuration = sp.GetService<IConfiguration>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<BaseEnricher>>();
            return new EnvironmentEnricher(
                configuration,
                logger: logger,
                includeSystemInfo: true,         // Can be configured via appsettings
                includeProcessInfo: true
            );
        });

        services.AddSingleton<ILogEnricher, CorrelationEnricher>(sp =>
        {
            var httpContextAccessorFactory = sp.GetService<Func<IHttpContextAccessor>>();
            var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<BaseEnricher>>();
            return new CorrelationEnricher(
                httpContextAccessorFactory,
                logger: logger,
                generateIfMissing: true,         // Can be configured via appsettings
                includeTraceInfo: true
            );
        });

        // Register the composite enricher manager
        services.AddSingleton<CompositeEnricher>();
    }

    /// <summary>
    /// Registers all formatter-related services for structured logging
    /// </summary>
    private static void RegisterFormatterServices(IServiceCollection services)
    {
        // Register individual formatters
        services.AddSingleton<ExceptionLogFormatter>();
        services.AddSingleton<RequestLogFormatter>();
        services.AddSingleton<DefaultLogFormatter>();

        // Register the formatter manager
        services.AddSingleton<LogFormatterManager>();

        // Register the enhanced SerilogFileFormatter
        services.AddSingleton<SerilogFileFormatter>();
    }

    public static void AddLoggerApplications(this IApplicationBuilder app,IConfiguration configuration)
    {
        app.ApplicationServices.GetRequiredService<ILoggerConfig>().Load(configuration);
    }
}

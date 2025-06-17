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

        services.AddSingleton<HttpContextEnricher>();

        services.AddSingleton<ILogEventEnricher>(sp =>
            sp.GetRequiredService<HttpContextEnricher>());

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

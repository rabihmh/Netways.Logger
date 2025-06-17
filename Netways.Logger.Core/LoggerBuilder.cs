using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Netways.Logger.Core.Formatters;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Email;

namespace Netways.Logger.Core;

public class LoggerBuilder
{
    private readonly LoggerConfiguration _loggerConfig;
    private IServiceProvider? _serviceProvider;

    public LoggerBuilder()
    {
        _loggerConfig = new LoggerConfiguration()
            .Enrich.FromLogContext();
    }

    /// <summary>
    /// Sets the service provider for dependency injection
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <returns>The current LoggerBuilder instance</returns>
    public LoggerBuilder WithServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        return this;
    }

    public LoggerBuilder WriteToFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return this;

        // Get the enhanced file formatter from DI container or create a default one
        SerilogFileFormatter fileFormatter;
        
        if (_serviceProvider != null)
        {
            // Try to get the enhanced formatter from DI
            var formatterManager = _serviceProvider.GetService<LogFormatterManager>();
            if (formatterManager != null)
            {
                fileFormatter = new SerilogFileFormatter(formatterManager);
            }
            else
            {
                fileFormatter = _serviceProvider.GetService<SerilogFileFormatter>() ?? new SerilogFileFormatter();
            }
        }
        else
        {
            fileFormatter = new SerilogFileFormatter();
        }

        _loggerConfig
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(evt => evt.Level == LogEventLevel.Error
                                               && evt.Properties.ContainsKey("Type")
                                               && evt.Properties["Type"].ToString() == "\"Exception\"")
                .WriteTo.File(
                    formatter: fileFormatter,
                    path: $"{path}/ErrorsLogs/error-.log",
                    rollingInterval: RollingInterval.Day))
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(evt => evt.Level == LogEventLevel.Information
                                               && evt.Properties.ContainsKey("Type")
                                               && evt.Properties["Type"].ToString() == "\"Request\"")
                .WriteTo.File(
                    formatter: fileFormatter,
                    path: $"{path}/RequestsLogs/request-.log",
                    rollingInterval: RollingInterval.Day)
            );

        return this;
    }

    public LoggerBuilder WriteToSeq(string serverUrl)
    {
        if (!string.IsNullOrEmpty(serverUrl))
        {
            _loggerConfig.WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(evt => evt.Properties.ContainsKey("Type") && evt.Properties["Type"] != null)
                .WriteTo.Seq(serverUrl));
        }
        return this;
    }

    public LoggerBuilder WriteToAppInsights()
    {
        var connectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connectionString))
        {
            Log.Warning("Application Insights connection string is not set. Skipping Application Insights logging.");
            return this;
        }

        _loggerConfig.WriteTo.Logger(lc =>
            lc.Filter.ByIncludingOnly(evt => evt.Properties.ContainsKey("Type") && evt.Properties["Type"] != null)
                .WriteTo.ApplicationInsights(
                    new TelemetryConfiguration { ConnectionString = connectionString },
                    TelemetryConverter.Traces
                )
        );

        return this;
    }

    /// <summary>
    /// Configures the logger to send log events via email for non-CRM exceptions only.
    /// </summary>
    /// <param name="emailSinkOptions">
    /// The email sink options used to configure email delivery (such as SMTP server, recipients, etc).
    /// </param>
    /// <returns>
    /// The current <see cref="LoggerBuilder"/> instance for fluent configuration.
    /// </returns>
    public LoggerBuilder WriteToEmail(EmailSinkOptions emailSinkOptions)
    {
        _loggerConfig.WriteTo.Logger(lc =>
            lc.Filter.ByIncludingOnly(evt =>
                evt.Properties.ContainsKey("IsCrmValidation") &&
                evt.Properties["IsCrmValidation"] is ScalarValue sv &&
                sv.Value is bool b &&
                b == false
            )
            .WriteTo.Email(emailSinkOptions)
            .MinimumLevel.Error()
        );

        return this;
    }

    public void Build(IServiceProvider serviceProvider)
    {
        // Update the service provider if not already set
        if (_serviceProvider == null)
        {
            _serviceProvider = serviceProvider;
        }

        var enricher = serviceProvider.GetRequiredService<ILogEventEnricher>();

        _loggerConfig.Enrich.With(enricher);

        var logger = _loggerConfig.CreateLogger();
        Log.Logger = logger;

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSerilog(logger, dispose: true);
        });

        AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();
    }
}

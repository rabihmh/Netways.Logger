using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;

namespace Netways.Logger.Core.Enrichers
{
    /// <summary>
    /// Base class for log enrichers providing common functionality and error handling
    /// </summary>
    public abstract class BaseEnricher : ILogEnricher
    {
        private readonly ILogger<BaseEnricher>? _logger;

        protected BaseEnricher(ILogger<BaseEnricher>? logger = null)
        {
            _logger = logger;
        }

        public abstract string EnricherName { get; }
        public virtual bool IsEnabled => true;
        public virtual int Priority => 100;

        /// <summary>
        /// Template method for enriching log events with error handling
        /// </summary>
        /// <param name="logEvent">The log event to enrich</param>
        /// <param name="propertyFactory">Factory for creating log properties</param>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (!IsEnabled || logEvent == null || propertyFactory == null)
                return;

            try
            {
                var stopwatch = Stopwatch.StartNew();
                
                // Call the derived class implementation
                EnrichCore(logEvent, propertyFactory);
                
                stopwatch.Stop();
                
                // Log performance if it takes too long (> 10ms)
                if (stopwatch.ElapsedMilliseconds > 10)
                {
                    _logger?.LogWarning(
                        "Enricher {EnricherName} took {ElapsedMs}ms to execute, consider optimization",
                        EnricherName, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, 
                    "Error in enricher {EnricherName}: {ErrorMessage}", 
                    EnricherName, ex.Message);

                // Add error information to the log event
                SafeAddProperty(logEvent, propertyFactory, $"{EnricherName}_Error", ex.Message);
            }
        }

        /// <summary>
        /// Core enrichment logic to be implemented by derived classes
        /// </summary>
        /// <param name="logEvent">The log event to enrich</param>
        /// <param name="propertyFactory">Factory for creating log properties</param>
        protected abstract void EnrichCore(LogEvent logEvent, ILogEventPropertyFactory propertyFactory);

        /// <summary>
        /// Safely adds a property to the log event without throwing exceptions
        /// </summary>
        /// <param name="logEvent">The log event</param>
        /// <param name="propertyFactory">Property factory</param>
        /// <param name="name">Property name</param>
        /// <param name="value">Property value</param>
        protected static void SafeAddProperty(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, 
            string name, object? value)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var property = propertyFactory.CreateProperty(name, value);
                    logEvent.AddPropertyIfAbsent(property);
                }
            }
            catch
            {
                // Silently ignore property addition errors to prevent logging failures
            }
        }

        /// <summary>
        /// Gets a safe string representation of an object
        /// </summary>
        /// <param name="value">The object to convert</param>
        /// <param name="defaultValue">Default value if conversion fails</param>
        /// <returns>String representation or default value</returns>
        protected static string GetSafeString(object? value, string defaultValue = "")
        {
            try
            {
                return value?.ToString() ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
    }
} 
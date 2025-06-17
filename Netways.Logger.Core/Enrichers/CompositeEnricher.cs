using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace Netways.Logger.Core.Enrichers
{
    /// <summary>
    /// Composite enricher that manages multiple enrichers and executes them in priority order
    /// </summary>
    public class CompositeEnricher : ILogEventEnricher
    {
        private readonly IEnumerable<ILogEnricher> _enrichers;
        private readonly ILogger<CompositeEnricher>? _logger;

        public CompositeEnricher(IEnumerable<ILogEnricher> enrichers, ILogger<CompositeEnricher>? logger = null)
        {
            _enrichers = enrichers?.OrderBy(e => e.Priority).Where(e => e.IsEnabled) ?? Enumerable.Empty<ILogEnricher>();
            _logger = logger;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent == null || propertyFactory == null)
                return;

            var enabledEnrichers = _enrichers.Where(e => e.IsEnabled).ToList();
            
            if (!enabledEnrichers.Any())
                return;

            foreach (var enricher in enabledEnrichers)
            {
                try
                {
                    enricher.Enrich(logEvent, propertyFactory);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, 
                        "Error in composite enricher while executing {EnricherName}: {ErrorMessage}", 
                        enricher.EnricherName, ex.Message);

                    // Continue with other enrichers even if one fails
                }
            }
        }

        /// <summary>
        /// Gets information about all registered enrichers
        /// </summary>
        /// <returns>Dictionary with enricher names and their enabled status</returns>
        public Dictionary<string, object> GetEnricherInfo()
        {
            var info = new Dictionary<string, object>();
            
            foreach (var enricher in _enrichers.OrderBy(e => e.Priority))
            {
                info[enricher.EnricherName] = new
                {
                    Enabled = enricher.IsEnabled,
                    Priority = enricher.Priority,
                    Type = enricher.GetType().Name
                };
            }
            
            return info;
        }

        /// <summary>
        /// Gets the count of enabled enrichers
        /// </summary>
        /// <returns>Number of enabled enrichers</returns>
        public int GetEnabledEnricherCount()
        {
            return _enrichers.Count(e => e.IsEnabled);
        }
    }
} 
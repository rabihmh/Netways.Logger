using Serilog.Core;
using Serilog.Events;

namespace Netways.Logger.Core.Enrichers
{
    /// <summary>
    /// Base interface for log enrichers providing consistent abstraction
    /// </summary>
    public interface ILogEnricher : ILogEventEnricher
    {
        /// <summary>
        /// The name of this enricher for identification and debugging
        /// </summary>
        string EnricherName { get; }

        /// <summary>
        /// Indicates if this enricher is enabled
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// The priority of this enricher (lower values = higher priority)
        /// </summary>
        int Priority { get; }
    }
} 
using Serilog.Events;
using System.Text;

namespace Netways.Logger.Core.Formatters
{
    /// <summary>
    /// Interface for formatting different types of log events
    /// </summary>
    public interface ILogEventFormatter
    {
        /// <summary>
        /// Determines if this formatter can handle the given log event
        /// </summary>
        /// <param name="logEvent">The log event to check</param>
        /// <returns>True if this formatter can handle the event</returns>
        bool CanFormat(LogEvent logEvent);

        /// <summary>
        /// Formats the log event into a structured string representation
        /// </summary>
        /// <param name="logEvent">The log event to format</param>
        /// <param name="message">The StringBuilder to append the formatted content to</param>
        void Format(LogEvent logEvent, StringBuilder message);

        /// <summary>
        /// The priority of this formatter (lower values = higher priority)
        /// </summary>
        int Priority { get; }
    }
} 
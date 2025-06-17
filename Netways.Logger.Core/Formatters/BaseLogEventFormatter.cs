using Netways.Logger.Core.Helpers;
using Serilog.Events;
using System.Text;

namespace Netways.Logger.Core.Formatters
{
    /// <summary>
    /// Base class for log event formatters providing common functionality
    /// </summary>
    public abstract class BaseLogEventFormatter : ILogEventFormatter
    {
        public abstract bool CanFormat(LogEvent logEvent);
        public abstract void Format(LogEvent logEvent, StringBuilder message);
        public virtual int Priority => 100;

        /// <summary>
        /// Appends a formatted property to the message if it exists
        /// </summary>
        /// <param name="message">StringBuilder to append to</param>
        /// <param name="logEvent">Log event containing properties</param>
        /// <param name="propertyName">Name of the property to append</param>
        /// <param name="label">Label to display (defaults to property name)</param>
        protected static void AppendPropertyIfExists(StringBuilder message, LogEvent logEvent, string propertyName, string? label = null)
        {
            if (logEvent.Properties.TryGetValue(propertyName, out var propertyValue))
            {
                string value = propertyValue.ToString().Trim('"');
                
                // Format JSON if applicable
                if (JsonFormattingHelper.IsJson(value))
                {
                    value = JsonFormattingHelper.FormatJson(value);
                }

                message.AppendLine($"{label ?? propertyName}: {value}");
            }
        }

        /// <summary>
        /// Appends multiple properties in the specified order
        /// </summary>
        /// <param name="message">StringBuilder to append to</param>
        /// <param name="logEvent">Log event containing properties</param>
        /// <param name="propertyNames">Property names in order of appearance</param>
        protected static void AppendOrderedProperties(StringBuilder message, LogEvent logEvent, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                AppendPropertyIfExists(message, logEvent, propertyName);
            }
        }

        /// <summary>
        /// Checks if a log event has a specific property with a specific value
        /// </summary>
        /// <param name="logEvent">Log event to check</param>
        /// <param name="propertyName">Property name</param>
        /// <param name="expectedValue">Expected property value</param>
        /// <returns>True if property matches expected value</returns>
        protected static bool HasPropertyWithValue(LogEvent logEvent, string propertyName, string expectedValue)
        {
            return logEvent.Properties.TryGetValue(propertyName, out var property) &&
                   property.ToString().Trim('"') == expectedValue;
        }

        /// <summary>
        /// Appends basic log information (timestamp, level, etc.)
        /// </summary>
        /// <param name="message">StringBuilder to append to</param>
        /// <param name="logEvent">Log event</param>
        protected static void AppendBasicLogInfo(StringBuilder message, LogEvent logEvent)
        {
            message.AppendLine($"Timestamp: {logEvent.Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}");
            message.AppendLine($"Level: {logEvent.Level}");
            
            if (!string.IsNullOrEmpty(logEvent.MessageTemplate.Text))
            {
                message.AppendLine($"Template: {logEvent.MessageTemplate.Text}");
                message.AppendLine($"Rendered Message: {logEvent.RenderMessage()}");
            }
        }
    }
} 
using Serilog.Events;
using System.Text;
using System.Linq;

namespace Netways.Logger.Core.Formatters
{
    /// <summary>
    /// Default formatter for log events that don't match specific formatters
    /// </summary>
    public class DefaultLogFormatter : BaseLogEventFormatter
    {
        public override int Priority => 1000; // Lowest priority - fallback formatter

        public override bool CanFormat(LogEvent logEvent)
        {
            return true; // Can format any log event as fallback
        }

        public override void Format(LogEvent logEvent, StringBuilder message)
        {
            message.AppendLine("===================== GENERAL LOG =====================");
            AppendBasicLogInfo(message, logEvent);
            message.AppendLine("--------------------------------------------------------");

            // Output all properties in a generic way
            foreach (var property in logEvent.Properties.OrderBy(p => p.Key))
            {
                AppendPropertyIfExists(message, logEvent, property.Key);
            }

            // Include exception if present
            if (logEvent.Exception != null)
            {
                message.AppendLine("Exception Details:");
                message.AppendLine(logEvent.Exception.ToString());
            }

            message.AppendLine("========================================================");
        }
    }
} 
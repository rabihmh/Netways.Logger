using Serilog.Events;
using System.Text;

namespace Netways.Logger.Core.Formatters
{
    /// <summary>
    /// Formatter for request log events with CustomMessageLogData structure
    /// </summary>
    public class RequestLogFormatter : BaseLogEventFormatter
    {
        public override int Priority => 20; // Medium priority for requests

        public override bool CanFormat(LogEvent logEvent)
        {
            return HasPropertyWithValue(logEvent, "Type", "Request") || 
                   logEvent.Level == LogEventLevel.Information;
        }

        public override void Format(LogEvent logEvent, StringBuilder message)
        {
            message.AppendLine("===================== REQUEST LOG =====================");
            AppendBasicLogInfo(message, logEvent);
            message.AppendLine("--------------------------------------------------------");

            // Request specific information
            AppendPropertyIfExists(message, logEvent, "Source", "Request Source");
            AppendPropertyIfExists(message, logEvent, "FunctionName", "Function Name");
            AppendPropertyIfExists(message, logEvent, "Message", "Custom Message");
            
            // HTTP Context information
            AppendPropertyIfExists(message, logEvent, "CorrelationId", "Correlation ID");
            AppendPropertyIfExists(message, logEvent, "Route", "Request Route");
            AppendPropertyIfExists(message, logEvent, "RequestPath", "Request Path");
            AppendPropertyIfExists(message, logEvent, "RequestMethod", "HTTP Method");
            AppendPropertyIfExists(message, logEvent, "IPAddress", "Client IP");
            AppendPropertyIfExists(message, logEvent, "UserAgent", "User Agent");

            // Request/Response details if available
            AppendPropertyIfExists(message, logEvent, "RequestDetails", "Request Details");
            AppendPropertyIfExists(message, logEvent, "ResponseDetails", "Response Details");
            AppendPropertyIfExists(message, logEvent, "FunctionContext", "Function Context");

            // API specific data (for LogByApi calls)
            if (logEvent.Properties.TryGetValue("Payload", out var payload))
            {
                message.AppendLine("API Payload:");
                string payloadValue = payload.ToString().Trim('"');
                message.AppendLine(payloadValue);
            }

            message.AppendLine("========================================================");
        }
    }
} 
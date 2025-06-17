using Netways.Logger.Core.Helpers;
using Serilog.Events;
using System.Text;

namespace Netways.Logger.Core.Formatters
{
    /// <summary>
    /// Formatter for exception log events with ExceptionLogData structure
    /// </summary>
    public class ExceptionLogFormatter : BaseLogEventFormatter
    {
        public override int Priority => 10; // High priority for exceptions

        public override bool CanFormat(LogEvent logEvent)
        {
            return HasPropertyWithValue(logEvent, "Type", "Exception") || 
                   logEvent.Level == LogEventLevel.Error ||
                   logEvent.Exception != null;
        }

        public override void Format(LogEvent logEvent, StringBuilder message)
        {
            message.AppendLine("==================== EXCEPTION LOG ====================");
            AppendBasicLogInfo(message, logEvent);
            message.AppendLine("--------------------------------------------------------");

            // Core exception information
            AppendPropertyIfExists(message, logEvent, "Source", "Exception Source");
            AppendPropertyIfExists(message, logEvent, "FunctionName", "Function Name");
            AppendPropertyIfExists(message, logEvent, "Message", "Exception Message");
            
            // Context information
            AppendPropertyIfExists(message, logEvent, "CorrelationId", "Correlation ID");
            AppendPropertyIfExists(message, logEvent, "Route", "Request Route");
            AppendPropertyIfExists(message, logEvent, "IsCrmValidation", "Is CRM Validation");

            // Function parameters (formatted as JSON)
            if (logEvent.Properties.TryGetValue("FunctionParameters", out var funcParams))
            {
                string parametersValue = funcParams.ToString().Trim('"');
                if (JsonFormattingHelper.IsJson(parametersValue))
                {
                    message.AppendLine("Function Parameters:");
                    message.AppendLine(JsonFormattingHelper.TryFormatJson(parametersValue));
                }
                else
                {
                    message.AppendLine($"Function Parameters: {parametersValue}");
                }
            }

            // Stack trace information
            AppendPropertyIfExists(message, logEvent, "Trace", "Stack Trace");
            AppendPropertyIfExists(message, logEvent, "InnerMessage", "Inner Exception Message");
            AppendPropertyIfExists(message, logEvent, "InnerStackTrace", "Inner Stack Trace");

            // Include actual exception if present
            if (logEvent.Exception != null)
            {
                message.AppendLine("Full Exception Details:");
                message.AppendLine(logEvent.Exception.ToString());
            }

            message.AppendLine("========================================================");
        }
    }
} 
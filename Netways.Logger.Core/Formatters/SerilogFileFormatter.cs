using Serilog.Events;
using Serilog.Formatting;
using System.Text;

namespace Netways.Logger.Core.Formatters
{
    /// <summary>
    /// Enhanced file formatter that creates rich, readable log entries with structured data
    /// </summary>
    public class SerilogFileFormatter : ITextFormatter
    {
        public void Format(LogEvent logEvent, TextWriter output)
        {
            try
            {
                var sb = new StringBuilder();

                // Header with timestamp and level
                sb.AppendLine("=" + new string('=', 80));
                sb.AppendLine($"Timestamp: {logEvent.Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}");
                sb.AppendLine($"Level: {logEvent.Level}");

                // Template and rendered message
                if (logEvent.MessageTemplate != null)
                {
                    sb.AppendLine($"Template: {logEvent.MessageTemplate}");
                }
                sb.AppendLine($"Rendered Message: {logEvent.RenderMessage()}");

                // Separator
                sb.AppendLine(new string('-', 60));

                // Key properties first
                WriteKeyProperties(sb, logEvent);

                // All other properties
                WriteOtherProperties(sb, logEvent);

                // Exception if present
                if (logEvent.Exception != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("EXCEPTION DETAILS:");
                    sb.AppendLine($"Type: {logEvent.Exception.GetType().FullName}");
                    sb.AppendLine($"Message: {logEvent.Exception.Message}");
                    if (!string.IsNullOrEmpty(logEvent.Exception.StackTrace))
                    {
                        sb.AppendLine("Stack Trace:");
                        sb.AppendLine(logEvent.Exception.StackTrace);
                    }

                    // Inner exception
                    var innerEx = logEvent.Exception.InnerException;
                    if (innerEx != null)
                    {
                        sb.AppendLine();
                        sb.AppendLine("INNER EXCEPTION:");
                        sb.AppendLine($"Type: {innerEx.GetType().FullName}");
                        sb.AppendLine($"Message: {innerEx.Message}");
                        if (!string.IsNullOrEmpty(innerEx.StackTrace))
                        {
                            sb.AppendLine("Stack Trace:");
                            sb.AppendLine(innerEx.StackTrace);
                        }
                    }
                }

                // Footer
                sb.AppendLine("=" + new string('=', 80));
                sb.AppendLine();

                output.Write(sb.ToString());
            }
            catch (Exception ex)
            {
                // Fallback formatting if there's an error
                output.WriteLine($"[{logEvent.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{logEvent.Level}] {logEvent.RenderMessage()}");
                output.WriteLine($"Formatter Error: {ex.Message}");
                output.WriteLine();
            }
        }

        private void WriteKeyProperties(StringBuilder sb, LogEvent logEvent)
        {
            // Key properties that should appear first
            var keyProperties = new[] { "CorrelationId", "Method", "Path", "StatusCode", "ClientIP", "Type" };

            foreach (var key in keyProperties)
            {
                if (logEvent.Properties.TryGetValue(key, out var value))
                {
                    sb.AppendLine($"{key}: {FormatPropertyValue(value)}");
                }
            }
        }

        private void WriteOtherProperties(StringBuilder sb, LogEvent logEvent)
        {
            var keyProperties = new[] { "CorrelationId", "Method", "Path", "StatusCode", "ClientIP", "Type" };
            var otherProperties = logEvent.Properties
                .Where(p => !keyProperties.Contains(p.Key))
                .OrderBy(p => p.Key);

            foreach (var property in otherProperties)
            {
                sb.AppendLine($"{property.Key}: {FormatPropertyValue(property.Value)}");
            }
        }

        private string FormatPropertyValue(LogEventPropertyValue value)
        {
            try
            {
                switch (value)
                {
                    case ScalarValue scalar:
                        return scalar.Value?.ToString() ?? "null";

                    case SequenceValue sequence:
                        var items = sequence.Elements.Select(e => FormatPropertyValue(e));
                        return $"[{string.Join(", ", items)}]";

                    case StructureValue structure:
                        var props = structure.Properties.Select(p => $"{p.Name}: {FormatPropertyValue(p.Value)}");
                        return $"{{{string.Join(", ", props)}}}";

                    case DictionaryValue dictionary:
                        var dictItems = dictionary.Elements.Select(kvp => 
                            $"{FormatPropertyValue(kvp.Key)}: {FormatPropertyValue(kvp.Value)}");
                        return $"{{{string.Join(", ", dictItems)}}}";

                    default:
                        return value.ToString();
                }
            }
            catch
            {
                return value.ToString();
            }
        }
    }
} 
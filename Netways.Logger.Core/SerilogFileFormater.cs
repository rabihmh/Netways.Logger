using Netways.Logger.Core.Formatters;
using Serilog.Events;
using Serilog.Formatting;

namespace Netways.Logger.Core
{
    /// <summary>
    /// Enhanced Serilog file formatter that uses structured logging patterns
    /// and professional design principles with Strategy Pattern implementation
    /// </summary>
    public class SerilogFileFormatter : ITextFormatter
    {
        private readonly LogFormatterManager _formatterManager;

        /// <summary>
        /// Initializes a new instance of the SerilogFileFormatter
        /// </summary>
        public SerilogFileFormatter()
        {
            _formatterManager = new LogFormatterManager();
        }

        /// <summary>
        /// Initializes a new instance of the SerilogFileFormatter with a custom formatter manager
        /// </summary>
        /// <param name="formatterManager">Custom formatter manager</param>
        public SerilogFileFormatter(LogFormatterManager formatterManager)
        {
            _formatterManager = formatterManager ?? throw new ArgumentNullException(nameof(formatterManager));
        }

        /// <summary>
        /// Formats the log event and writes it to the output
        /// </summary>
        /// <param name="logEvent">The log event to format</param>
        /// <param name="output">The text writer to write the formatted output to</param>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            try
            {
                var formattedMessage = _formatterManager.FormatLogEvent(logEvent);
                output.WriteLine(formattedMessage);
            }
            catch (Exception ex)
            {
                // Fallback formatting in case of formatter errors
                output.WriteLine($"[FORMATTER ERROR] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC");
                output.WriteLine($"Original Message: {logEvent.RenderMessage()}");
                output.WriteLine($"Formatter Error: {ex.Message}");
                output.WriteLine("Raw Properties:");
                
                foreach (var prop in logEvent.Properties)
                {
                    output.WriteLine($"  {prop.Key}: {prop.Value}");
                }
                
                if (logEvent.Exception != null)
                {
                    output.WriteLine($"Exception: {logEvent.Exception}");
                }
                
                output.WriteLine("========================================");
            }
        }

        /// <summary>
        /// Adds a custom formatter to the formatter manager
        /// </summary>
        /// <param name="formatter">The custom formatter to add</param>
        public void AddCustomFormatter(ILogEventFormatter formatter)
        {
            _formatterManager.AddFormatter(formatter);
        }
    }
}

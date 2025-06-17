using Serilog.Events;
using System.Text;
using System.Linq;

namespace Netways.Logger.Core.Formatters
{
    /// <summary>
    /// Manager class that coordinates different log formatters using Strategy Pattern
    /// </summary>
    public class LogFormatterManager
    {
        private readonly List<ILogEventFormatter> _formatters;
        private readonly ILogEventFormatter _defaultFormatter;

        public LogFormatterManager()
        {
            _formatters = new List<ILogEventFormatter>
            {
                new ExceptionLogFormatter(),
                new RequestLogFormatter()
            };

            _defaultFormatter = new DefaultLogFormatter();

            // Sort formatters by priority (ascending)
            _formatters = _formatters.OrderBy(f => f.Priority).ToList();
        }

        /// <summary>
        /// Adds a custom formatter to the collection
        /// </summary>
        /// <param name="formatter">The formatter to add</param>
        public void AddFormatter(ILogEventFormatter formatter)
        {
            if (formatter == null) 
                throw new ArgumentNullException(nameof(formatter));

            _formatters.Add(formatter);
            _formatters.Sort((x, y) => x.Priority.CompareTo(y.Priority));
        }

        /// <summary>
        /// Formats a log event using the most appropriate formatter
        /// </summary>
        /// <param name="logEvent">The log event to format</param>
        /// <returns>Formatted log string</returns>
        public string FormatLogEvent(LogEvent logEvent)
        {
            if (logEvent == null) 
                throw new ArgumentNullException(nameof(logEvent));

            var message = new StringBuilder(1000);

            // Find the first formatter that can handle this log event
            var formatter = _formatters.FirstOrDefault(f => f.CanFormat(logEvent)) ?? _defaultFormatter;
            
            formatter.Format(logEvent, message);

            return message.ToString();
        }

        /// <summary>
        /// Gets all registered formatters (for testing/debugging purposes)
        /// </summary>
        /// <returns>Read-only collection of formatters</returns>
        public IReadOnlyList<ILogEventFormatter> GetFormatters()
        {
            return _formatters.AsReadOnly();
        }
    }
} 
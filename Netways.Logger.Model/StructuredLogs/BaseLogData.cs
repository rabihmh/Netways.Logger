using System;

namespace Netways.Logger.Model.StructuredLogs
{
    public abstract class BaseLogData
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string LogType { get; set; } = string.Empty;
        public string? CorrelationId { get; set; }
        public string? Route { get; set; }
        public string Source { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
    }
} 
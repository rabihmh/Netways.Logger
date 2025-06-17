using System;
using System.Collections.Generic;

namespace Netways.Logger.Model.StructuredLogs
{
    public class ApiLogData : BaseLogData
    {
        public Dictionary<string, string?> Payload { get; set; } = new Dictionary<string, string?>();
        public bool IsException { get; set; }
        
        public ApiLogData(Dictionary<string, string?> payload, bool isException)
        {
            Payload = payload;
            IsException = isException;
            LogType = isException ? "Exception" : "Request";
        }
    }
} 
using Newtonsoft.Json.Linq;

namespace Netways.Logger.Model.StructuredLogs
{
    public class ExceptionLogData : BaseLogData
    {
        public string Message { get; set; } = string.Empty;
        public JObject FunctionParameters { get; set; } = new JObject();
        public string Trace { get; set; } = string.Empty;
        public string InnerMessage { get; set; } = string.Empty;
        public string InnerStackTrace { get; set; } = string.Empty;
        public bool IsCrmValidation { get; set; }
        
        public ExceptionLogData()
        {
            LogType = "Exception";
        }
    }
} 
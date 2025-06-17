namespace Netways.Logger.Model.StructuredLogs
{
    public class CustomMessageLogData : BaseLogData
    {
        public string Message { get; set; } = string.Empty;
        
        public CustomMessageLogData()
        {
            LogType = "Request";
        }
    }
} 
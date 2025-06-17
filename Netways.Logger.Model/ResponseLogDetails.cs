namespace Netways.Logger.Model.Logger;

public class ResponseLogDetails
{
    public int StatusCode { get; set; }

    public long ElapsedMilliseconds { get; set; }
    
    public Dictionary<string, string> Headers { get; set; } = [];
    
    public string Body { get; set; } = string.Empty;
}

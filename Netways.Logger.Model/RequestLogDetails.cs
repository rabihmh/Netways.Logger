namespace Netways.Logger.Model.Logger;

 public class RequestLogDetails
{
    public string Method { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    // public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    
    public string Body { get; set; } = string.Empty;
    
    public List<string> FileNames { get; set; } = new List<string>();
    
    //public Dictionary<string, string> FormData { get; set; } = new Dictionary<string, string>();
 }

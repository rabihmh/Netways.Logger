namespace Netways.Logger.Model.Configurations;

using Microsoft.Extensions.Configuration;

public class LoggerConfig : ILoggerConfig
{
    public required bool OnPremise { get; set; } = true;

    public string? Path { get; set; }

    public bool LogRequest { get; set; }

    public bool LogResponse { get; set; }

    public bool GenericError { get; set; }

    public void Load(IConfiguration configuration)
    {
        configuration.Bind("Logger", this);
    }
}

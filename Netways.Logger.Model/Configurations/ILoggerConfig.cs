using Microsoft.Extensions.Configuration;

namespace Netways.Logger.Model.Configurations
{
    public interface ILoggerConfig
    {
        bool OnPremise { get; }

        string? Path { get; }

        bool LogRequest { get; }

        bool LogResponse { get; }

        bool GenericError { get; }

        void Load(IConfiguration configuration);
    }
}

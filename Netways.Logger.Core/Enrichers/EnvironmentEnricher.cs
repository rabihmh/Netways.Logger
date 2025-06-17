using Microsoft.Extensions.Configuration;

namespace Netways.Logger.Core.Enrichers
{
    /// <summary>
    /// Enriches log context with environment information.
    /// </summary>
    public class EnvironmentEnricher(
        IConfiguration? configuration = null,
        string? applicationName = null,
        string? environmentName = null)
    {
        private readonly IConfiguration? _configuration = configuration;
        private readonly string _applicationName = applicationName ??
            configuration?.GetValue<string>("ApplicationName") ??
            AppDomain.CurrentDomain.FriendlyName;

        private readonly string _environmentName = environmentName ??
            configuration?.GetValue<string>("EnvironmentName") ??
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
            "Production";

        /// <summary>
        /// Enriches the provided dictionary with environment information.
        /// </summary>
        /// <param name="properties">The dictionary to enrich.</param>
        public void Enrich(IDictionary<string, object> properties)
        {
            properties["ApplicationName"] = _applicationName;
            properties["EnvironmentName"] = _environmentName;
            properties["MachineName"] = Environment.MachineName;
            properties["OSVersion"] = Environment.OSVersion.ToString();
            properties["ProcessId"] = Environment.ProcessId;
            properties["FrameworkDescription"] = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

            // Add timestamp information
            properties["Timestamp"] = DateTime.UtcNow;
            properties["TimestampEpoch"] = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
        }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Netways.Logger.Core.Enrichers
{
    /// <summary>
    /// Enhanced enricher that adds comprehensive environment and system information to log events
    /// </summary>
    public class EnvironmentEnricher : BaseEnricher
    {
        private readonly IConfiguration? _configuration;
        private readonly string _applicationName;
        private readonly string _environmentName;
        private readonly bool _includeSystemInfo;
        private readonly bool _includeProcessInfo;
        
        // Cached values for performance
        private readonly string _machineName;
        private readonly string _osVersion;
        private readonly string _frameworkDescription;
        private readonly int _processId;

        public EnvironmentEnricher(
            IConfiguration? configuration = null,
            string? applicationName = null,
            string? environmentName = null,
            bool includeSystemInfo = true,
            bool includeProcessInfo = true,
            ILogger<BaseEnricher>? logger = null)
            : base(logger)
        {
            _configuration = configuration;
            _includeSystemInfo = includeSystemInfo;
            _includeProcessInfo = includeProcessInfo;

            _applicationName = applicationName ??
                configuration?.GetValue<string>("ApplicationName") ??
                AppDomain.CurrentDomain.FriendlyName;

            _environmentName = environmentName ??
                configuration?.GetValue<string>("EnvironmentName") ??
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                "Production";

            // Cache expensive operations
            try
            {
                _machineName = Environment.MachineName;
                _osVersion = Environment.OSVersion.ToString();
                _frameworkDescription = RuntimeInformation.FrameworkDescription;
                _processId = Environment.ProcessId;
            }
            catch
            {
                _machineName = "Unknown";
                _osVersion = "Unknown";
                _frameworkDescription = "Unknown";
                _processId = 0;
            }
        }

        public override string EnricherName => "Environment";
        public override int Priority => 20; // Medium priority for environment info

        protected override void EnrichCore(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            // Core application information
            EnrichWithApplicationInfo(logEvent, propertyFactory);

            // Optional system information
            if (_includeSystemInfo)
                EnrichWithSystemInfo(logEvent, propertyFactory);

            // Optional process information
            if (_includeProcessInfo)
                EnrichWithProcessInfo(logEvent, propertyFactory);

            // Runtime information
            EnrichWithRuntimeInfo(logEvent, propertyFactory);
        }

        private void EnrichWithApplicationInfo(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            SafeAddProperty(logEvent, propertyFactory, "ApplicationName", _applicationName);
            SafeAddProperty(logEvent, propertyFactory, "EnvironmentName", _environmentName);
            
            // Application version if available
            var version = GetApplicationVersion();
            if (!string.IsNullOrEmpty(version))
            {
                SafeAddProperty(logEvent, propertyFactory, "ApplicationVersion", version);
            }

            // Build configuration if available
            var buildConfig = GetBuildConfiguration();
            if (!string.IsNullOrEmpty(buildConfig))
            {
                SafeAddProperty(logEvent, propertyFactory, "BuildConfiguration", buildConfig);
            }
        }

        private void EnrichWithSystemInfo(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            SafeAddProperty(logEvent, propertyFactory, "MachineName", _machineName);
            SafeAddProperty(logEvent, propertyFactory, "OSVersion", _osVersion);
            SafeAddProperty(logEvent, propertyFactory, "FrameworkDescription", _frameworkDescription);

            // Additional system info
            SafeAddProperty(logEvent, propertyFactory, "ProcessorCount", Environment.ProcessorCount);
            SafeAddProperty(logEvent, propertyFactory, "Is64BitOperatingSystem", Environment.Is64BitOperatingSystem);
            SafeAddProperty(logEvent, propertyFactory, "Is64BitProcess", Environment.Is64BitProcess);

            // Current directory
            try
            {
                SafeAddProperty(logEvent, propertyFactory, "CurrentDirectory", Environment.CurrentDirectory);
            }
            catch
            {
                // Ignore if unable to get current directory
            }
        }

        private void EnrichWithProcessInfo(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            SafeAddProperty(logEvent, propertyFactory, "ProcessId", _processId);

            try
            {
                // Memory information
                var workingSet = Environment.WorkingSet;
                SafeAddProperty(logEvent, propertyFactory, "WorkingSetMemory", workingSet);

                // GC information
                var gen0Collections = GC.CollectionCount(0);
                var gen1Collections = GC.CollectionCount(1);
                var gen2Collections = GC.CollectionCount(2);

                SafeAddProperty(logEvent, propertyFactory, "GCGen0Collections", gen0Collections);
                SafeAddProperty(logEvent, propertyFactory, "GCGen1Collections", gen1Collections);
                SafeAddProperty(logEvent, propertyFactory, "GCGen2Collections", gen2Collections);

                // Total memory
                var totalMemory = GC.GetTotalMemory(false);
                SafeAddProperty(logEvent, propertyFactory, "TotalMemory", totalMemory);
            }
            catch
            {
                // Ignore if unable to get process info
            }
        }

        private void EnrichWithRuntimeInfo(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            // Timestamps
            SafeAddProperty(logEvent, propertyFactory, "Timestamp", DateTime.UtcNow);
            SafeAddProperty(logEvent, propertyFactory, "TimestampEpoch", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            // Thread information
            SafeAddProperty(logEvent, propertyFactory, "ThreadId", Environment.CurrentManagedThreadId);

            // Uptime
            try
            {
                var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
                SafeAddProperty(logEvent, propertyFactory, "ApplicationUptime", uptime.TotalMilliseconds);
            }
            catch
            {
                // Ignore if unable to get uptime
            }
        }

        private string GetApplicationVersion()
        {
            try
            {
                return _configuration?.GetValue<string>("ApplicationVersion") ??
                       Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ??
                       string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GetBuildConfiguration()
        {
            try
            {
                return _configuration?.GetValue<string>("BuildConfiguration") ??
#if DEBUG
                       "Debug";
#else
                       "Release";
#endif
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}

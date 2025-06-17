namespace Netways.Logger.Core.Enrichers
{
    /// <summary>
    /// Configuration class for enrichers to support external configuration
    /// </summary>
    public class EnricherConfiguration
    {
        /// <summary>
        /// HTTP Context enricher configuration
        /// </summary>
        public HttpContextEnricherConfig HttpContext { get; set; } = new();

        /// <summary>
        /// Environment enricher configuration
        /// </summary>
        public EnvironmentEnricherConfig Environment { get; set; } = new();

        /// <summary>
        /// Correlation enricher configuration
        /// </summary>
        public CorrelationEnricherConfig Correlation { get; set; } = new();

        /// <summary>
        /// Global enricher settings
        /// </summary>
        public GlobalEnricherConfig Global { get; set; } = new();
    }

    /// <summary>
    /// HTTP Context enricher specific configuration
    /// </summary>
    public class HttpContextEnricherConfig
    {
        public bool Enabled { get; set; } = true;
        public bool IncludeRequestHeaders { get; set; } = false;
        public bool IncludeResponseHeaders { get; set; } = false;
        public bool IncludeQueryString { get; set; } = true;
        public bool IncludeUserAgent { get; set; } = true;
        public string[] SensitiveHeaders { get; set; } = { "Authorization", "Cookie", "X-API-Key", "X-Auth-Token" };
        public int Priority { get; set; } = 10;
    }

    /// <summary>
    /// Environment enricher specific configuration
    /// </summary>
    public class EnvironmentEnricherConfig
    {
        public bool Enabled { get; set; } = true;
        public bool IncludeSystemInfo { get; set; } = true;
        public bool IncludeProcessInfo { get; set; } = true;
        public string? ApplicationName { get; set; }
        public string? EnvironmentName { get; set; }
        public int Priority { get; set; } = 20;
    }

    /// <summary>
    /// Correlation enricher specific configuration
    /// </summary>
    public class CorrelationEnricherConfig
    {
        public bool Enabled { get; set; } = true;
        public string CorrelationIdHeaderName { get; set; } = "X-Correlation-Id";
        public bool GenerateIfMissing { get; set; } = true;
        public bool IncludeTraceInfo { get; set; } = true;
        public int Priority { get; set; } = 5;
    }

    /// <summary>
    /// Global enricher configuration
    /// </summary>
    public class GlobalEnricherConfig
    {
        public bool EnablePerformanceLogging { get; set; } = true;
        public int PerformanceThresholdMs { get; set; } = 10;
        public bool ContinueOnError { get; set; } = true;
    }
} 
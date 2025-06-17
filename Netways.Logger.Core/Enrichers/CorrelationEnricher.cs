using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;

namespace Netways.Logger.Core.Enrichers;

/// <summary>
/// Enhanced enricher that adds correlation IDs and distributed tracing information to log events
/// </summary>
public class CorrelationEnricher : BaseEnricher
{
    private readonly Func<IHttpContextAccessor>? _httpContextAccessorFactory;
    private readonly string _correlationIdHeaderName;
    private readonly Func<string> _correlationIdGenerator;
    private readonly bool _generateIfMissing;
    private readonly bool _includeTraceInfo;

    public CorrelationEnricher(
        Func<IHttpContextAccessor>? httpContextAccessorFactory = null,
        string correlationIdHeaderName = "X-Correlation-Id",
        Func<string>? correlationIdGenerator = null,
        bool generateIfMissing = true,
        bool includeTraceInfo = true,
        ILogger<BaseEnricher>? logger = null)
        : base(logger)
    {
        _httpContextAccessorFactory = httpContextAccessorFactory;
        _correlationIdHeaderName = correlationIdHeaderName;
        _correlationIdGenerator = correlationIdGenerator ?? (() => Guid.NewGuid().ToString("D"));
        _generateIfMissing = generateIfMissing;
        _includeTraceInfo = includeTraceInfo;
    }

    public override string EnricherName => "Correlation";
    public override int Priority => 5; // Very high priority for correlation

    protected override void EnrichCore(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Get correlation ID from multiple sources
        var correlationId = GetCorrelationId();
        if (!string.IsNullOrEmpty(correlationId))
        {
            SafeAddProperty(logEvent, propertyFactory, "CorrelationId", correlationId);
        }

        // Add trace information if enabled
        if (_includeTraceInfo)
        {
            EnrichWithTraceInfo(logEvent, propertyFactory);
        }

        // Add request context if available
        EnrichWithRequestContext(logEvent, propertyFactory);
    }

    private string GetCorrelationId()
    {
        // Priority order for correlation ID sources:
        // 1. HTTP Context header
        // 2. HTTP Context items
        // 3. Activity/Trace context
        // 4. Generate new if enabled

        var correlationId = GetFromHttpContext();
        if (!string.IsNullOrEmpty(correlationId))
            return correlationId;

        correlationId = GetFromActivity();
        if (!string.IsNullOrEmpty(correlationId))
            return correlationId;

        // Generate new correlation ID if none found and generation is enabled
        if (_generateIfMissing)
        {
            correlationId = _correlationIdGenerator();
            
            // Store back to HTTP context if available
            StoreToHttpContext(correlationId);
            
            return correlationId;
        }

        return string.Empty;
    }

    private string GetFromHttpContext()
    {
        try
        {
            var httpContext = _httpContextAccessorFactory?.Invoke()?.HttpContext;
            if (httpContext == null)
                return string.Empty;

            // Check headers first
            var headerValue = httpContext.Request.Headers[_correlationIdHeaderName].FirstOrDefault();
            if (!string.IsNullOrEmpty(headerValue))
                return headerValue;

                         // Check common correlation headers
             var commonHeaders = new[] { "X-Request-Id", "X-Trace-Id", "TraceId", "RequestId" };
             foreach (var header in commonHeaders)
             {
                 var value = httpContext.Request.Headers[header].FirstOrDefault();
                 if (!string.IsNullOrEmpty(value))
                     return value;
             }

             // Check items collection
             if (httpContext.Items.TryGetValue("X-Correlation-Id", out var itemValue))
             {
                 return itemValue?.ToString() ?? string.Empty;
             }

             return string.Empty;
         }
         catch
         {
             return string.Empty;
         }
     }

     private string GetFromActivity()
     {
         try
         {
             var activity = Activity.Current;
             if (activity != null)
             {
                 // Try to get from trace ID
                 if (activity.TraceId != default)
                 {
                     return activity.TraceId.ToString();
                 }

                 // Try to get from span ID
                 if (activity.SpanId != default)
                 {
                     return activity.SpanId.ToString();
                 }

                 // Check baggage for correlation ID
                 foreach (var baggage in activity.Baggage)
                 {
                     if (baggage.Key.Equals("CorrelationId", StringComparison.OrdinalIgnoreCase) ||
                         baggage.Key.Equals("X-Correlation-Id", StringComparison.OrdinalIgnoreCase))
                     {
                         return baggage.Value ?? string.Empty;
                     }
                 }
             }

             return string.Empty;
         }
         catch
         {
             return string.Empty;
         }
     }

     private void StoreToHttpContext(string correlationId)
     {
         try
         {
             var httpContext = _httpContextAccessorFactory?.Invoke()?.HttpContext;
             if (httpContext != null)
             {
                 // Store in items for later retrieval
                 httpContext.Items["X-Correlation-Id"] = correlationId;

                                     // Also add to response headers if not already present
                    if (!httpContext.Response.Headers.ContainsKey(_correlationIdHeaderName))
                    {
                        httpContext.Response.Headers.Append(_correlationIdHeaderName, correlationId);
                    }
             }
         }
         catch
         {
             // Ignore errors when storing correlation ID
         }
     }

     private void EnrichWithTraceInfo(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
     {
         try
         {
             var activity = Activity.Current;
             if (activity != null)
             {
                 SafeAddProperty(logEvent, propertyFactory, "TraceId", activity.TraceId.ToString());
                 SafeAddProperty(logEvent, propertyFactory, "SpanId", activity.SpanId.ToString());
                 SafeAddProperty(logEvent, propertyFactory, "ParentSpanId", activity.ParentSpanId.ToString());
                                     SafeAddProperty(logEvent, propertyFactory, "ActivityId", activity.Id ?? string.Empty);
                 SafeAddProperty(logEvent, propertyFactory, "OperationName", activity.OperationName);

                 // Add activity tags if any
                 if (activity.Tags.Any())
                 {
                     var tags = activity.Tags.ToDictionary(t => t.Key, t => t.Value);
                     SafeAddProperty(logEvent, propertyFactory, "ActivityTags", tags);
                 }
             }
         }
         catch
         {
             // Ignore errors when enriching with trace info
         }
     }

     private void EnrichWithRequestContext(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
     {
         try
         {
             var httpContext = _httpContextAccessorFactory?.Invoke()?.HttpContext;
             if (httpContext != null)
             {
                 // Session ID if available
                 if (httpContext.Session?.IsAvailable == true)
                 {
                     SafeAddProperty(logEvent, propertyFactory, "SessionId", httpContext.Session.Id);
                 }

                 // Request ID from ASP.NET Core
                 if (!string.IsNullOrEmpty(httpContext.TraceIdentifier))
                 {
                     SafeAddProperty(logEvent, propertyFactory, "RequestId", httpContext.TraceIdentifier);
                 }

                 // Connection ID if available
                 if (httpContext.Connection?.Id != null)
                 {
                     SafeAddProperty(logEvent, propertyFactory, "ConnectionId", httpContext.Connection.Id);
                 }
             }
         }
         catch
         {
             // Ignore errors when enriching with request context
         }
     }

     /// <summary>
     /// Gets a new correlation ID using the configured generator
     /// </summary>
     /// <returns>A new correlation ID</returns>
     public string GetNewCorrelationId()
     {
         return _correlationIdGenerator();
     }

     /// <summary>
     /// Gets the correlation ID header name
     /// </summary>
     /// <returns>The correlation ID header name</returns>
     public string GetCorrelationIdHeaderName()
     {
         return _correlationIdHeaderName;
     }
}
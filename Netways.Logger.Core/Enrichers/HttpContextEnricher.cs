using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using System.Net;
using System.Linq;

namespace Netways.Logger.Core.Enrichers;

/// <summary>
/// Enhanced enricher that adds comprehensive HTTP context information to log events
/// </summary>
public class HttpContextEnricher : BaseEnricher
{
    private readonly Func<IHttpContextAccessor> _httpContextAccessorFactory;
    private readonly bool _includeRequestHeaders;
    private readonly bool _includeResponseHeaders;
    private readonly bool _includeQueryString;
    private readonly bool _includeUserAgent;
    private readonly string[] _sensitiveHeaders;

    public HttpContextEnricher(
        Func<IHttpContextAccessor> httpContextAccessorFactory,
        ILogger<BaseEnricher>? logger = null,
        bool includeRequestHeaders = false,
        bool includeResponseHeaders = false,
        bool includeQueryString = true,
        bool includeUserAgent = true,
        string[]? sensitiveHeaders = null)
        : base(logger)
    {
        _httpContextAccessorFactory = httpContextAccessorFactory ?? throw new ArgumentNullException(nameof(httpContextAccessorFactory));
        _includeRequestHeaders = includeRequestHeaders;
        _includeResponseHeaders = includeResponseHeaders;
        _includeQueryString = includeQueryString;
        _includeUserAgent = includeUserAgent;
        _sensitiveHeaders = sensitiveHeaders ?? new[] { "Authorization", "Cookie", "X-API-Key", "X-Auth-Token" };
    }

    public override string EnricherName => "HttpContext";
    public override int Priority => 10; // High priority for HTTP context

    protected override void EnrichCore(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = GetHttpContext();
        if (httpContext == null)
            return;

        // Basic HTTP information
        EnrichWithBasicHttpInfo(logEvent, propertyFactory, httpContext);

        // Optional enrichments based on configuration
        if (_includeQueryString)
            EnrichWithQueryString(logEvent, propertyFactory, httpContext);

        if (_includeUserAgent)
            EnrichWithUserAgent(logEvent, propertyFactory, httpContext);

        if (_includeRequestHeaders)
            EnrichWithRequestHeaders(logEvent, propertyFactory, httpContext);

        if (_includeResponseHeaders)
            EnrichWithResponseHeaders(logEvent, propertyFactory, httpContext);

        // Additional context information
        EnrichWithAdditionalContext(logEvent, propertyFactory, httpContext);
    }

    private HttpContext? GetHttpContext()
    {
        try
        {
            return _httpContextAccessorFactory()?.HttpContext;
        }
        catch
        {
            return null;
        }
    }

    private void EnrichWithBasicHttpInfo(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, HttpContext httpContext)
    {
        // IP Address with fallback handling
        var ipAddress = GetClientIpAddress(httpContext);
        SafeAddProperty(logEvent, propertyFactory, "IPAddress", ipAddress);

        // Request information
        SafeAddProperty(logEvent, propertyFactory, "RequestPath", httpContext.Request.Path.Value);
        SafeAddProperty(logEvent, propertyFactory, "RequestMethod", httpContext.Request.Method);
        SafeAddProperty(logEvent, propertyFactory, "Protocol", httpContext.Request.Protocol);
        SafeAddProperty(logEvent, propertyFactory, "Scheme", httpContext.Request.Scheme);
        SafeAddProperty(logEvent, propertyFactory, "Host", httpContext.Request.Host.Value);

        // Response information
        SafeAddProperty(logEvent, propertyFactory, "StatusCode", httpContext.Response.StatusCode);

        // Correlation ID from multiple sources
        var correlationId = GetCorrelationId(httpContext);
        SafeAddProperty(logEvent, propertyFactory, "CorrelationId", correlationId);

        // Request timing if available
        if (httpContext.Items.TryGetValue("RequestStartTime", out var startTimeObj) && startTimeObj is DateTime startTime)
        {
            var duration = DateTime.UtcNow - startTime;
            SafeAddProperty(logEvent, propertyFactory, "RequestDuration", duration.TotalMilliseconds);
        }
    }

    private void EnrichWithQueryString(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, HttpContext httpContext)
    {
        if (httpContext.Request.QueryString.HasValue)
        {
            SafeAddProperty(logEvent, propertyFactory, "QueryString", httpContext.Request.QueryString.Value);
        }
    }

    private void EnrichWithUserAgent(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, HttpContext httpContext)
    {
        var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault();
        if (!string.IsNullOrEmpty(userAgent))
        {
            SafeAddProperty(logEvent, propertyFactory, "UserAgent", userAgent);
        }
    }

    private void EnrichWithRequestHeaders(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, HttpContext httpContext)
    {
        var headers = new Dictionary<string, string>();
                    foreach (var header in httpContext.Request.Headers)
            {
                if (!IsSensitiveHeader(header.Key))
                {
                    headers[header.Key] = string.Join(", ", header.Value.AsEnumerable());
                }
                else
                {
                    headers[header.Key] = "[REDACTED]";
                }
            }

        if (headers.Any())
        {
            SafeAddProperty(logEvent, propertyFactory, "RequestHeaders", headers);
        }
    }

    private void EnrichWithResponseHeaders(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, HttpContext httpContext)
    {
        var headers = new Dictionary<string, string>();
                    foreach (var header in httpContext.Response.Headers)
            {
                if (!IsSensitiveHeader(header.Key))
                {
                    headers[header.Key] = string.Join(", ", header.Value.AsEnumerable());
                }
            }

        if (headers.Any())
        {
            SafeAddProperty(logEvent, propertyFactory, "ResponseHeaders", headers);
        }
    }

    private void EnrichWithAdditionalContext(LogEvent logEvent, ILogEventPropertyFactory propertyFactory, HttpContext httpContext)
    {
        // User information if available
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            SafeAddProperty(logEvent, propertyFactory, "UserId", httpContext.User.Identity.Name);
            
            var roles = httpContext.User.Claims
                .Where(c => c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(c => c.Value)
                .ToArray();

            if (roles.Any())
            {
                SafeAddProperty(logEvent, propertyFactory, "UserRoles", roles);
            }
        }

        // Request size if available
        if (httpContext.Request.ContentLength.HasValue)
        {
            SafeAddProperty(logEvent, propertyFactory, "RequestSize", httpContext.Request.ContentLength.Value);
        }

        // Connection information
        SafeAddProperty(logEvent, propertyFactory, "RemotePort", httpContext.Connection.RemotePort);
        SafeAddProperty(logEvent, propertyFactory, "LocalPort", httpContext.Connection.LocalPort);
    }

    private string GetClientIpAddress(HttpContext httpContext)
    {
        // Check for forwarded headers (load balancers, proxies)
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ip = forwardedFor.Split(',')[0].Trim();
            if (IPAddress.TryParse(ip, out _))
                return ip;
        }

        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp) && IPAddress.TryParse(realIp, out _))
            return realIp;

        // Fallback to connection remote IP
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private string GetCorrelationId(HttpContext httpContext)
    {
        // Try multiple correlation ID headers
        var correlationHeaders = new[] { "X-Correlation-Id", "X-Request-Id", "X-Trace-Id", "TraceId" };
        
        foreach (var header in correlationHeaders)
        {
            var value = httpContext.Request.Headers[header].FirstOrDefault();
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        // Check items collection
        if (httpContext.Items.TryGetValue("X-Correlation-Id", out var correlationObj))
        {
            return correlationObj?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }

    private bool IsSensitiveHeader(string headerName)
    {
        return _sensitiveHeaders.Contains(headerName, StringComparer.OrdinalIgnoreCase);
    }
}
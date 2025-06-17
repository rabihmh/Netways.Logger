using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using System.Net;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Netways.Logger.Core.Enrichers;

/// <summary>
/// Simple HTTP context enricher that adds essential request information to log events
/// </summary>
public class HttpContextEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        try
        {
            var httpContext = GetHttpContext();
            if (httpContext == null) return;

            var request = httpContext.Request;
            var response = httpContext.Response;

            // Add essential HTTP information
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Method", request.Method));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Path", request.Path.Value));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("QueryString", request.QueryString.Value));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("StatusCode", response.StatusCode));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Protocol", request.Protocol));
            
            // Client information
            var clientIp = GetClientIpAddress(httpContext);
            if (!string.IsNullOrEmpty(clientIp))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ClientIP", clientIp));
            }

            // User agent
            var userAgent = request.Headers.UserAgent.FirstOrDefault();
            if (!string.IsNullOrEmpty(userAgent))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserAgent", userAgent));
            }

            // Content information
            if (request.ContentLength.HasValue)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ContentLength", request.ContentLength.Value));
            }

            if (!string.IsNullOrEmpty(request.ContentType))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ContentType", request.ContentType));
            }
        }
        catch
        {
            // Silently ignore errors in enrichment
        }
    }

    private HttpContext? GetHttpContext()
    {
        // Try to get HttpContext from current context
        var httpContextAccessor = ServiceLocator.ServiceProvider?.GetService<IHttpContextAccessor>();
        return httpContextAccessor?.HttpContext;
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Try X-Forwarded-For header first (for proxy scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Fall back to remote IP address
        return context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
    }
}

/// <summary>
/// Simple service locator for accessing services in enrichers
/// </summary>
public static class ServiceLocator
{
    public static IServiceProvider? ServiceProvider { get; set; }
}
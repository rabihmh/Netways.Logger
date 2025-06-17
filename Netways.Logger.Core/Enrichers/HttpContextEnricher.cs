using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace Netways.Logger.Core.Enrichers;

/// <summary>
/// Enriches log context with HTTP request information.
/// </summary>
public class HttpContextEnricher(Func<IHttpContextAccessor> httpContextAccessorFactory) : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = httpContextAccessorFactory()?.HttpContext;

        if (httpContext == null)
            return;

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var requestPath = httpContext.Request.Path;
        var requestMethod = httpContext.Request.Method;
        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].ToString();
        var statusCode = httpContext.Response.StatusCode;


        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("IPAddress", ipAddress));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestPath", requestPath));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestMethod", requestMethod));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", correlationId));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("StatusCode", statusCode));
    }
}
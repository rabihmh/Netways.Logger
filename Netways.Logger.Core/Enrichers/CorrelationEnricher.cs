using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;

namespace Netways.Logger.Core.Enrichers
{
    /// <summary>
    /// Simple correlation enricher that adds correlation ID to log events
    /// </summary>
    public class CorrelationEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            try
            {
                var httpContext = GetHttpContext();
                if (httpContext == null) return;

                var correlationId = GetOrGenerateCorrelationId(httpContext);
                if (!string.IsNullOrEmpty(correlationId))
                {
                    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", correlationId));
                }
            }
            catch
            {
                // Silently ignore errors in enrichment
            }
        }

        private HttpContext? GetHttpContext()
        {
            var httpContextAccessor = ServiceLocator.ServiceProvider?.GetService<IHttpContextAccessor>();
            return httpContextAccessor?.HttpContext;
        }

        private string GetOrGenerateCorrelationId(HttpContext httpContext)
        {
            // Try to get from context items first (set by middleware)
            if (httpContext.Items.TryGetValue("CorrelationId", out var existingId) && existingId is string existing)
            {
                return existing;
            }

            // Try to get from request headers
            var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(correlationId))
            {
                httpContext.Items["CorrelationId"] = correlationId;
                return correlationId;
            }

            // Generate new correlation ID
            correlationId = Guid.NewGuid().ToString("D");
            httpContext.Items["CorrelationId"] = correlationId;

            return correlationId;
        }
    }
}
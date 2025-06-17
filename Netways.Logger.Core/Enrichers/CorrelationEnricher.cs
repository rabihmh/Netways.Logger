using System;
using System.Collections.Generic;

namespace Netways.Logger.Core.Enrichers;

/// <summary>
/// Enriches log context with correlation IDs for distributed tracing.
/// </summary>
public class CorrelationEnricher(
    string correlationIdHeaderName = "X-Correlation-Id",
    Func<string>? correlationIdGenerator = null)
{
    private readonly string _correlationIdHeaderName = correlationIdHeaderName;
    private readonly Func<string> _correlationIdGenerator = correlationIdGenerator ?? (() => Guid.NewGuid().ToString());

    /// <summary>
    /// Gets a new correlation ID.
    /// </summary>
    /// <returns>A new correlation ID.</returns>
    public string GetNewCorrelationId()
    {
        return _correlationIdGenerator();
    }

    /// <summary>
    /// Enriches the provided dictionary with correlation information.
    /// </summary>
    /// <param name="properties">The dictionary to enrich.</param>
    /// <param name="existingCorrelationId">An optional existing correlation ID.</param>
    public void Enrich(IDictionary<string, object> properties, string? existingCorrelationId = null)
    {
        string correlationId = existingCorrelationId ?? GetNewCorrelationId();
        properties["CorrelationId"] = correlationId;
    }

    /// <summary>
    /// Gets the correlation ID header name.
    /// </summary>
    /// <returns>The correlation ID header name.</returns>
    public string GetCorrelationIdHeaderName()
    {
        return _correlationIdHeaderName;
    }
}

using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Netways.Logger.Test.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MiddlewareTestController : ControllerBase
{
    private readonly Microsoft.Extensions.Logging.ILogger<MiddlewareTestController> _logger;

    public MiddlewareTestController(Microsoft.Extensions.Logging.ILogger<MiddlewareTestController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Test correlation ID tracking across requests
    /// </summary>
    [HttpGet("correlation")]
    public IActionResult TestCorrelation()
    {
        var correlationId = HttpContext.Items["X-Correlation-Id"]?.ToString();
        var traceId = Activity.Current?.TraceId.ToString();
        var spanId = Activity.Current?.SpanId.ToString();

        _logger.LogInformation("Correlation test endpoint called with CorrelationId: {CorrelationId}", correlationId);

        return Ok(new
        {
            CorrelationId = correlationId,
            TraceId = traceId,
            SpanId = spanId,
            Headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test performance monitoring with configurable delay
    /// </summary>
    [HttpGet("performance/{delayMs:int}")]
    public async Task<IActionResult> TestPerformance(int delayMs)
    {
        _logger.LogInformation("Starting performance test with {DelayMs}ms delay", delayMs);

        var stopwatch = Stopwatch.StartNew();

        // Simulate work
        await Task.Delay(delayMs);

        stopwatch.Stop();

        _logger.LogInformation("Performance test completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

        return Ok(new
        {
            RequestedDelay = delayMs,
            ActualElapsed = stopwatch.ElapsedMilliseconds,
            CorrelationId = HttpContext.Items["X-Correlation-Id"],
            MemoryUsage = GC.GetTotalMemory(false)
        });
    }

    /// <summary>
    /// Test request body logging
    /// </summary>
    [HttpPost("request-body")]
    public IActionResult TestRequestBody([FromBody] TestRequestModel request)
    {
        _logger.LogInformation("Request body received for user {UserId}", request.UserId);

        return Ok(new
        {
            ProcessedRequest = request,
            ProcessedAt = DateTime.UtcNow,
            CorrelationId = HttpContext.Items["X-Correlation-Id"]
        });
    }

    /// <summary>
    /// Test form data logging
    /// </summary>
    [HttpPost("form-data")]
    public IActionResult TestFormData([FromForm] FormDataModel model)
    {
        _logger.LogInformation("Form data received: {Name}, {Email}", model.Name, model.Email);

        return Ok(new
        {
            Name = model.Name,
            Email = model.Email,
            HasFile = model.File != null,
            FileSize = model.File?.Length ?? 0,
            ProcessedAt = DateTime.UtcNow,
            CorrelationId = HttpContext.Items["X-Correlation-Id"]
        });
    }

    /// <summary>
    /// Test query parameter logging
    /// </summary>
    [HttpGet("query-params")]
    public IActionResult TestQueryParams(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string? sort = null,
        [FromQuery] bool includeDeleted = false)
    {
        _logger.LogInformation("Query parameters: Search={Search}, Page={Page}, Size={Size}", search, page, size);

        return Ok(new
        {
            Search = search,
            Page = page,
            Size = size,
            Sort = sort,
            IncludeDeleted = includeDeleted,
            QueryString = Request.QueryString.ToString(),
            CorrelationId = HttpContext.Items["X-Correlation-Id"]
        });
    }

    /// <summary>
    /// Test header logging and sensitive data redaction
    /// </summary>
    [HttpGet("headers")]
    public IActionResult TestHeaders()
    {
        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
        
        _logger.LogInformation("Headers endpoint called with {HeaderCount} headers", headers.Count);

        return Ok(new
        {
            HeaderCount = headers.Count,
            Headers = headers,
            UserAgent = Request.Headers.UserAgent.ToString(),
            ContentType = Request.ContentType,
            CorrelationId = HttpContext.Items["X-Correlation-Id"]
        });
    }

    /// <summary>
    /// Test large response body logging
    /// </summary>
    [HttpGet("large-response")]
    public IActionResult TestLargeResponse([FromQuery] int size = 1000)
    {
        _logger.LogInformation("Generating large response with {Size} items", size);

        var data = Enumerable.Range(1, size).Select(i => new
        {
            Id = i,
            Name = $"Item {i}",
            Description = $"This is item number {i} with some additional text to make it larger",
            CreatedAt = DateTime.UtcNow.AddDays(-i),
            IsActive = i % 2 == 0,
            Tags = new[] { $"tag{i}", $"category{i % 10}", "test" }
        }).ToList();

        return Ok(new
        {
            TotalItems = size,
            Data = data,
            GeneratedAt = DateTime.UtcNow,
            CorrelationId = HttpContext.Items["X-Correlation-Id"]
        });
    }

    /// <summary>
    /// Test error response logging
    /// </summary>
    [HttpGet("error/{statusCode:int}")]
    public IActionResult TestErrorResponse(int statusCode, [FromQuery] string? message = null)
    {
        _logger.LogWarning("Error response test: returning status {StatusCode}", statusCode);

        var errorMessage = message ?? $"Test error with status code {statusCode}";

        return StatusCode(statusCode, new
        {
            Error = errorMessage,
            StatusCode = statusCode,
            Timestamp = DateTime.UtcNow,
            CorrelationId = HttpContext.Items["X-Correlation-Id"]
        });
    }

    /// <summary>
    /// Test concurrent requests for middleware performance
    /// </summary>
    [HttpGet("concurrent/{requestId:int}")]
    public async Task<IActionResult> TestConcurrent(int requestId, [FromQuery] int delayMs = 100)
    {
        _logger.LogInformation("Concurrent request {RequestId} started", requestId);

        // Simulate concurrent processing
        await Task.Delay(delayMs);

        _logger.LogInformation("Concurrent request {RequestId} completed", requestId);

        return Ok(new
        {
            RequestId = requestId,
            DelayMs = delayMs,
            ProcessedAt = DateTime.UtcNow,
            ThreadId = Thread.CurrentThread.ManagedThreadId,
            CorrelationId = HttpContext.Items["X-Correlation-Id"]
        });
    }

    /// <summary>
    /// Test memory allocation for performance monitoring
    /// </summary>
    [HttpGet("memory-allocation")]
    public IActionResult TestMemoryAllocation([FromQuery] int sizeKb = 1024)
    {
        _logger.LogInformation("Allocating {SizeKb}KB of memory", sizeKb);

        var initialMemory = GC.GetTotalMemory(false);

        // Allocate memory
        var data = new byte[sizeKb * 1024];
        Array.Fill(data, (byte)42);

        var finalMemory = GC.GetTotalMemory(false);

        _logger.LogInformation("Memory allocation completed: {InitialMemory} -> {FinalMemory}", initialMemory, finalMemory);

        return Ok(new
        {
            AllocatedSizeKb = sizeKb,
            InitialMemory = initialMemory,
            FinalMemory = finalMemory,
            MemoryDifference = finalMemory - initialMemory,
            DataLength = data.Length,
            CorrelationId = HttpContext.Items["X-Correlation-Id"]
        });
    }
}

// DTOs for middleware testing
public class TestRequestModel
{
    public int UserId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class FormDataModel
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; } // This should be redacted in logs
    public IFormFile? File { get; set; }
} 
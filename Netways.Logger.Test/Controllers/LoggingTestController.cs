using Microsoft.AspNetCore.Mvc;
using Netways.Logger.Core;
using Netways.Logger.Model.Common;
using System.ComponentModel.DataAnnotations;

namespace Netways.Logger.Test.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoggingTestController : ControllerBase
{
    private readonly Netways.Logger.Core.ILogger _logger;
    private readonly Microsoft.Extensions.Logging.ILogger<LoggingTestController> _msLogger;

    public LoggingTestController(Netways.Logger.Core.ILogger logger, Microsoft.Extensions.Logging.ILogger<LoggingTestController> msLogger)
    {
        _logger = logger;
        _msLogger = msLogger;
    }

    /// <summary>
    /// Test basic exception logging with structured data
    /// </summary>
    [HttpGet("exception")]
    public IActionResult TestException([FromQuery] string? message = null)
    {
        try
        {
            var testData = new { UserId = 123, Action = "TestException", Timestamp = DateTime.UtcNow };
            
            // Simulate an exception
            throw new InvalidOperationException(message ?? "This is a test exception for logging demonstration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, this, new object[] { message });
            return StatusCode(500, new { Error = "An error occurred", CorrelationId = HttpContext.Items["X-Correlation-Id"] });
        }
    }

    /// <summary>
    /// Test exception logging with return value
    /// </summary>
    [HttpGet("exception-with-return")]
    public IActionResult TestExceptionWithReturn([FromQuery] int userId = 0, [FromQuery] string operation = "test")
    {
        try
        {
            if (userId <= 0)
                throw new ArgumentException("Invalid user ID provided", nameof(userId));

            // This won't be reached due to the exception
            return Ok(new { UserId = userId, Operation = operation });
        }
        catch (Exception ex)
        {
            var result = _logger.LogErrorAndReturn<string>(ex, this, new object[] { userId, operation });
            return BadRequest(new { Error = "Invalid request", Result = result });
        }
    }

    /// <summary>
    /// Test exception logging with default response
    /// </summary>
    [HttpGet("exception-with-default-response")]
    public DefaultResponse<UserData> TestExceptionWithDefaultResponse([FromQuery] int userId = 0)
    {
        try
        {
            if (userId <= 0)
                throw new ArgumentException("User ID must be greater than 0", nameof(userId));

            // This won't be reached due to the exception
            return new DefaultResponse<UserData>
            {
                IsSuccess = true,
                Result = new UserData { Id = userId, Name = "Test User" }
            };
        }
        catch (Exception ex)
        {
            return _logger.LogErrorAndReturnDefaultResponse<UserData>(ex, this, new object[] { userId });
        }
    }

    /// <summary>
    /// Test custom message logging
    /// </summary>
    [HttpPost("custom-message")]
    public IActionResult TestCustomMessage([FromBody] CustomMessageRequest request)
    {
        _logger.LogCustomMessage($"Custom message received: {request.Message} from user {request.UserId}", this);
        
        return Ok(new { 
            Success = true, 
            Message = "Custom message logged successfully",
            CorrelationId = HttpContext.Items["X-Correlation-Id"]
        });
    }

    /// <summary>
    /// Test API logging functionality
    /// </summary>
    [HttpPost("api-log")]
    public DefaultResponse<bool> TestApiLog([FromBody] ApiLogRequest request)
    {
        var payload = new Dictionary<string, string?>
        {
            ["UserId"] = request.UserId.ToString(),
            ["Action"] = request.Action,
            ["Data"] = request.Data,
            ["Timestamp"] = DateTime.UtcNow.ToString("O"),
            ["ClientIp"] = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        return _logger.LogByApi(payload, request.IsException);
    }

    /// <summary>
    /// Test slow request performance monitoring
    /// </summary>
    [HttpGet("slow-request")]
    public async Task<IActionResult> TestSlowRequest([FromQuery] int delayMs = 500)
    {
        _logger.LogCustomMessage($"Starting slow request with {delayMs}ms delay", this);
        
        // Simulate slow processing
        await Task.Delay(delayMs);
        
        _logger.LogCustomMessage($"Completed slow request after {delayMs}ms", this);
        
        return Ok(new { 
            Message = $"Request completed after {delayMs}ms delay",
            CorrelationId = HttpContext.Items["X-Correlation-Id"]
        });
    }

    /// <summary>
    /// Test request/response logging with body data
    /// </summary>
    [HttpPost("echo")]
    public IActionResult TestEcho([FromBody] EchoRequest request)
    {
        _logger.LogCustomMessage($"Echo request received for user: {request.UserId}", this);
        
        var response = new EchoResponse
        {
            OriginalMessage = request.Message,
            ProcessedMessage = request.Message?.ToUpper(),
            UserId = request.UserId,
            ProcessedAt = DateTime.UtcNow,
            CorrelationId = HttpContext.Items["X-Correlation-Id"]?.ToString()
        };

        return Ok(response);
    }

    /// <summary>
    /// Test multiple parameter logging
    /// </summary>
    [HttpGet("multiple-params")]
    public IActionResult TestMultipleParams(
        [FromQuery] int id,
        [FromQuery] string name,
        [FromQuery] DateTime? date = null,
        [FromQuery] bool active = true)
    {
        try
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name is required", nameof(name));

            var result = new
            {
                Id = id,
                Name = name,
                Date = date ?? DateTime.UtcNow,
                Active = active,
                ProcessedAt = DateTime.UtcNow
            };

            _logger.LogCustomMessage($"Multiple parameters processed successfully for {name}", this);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(_logger.LogErrorAndReturnDefaultResponse<object>(ex, this, new object[] { id, name, date, active }));
        }
    }

    /// <summary>
    /// Test file upload logging
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> TestFileUpload(IFormFile file, [FromForm] string description = "")
    {
        try
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is required", nameof(file));

            _logger.LogCustomMessage($"File upload started: {file.FileName} ({file.Length} bytes)", this);

            // Simulate file processing
            using var stream = file.OpenReadStream();
            var buffer = new byte[1024];
            var totalBytes = 0;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                totalBytes += bytesRead;
                // Simulate processing
                await Task.Delay(10);
            }

            _logger.LogCustomMessage($"File upload completed: {file.FileName} ({totalBytes} bytes processed)", this);

            return Ok(new
            {
                FileName = file.FileName,
                Size = file.Length,
                ContentType = file.ContentType,
                Description = description,
                ProcessedBytes = totalBytes,
                CorrelationId = HttpContext.Items["X-Correlation-Id"]
            });
        }
        catch (Exception ex)
        {
            return BadRequest(_logger.LogErrorAndReturnDefaultResponse<object>(ex, this, new object[] { file?.FileName, description }));
        }
    }

    /// <summary>
    /// Test sensitive data redaction
    /// </summary>
    [HttpPost("sensitive-data")]
    public IActionResult TestSensitiveData([FromBody] SensitiveDataRequest request)
    {
        _logger.LogCustomMessage($"Processing sensitive data for user: {request.UserId}", this);
        
        return Ok(new
        {
            UserId = request.UserId,
            Message = "Sensitive data processed (check logs for redaction)",
            CorrelationId = HttpContext.Items["X-Correlation-Id"]
        });
    }

    /// <summary>
    /// Test memory usage and performance monitoring
    /// </summary>
    [HttpGet("memory-test")]
    public IActionResult TestMemoryUsage([FromQuery] int iterations = 1000)
    {
        _logger.LogCustomMessage($"Starting memory test with {iterations} iterations", this);
        
        var data = new List<byte[]>();
        
        for (int i = 0; i < iterations; i++)
        {
            // Allocate memory to test monitoring
            data.Add(new byte[1024]);
            
            if (i % 100 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        var memoryUsed = GC.GetTotalMemory(false);
        
        _logger.LogCustomMessage($"Memory test completed: {iterations} iterations, {memoryUsed} bytes allocated", this);
        
        return Ok(new
        {
            Iterations = iterations,
            MemoryUsed = memoryUsed,
            DataSize = data.Count,
            CorrelationId = HttpContext.Items["X-Correlation-Id"]
        });
    }
}

// DTOs for testing
public class CustomMessageRequest
{
    [Required]
    public string Message { get; set; } = string.Empty;
    public int UserId { get; set; }
}

public class ApiLogRequest
{
    public int UserId { get; set; }
    [Required]
    public string Action { get; set; } = string.Empty;
    public string? Data { get; set; }
    public bool IsException { get; set; }
}

public class EchoRequest
{
    public string? Message { get; set; }
    public int UserId { get; set; }
}

public class EchoResponse
{
    public string? OriginalMessage { get; set; }
    public string? ProcessedMessage { get; set; }
    public int UserId { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string? CorrelationId { get; set; }
}

public class SensitiveDataRequest
{
    public int UserId { get; set; }
    public string? Password { get; set; }
    public string? Token { get; set; }
    public string? ApiKey { get; set; }
    public string? Secret { get; set; }
}

public class UserData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
} 
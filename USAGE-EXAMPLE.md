# Enhanced Logger Usage Examples

## 🚀 How to Use the Enhanced Structured Logger

This guide shows how to properly set up and use the enhanced logger system with dependency injection and structured logging.

---

## 📋 Setup and Configuration

### **1. Register Services in Program.cs or Startup.cs**

```csharp
using Netways.Logger.Core;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add HTTP context accessor (required for logger)
        builder.Services.AddHttpContextAccessor();

        // Register all logger services and get the LoggerBuilder
        var loggerBuilder = builder.Services.AddLoggerServices();

        // Configure your logging sinks
        loggerBuilder
            .WriteToFile(@"C:\Logs\MyApplication")           // Enhanced file logging with structured formatting
            .WriteToSeq("http://localhost:5341")             // Seq for structured log analysis
            .WriteToAppInsights()                             // Azure Application Insights
            .WriteToEmail(new EmailSinkOptions               // Email for critical errors
            {
                From = "noreply@myapp.com",
                To = new[] { "admin@myapp.com" },
                Subject = "Critical Error in MyApp",
                // ... other email settings
            });

        var app = builder.Build();

        // Build the logger with the final service provider
        loggerBuilder.Build(app.Services);

        // Load logger configuration
        app.AddLoggerApplications(builder.Configuration);

        app.Run();
    }
}
```

### **2. Alternative Setup (If you need more control)**

```csharp
public static void Main(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    
    // Register services first
    builder.Services.AddHttpContextAccessor();
    var loggerBuilder = builder.Services.AddLoggerServices();
    
    var app = builder.Build();
    
    // Now you can create a LoggerBuilder with the final service provider
    var enhancedLoggerBuilder = builder.Services.CreateLoggerBuilder(app.Services);
    
    enhancedLoggerBuilder
        .WriteToFile(@"C:\Logs\MyApplication")
        .WriteToSeq("http://localhost:5341")
        .WriteToAppInsights()
        .Build(app.Services);
    
    app.AddLoggerApplications(builder.Configuration);
    app.Run();
}
```

---

## 🔧 Using the Logger in Your Code

### **1. Inject the Logger in Your Controllers/Services**

```csharp
using Netways.Logger.Core;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly ILogger _logger;

    public WeatherController(ILogger logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetWeather(string city)
    {
        try
        {
            // Your business logic here
            var weather = await GetWeatherData(city);
            
            // Log custom message
            _logger.LogCustomMessage($"Weather data retrieved successfully for city: {city}", this);
            
            return Ok(weather);
        }
        catch (Exception ex)
        {
            // Log exception with structured data
            _logger.LogError(ex, this, new object[] { city });
            
            // Or return a structured response
            return BadRequest(_logger.LogErrorAndReturnDefaultResponse<WeatherData>(ex, this, new object[] { city }));
        }
    }

    [HttpPost("batch")]
    public async Task<IActionResult> ProcessBatchWeather([FromBody] BatchRequest request)
    {
        try
        {
            var results = await ProcessBatchData(request);
            return Ok(results);
        }
        catch (ArgumentException ex)
        {
            // Log and return with structured response
            var result = _logger.LogErrorAndReturn<List<WeatherData>>(ex, this, new object[] { request });
            return BadRequest(new { Error = "Invalid request parameters", Data = result });
        }
        catch (Exception ex)
        {
            // Return structured error response
            return BadRequest(_logger.LogErrorAndReturnDefaultResponse<List<WeatherData>>(ex, this, new object[] { request }));
        }
    }
}
```

### **2. Using LogByApi for External API Logging**

```csharp
public class ExternalApiService
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    public ExternalApiService(ILogger logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<ApiResponse> CallExternalApi(string endpoint, object data)
    {
        var payload = new Dictionary<string, string?>
        {
            ["Endpoint"] = endpoint,
            ["RequestData"] = JsonSerializer.Serialize(data),
            ["Timestamp"] = DateTime.UtcNow.ToString("O"),
            ["RequestId"] = Guid.NewGuid().ToString()
        };

        try
        {
            var response = await _httpClient.PostAsync(endpoint, JsonContent.Create(data));
            
            payload["ResponseStatus"] = response.StatusCode.ToString();
            payload["IsSuccess"] = response.IsSuccessStatusCode.ToString();
            
            // Log successful API call
            _logger.LogByApi(payload, isException: false);
            
            return new ApiResponse { Success = true, Data = await response.Content.ReadAsStringAsync() };
        }
        catch (Exception ex)
        {
            payload["ErrorMessage"] = ex.Message;
            payload["ErrorType"] = ex.GetType().Name;
            
            // Log API exception
            _logger.LogByApi(payload, isException: true);
            
            throw;
        }
    }
}
```

---

## 📊 What You Get in Your Logs

### **File Logs (Enhanced Formatting)**

**Exception Log Example:**
```
==================== EXCEPTION LOG ====================
Timestamp: 2024-01-15 14:30:25.123 +00:00
Level: Error
Template: Exception occurred in {Source}.{FunctionName}
Rendered Message: Exception occurred in WeatherController.GetWeather
--------------------------------------------------------
Exception Source: WeatherController
Function Name: GetWeather
Exception Message: Weather service unavailable
Correlation ID: abc123-def456-ghi789
Request Route: /api/weather
Is CRM Validation: False
Function Parameters:
{
  "city": "London"
}
Stack Trace: at WeatherController.GetWeather(String city) in...
Inner Exception Message: Connection timeout
Inner Stack Trace: at HttpClient.GetAsync...
========================================================
```

**Request Log Example:**
```
===================== REQUEST LOG =====================
Timestamp: 2024-01-15 14:30:20.456 +00:00
Level: Information
Template: Custom message from {Source}.{FunctionName}: {Message}
Rendered Message: Custom message from WeatherController.GetWeather: Weather data retrieved successfully for city: London
--------------------------------------------------------
Request Source: WeatherController
Function Name: GetWeather
Custom Message: Weather data retrieved successfully for city: London
Correlation ID: abc123-def456-ghi789
Request Route: /api/weather
========================================================
```

### **Structured Data for Seq/Application Insights**

Your logs will appear in Seq and Application Insights with full structured data:

```json
{
  "Timestamp": "2024-01-15T14:30:25.123Z",
  "Level": "Error",
  "MessageTemplate": "Exception occurred in {Source}.{FunctionName}",
  "Source": "WeatherController",
  "FunctionName": "GetWeather",
  "Message": "Weather service unavailable",
  "FunctionParameters": {
    "city": "London"
  },
  "CorrelationId": "abc123-def456-ghi789",
  "Route": "/api/weather",
  "IsCrmValidation": false,
  "Type": "Exception"
}
```

---

## 🔧 Adding Custom Formatters

### **Create a Custom Formatter**

```csharp
using Netways.Logger.Core.Formatters;
using Serilog.Events;
using System.Text;

public class PerformanceLogFormatter : BaseLogEventFormatter
{
    public override int Priority => 15; // Higher priority than Request, lower than Exception

    public override bool CanFormat(LogEvent logEvent)
    {
        return HasPropertyWithValue(logEvent, "Type", "Performance") ||
               logEvent.Properties.ContainsKey("ElapsedMilliseconds");
    }

    public override void Format(LogEvent logEvent, StringBuilder message)
    {
        message.AppendLine("================== PERFORMANCE LOG ==================");
        AppendBasicLogInfo(message, logEvent);
        message.AppendLine("-----------------------------------------------------");
        
        AppendPropertyIfExists(message, logEvent, "Operation", "Operation Name");
        AppendPropertyIfExists(message, logEvent, "ElapsedMilliseconds", "Duration (ms)");
        AppendPropertyIfExists(message, logEvent, "MemoryUsed", "Memory Usage");
        AppendPropertyIfExists(message, logEvent, "DatabaseQueries", "DB Queries Count");
        
        message.AppendLine("=====================================================");
    }
}
```

### **Register Custom Formatter**

```csharp
// In Program.cs
builder.Services.AddSingleton<PerformanceLogFormatter>();
builder.Services.AddSingleton<ILogEventFormatter, PerformanceLogFormatter>();

// The LogFormatterManager will automatically pick it up!
```

---

## ✅ Benefits You Get

1. **File Logs**: Beautiful, professional formatting with clear sections
2. **Seq/App Insights**: Full structured data for powerful querying
3. **Email Alerts**: Only non-CRM validation errors (configurable)
4. **Extensible**: Easy to add custom formatters
5. **Type Safe**: Compile-time checking with structured log objects
6. **Performance**: No string concatenation overhead
7. **Professional**: Industry-standard logging patterns

---

## 🎯 Best Practices

1. **Always pass parameters**: `_logger.LogError(ex, this, new object[] { param1, param2 })`
2. **Use structured objects**: Let the system handle formatting
3. **Custom formatters**: Create specific formatters for special log types
4. **Correlation IDs**: Automatically handled by the HTTP context enricher
5. **Exception handling**: Use the structured response methods for consistent error handling

This enhanced logging system provides enterprise-grade logging capabilities while maintaining simplicity and performance! 🚀 
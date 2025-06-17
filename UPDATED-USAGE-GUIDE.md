# Enhanced Logger - Complete Integration Guide

## 🚀 How to Properly Integrate the Enhanced Logger System

This guide shows the **correct way** to set up and use the enhanced structured logger with dependency injection.

---

## 📋 Setup and Configuration

### **1. Basic Setup in Program.cs**

```csharp
using Netways.Logger.Core;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add HTTP context accessor (REQUIRED for logger)
        builder.Services.AddHttpContextAccessor();

        // Register all logger services
        var loggerBuilder = builder.Services.AddLoggerServices();

        var app = builder.Build();

        // Configure your logging sinks with enhanced formatting
        var enhancedLoggerBuilder = ServiceCollectionExtension
            .CreateLoggerBuilderWithServices(app.Services);

        enhancedLoggerBuilder
            .WriteToFile(@"C:\Logs\MyApplication")           // Enhanced file logging
            .WriteToSeq("http://localhost:5341")             // Seq for structured analysis
            .WriteToAppInsights()                             // Azure Application Insights
            .WriteToEmail(new EmailSinkOptions               // Email for critical errors
            {
                From = "noreply@myapp.com",
                To = new[] { "admin@myapp.com" },
                Subject = "Critical Error in MyApp",
                // ... other email settings
            })
            .Build(app.Services); // Build with final service provider

        // Load logger configuration
        app.AddLoggerApplications(builder.Configuration);

        app.Run();
    }
}
```

### **2. Alternative Setup (Simpler approach)**

```csharp
public static void Main(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);
    
    // Register services
    builder.Services.AddHttpContextAccessor();
    var loggerBuilder = builder.Services.AddLoggerServices();
    
    var app = builder.Build();
    
    // Configure logging with the built app services
    loggerBuilder
        .WithServiceProvider(app.Services)  // Provide service provider
        .WriteToFile(@"C:\Logs\MyApplication")
        .WriteToSeq("http://localhost:5341")
        .WriteToAppInsights()
        .Build(app.Services);
    
    app.AddLoggerApplications(builder.Configuration);
    app.Run();
}
```

---

## 🎯 What Gets Registered in DI

The `AddLoggerServices()` method automatically registers:

```csharp
// Core Logger Services
services.AddSingleton<ILogger, Logger>();
services.AddSingleton<ILoggerConfig, LoggerConfig>();
services.AddSingleton<HttpContextEnricher>();

// Enhanced Formatter System
services.AddSingleton<ExceptionLogFormatter>();
services.AddSingleton<RequestLogFormatter>();
services.AddSingleton<DefaultLogFormatter>();
services.AddSingleton<LogFormatterManager>();
services.AddSingleton<SerilogFileFormatter>();
```

---

## 🔧 Using the Logger in Your Code

### **1. Basic Usage in Controllers/Services**

```csharp
using Netways.Logger.Core;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly ILogger _logger;

    public ProductController(ILogger logger)
    {
        _logger = logger; // Injected from DI container
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        try
        {
            var product = await GetProductById(id);
            
            // Log success message with structured data
            _logger.LogCustomMessage($"Product retrieved successfully: {product.Name}", this);
            
            return Ok(product);
        }
        catch (ProductNotFoundException ex)
        {
            // Log error with structured exception data
            _logger.LogError(ex, this, new object[] { id });
            return NotFound();
        }
        catch (Exception ex)
        {
            // Return structured error response
            return BadRequest(_logger.LogErrorAndReturnDefaultResponse<Product>(ex, this, new object[] { id }));
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        try
        {
            var product = await CreateNewProduct(request);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
        catch (ValidationException ex)
        {
            // Log and return structured response for validation errors
            var result = _logger.LogErrorAndReturn<Product>(ex, this, new object[] { request });
            return BadRequest(new { Error = "Validation failed", Details = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(_logger.LogErrorAndReturnDefaultResponse<Product>(ex, this, new object[] { request }));
        }
    }
}
```

### **2. API Integration Logging**

```csharp
public class PaymentService
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    public PaymentService(ILogger logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<PaymentResult> ProcessPayment(PaymentRequest request)
    {
        var apiPayload = new Dictionary<string, string?>
        {
            ["PaymentId"] = request.PaymentId,
            ["Amount"] = request.Amount.ToString(),
            ["Currency"] = request.Currency,
            ["Endpoint"] = "/api/payments/process",
            ["Timestamp"] = DateTime.UtcNow.ToString("O")
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/payments/process", request);
            
            apiPayload["ResponseStatus"] = response.StatusCode.ToString();
            apiPayload["Success"] = response.IsSuccessStatusCode.ToString();
            
            // Log successful API call
            _logger.LogByApi(apiPayload, isException: false);
            
            return await response.Content.ReadFromJsonAsync<PaymentResult>();
        }
        catch (Exception ex)
        {
            apiPayload["ErrorMessage"] = ex.Message;
            apiPayload["ErrorType"] = ex.GetType().Name;
            
            // Log API exception
            _logger.LogByApi(apiPayload, isException: true);
            
            throw;
        }
    }
}
```

---

## 📊 What You Get in Different Sinks

### **1. File Logs (Beautiful Professional Formatting)**

**Exception Log Output:**
```
==================== EXCEPTION LOG ====================
Timestamp: 2024-01-15 14:30:25.123 +00:00
Level: Error
Template: Exception occurred in {Source}.{FunctionName}
Rendered Message: Exception occurred in ProductController.GetProduct
--------------------------------------------------------
Exception Source: ProductController
Function Name: GetProduct
Exception Message: Product not found with ID: 123
Correlation ID: abc123-def456-ghi789
Request Route: /api/products/123
Is CRM Validation: False
Function Parameters:
{
  "id": 123
}
Stack Trace: at ProductController.GetProduct(Int32 id) in...
Inner Exception Message: Database connection timeout
Inner Stack Trace: at DbContext.FindAsync...
========================================================
```

**Request Log Output:**
```
===================== REQUEST LOG =====================
Timestamp: 2024-01-15 14:30:20.456 +00:00
Level: Information
Template: Custom message from {Source}.{FunctionName}: {Message}
Rendered Message: Custom message from ProductController.GetProduct: Product retrieved successfully: Laptop Pro
--------------------------------------------------------
Request Source: ProductController
Function Name: GetProduct
Custom Message: Product retrieved successfully: Laptop Pro
Correlation ID: abc123-def456-ghi789
Request Route: /api/products/123
========================================================
```

### **2. Seq/Application Insights (Full Structured Data)**

```json
{
  "Timestamp": "2024-01-15T14:30:25.123Z",
  "Level": "Error",
  "MessageTemplate": "Exception occurred in {Source}.{FunctionName}",
  "Properties": {
    "Source": "ProductController",
    "FunctionName": "GetProduct",
    "Message": "Product not found with ID: 123",
    "FunctionParameters": {
      "id": 123
    },
    "CorrelationId": "abc123-def456-ghi789",
    "Route": "/api/products/123",
    "IsCrmValidation": false,
    "Type": "Exception"
  }
}
```

### **3. Email Alerts (Only Non-CRM Critical Errors)**

Automatically sent for:
- System exceptions (not CRM validation errors)
- Critical errors only
- Includes full structured data

---

## 🔧 Adding Custom Formatters

### **Create Custom Formatter**

```csharp
using Netways.Logger.Core.Formatters;

public class PerformanceLogFormatter : BaseLogEventFormatter
{
    public override int Priority => 15; // Between Exception(10) and Request(20)

    public override bool CanFormat(LogEvent logEvent)
    {
        return HasPropertyWithValue(logEvent, "Type", "Performance");
    }

    public override void Format(LogEvent logEvent, StringBuilder message)
    {
        message.AppendLine("================== PERFORMANCE LOG ==================");
        AppendBasicLogInfo(message, logEvent);
        message.AppendLine("-----------------------------------------------------");
        
        AppendPropertyIfExists(message, logEvent, "Operation");
        AppendPropertyIfExists(message, logEvent, "Duration", "Duration (ms)");
        AppendPropertyIfExists(message, logEvent, "MemoryUsage", "Memory (MB)");
        
        message.AppendLine("=====================================================");
    }
}
```

### **Register Custom Formatter**

```csharp
// In Program.cs
builder.Services.AddSingleton<PerformanceLogFormatter>();

// The LogFormatterManager will automatically detect and use it!
```

---

## ✅ Key Benefits You Get

### **1. File Logs:**
- ✅ Professional formatting with clear sections
- ✅ Exception logs with "EXCEPTION LOG" headers  
- ✅ Request logs with "REQUEST LOG" headers
- ✅ JSON parameters properly formatted and indented
- ✅ Stack traces organized and readable

### **2. Structured Sinks (Seq/App Insights):**
- ✅ Full structured data for advanced querying
- ✅ Correlation tracking across requests
- ✅ Rich filtering and analytics capabilities

### **3. Development Experience:**
- ✅ Type-safe structured logging
- ✅ Automatic dependency injection
- ✅ Professional error handling
- ✅ Easy to extend with custom formatters

### **4. Production Ready:**
- ✅ Performance optimized (no string concatenation)
- ✅ Error-safe with fallback mechanisms  
- ✅ Email alerts for critical issues only
- ✅ SOLID principles and clean architecture

---

## 🎯 Best Practices

1. **Always use dependency injection**: Let the container manage logger instances
2. **Pass method parameters**: `_logger.LogError(ex, this, new object[] { param1, param2 })`
3. **Use structured responses**: `LogErrorAndReturnDefaultResponse<T>()` for consistent API errors
4. **Custom formatters**: Create specific formatters for special log types
5. **Correlation IDs**: Automatically handled by HTTP context enricher
6. **File organization**: Separate error and request logs into different directories

---

## 🚀 Migration from Old System

### **Before (Old StringBuilder approach):**
```csharp
var sb = new StringBuilder();
sb.AppendLine($"Error in {className}");
sb.AppendLine($"Message: {ex.Message}");
Log.Error(sb.ToString());
```

### **After (New structured approach):**
```csharp
_logger.LogError(ex, this, new object[] { parameters });
// Automatically creates structured ExceptionLogData with professional formatting!
```

The logger is now enterprise-ready with professional formatting, structured data, and clean architecture! 🎉 
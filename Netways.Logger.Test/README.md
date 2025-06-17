# Netways.Logger.Test - Comprehensive Testing Suite

This project provides a comprehensive testing suite for the enhanced Netways.Logger system. It demonstrates all the advanced logging features, middleware capabilities, and structured logging patterns implemented in the core library.

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- (Optional) Seq for structured log viewing: https://datalust.co/seq

### Running the Application

1. **Start the application:**
   ```bash
   dotnet run --project Netways.Logger.Test
   ```

2. **Access Swagger UI:**
   Open https://localhost:7xxx/swagger (port varies) to explore all test endpoints interactively.

3. **Optional - Start Seq for enhanced log viewing:**
   ```bash
   docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
   ```
   Then visit http://localhost:5341 to view structured logs.

## 📋 Test Categories

### 1. Core Logger Testing (`/api/LoggingTest`)

#### Exception Logging Tests
- **`GET /api/LoggingTest/exception`** - Test basic exception logging with structured data
- **`GET /api/LoggingTest/exception-with-return`** - Test LogErrorAndReturn functionality
- **`GET /api/LoggingTest/exception-with-default-response`** - Test LogErrorAndReturnDefaultResponse

#### Custom Logging Tests
- **`POST /api/LoggingTest/custom-message`** - Test custom message logging
- **`POST /api/LoggingTest/api-log`** - Test API logging functionality

#### Performance & Memory Tests
- **`GET /api/LoggingTest/slow-request`** - Test performance monitoring with configurable delays
- **`GET /api/LoggingTest/memory-test`** - Test memory usage monitoring

#### Data Processing Tests
- **`POST /api/LoggingTest/echo`** - Test request/response logging with body data
- **`GET /api/LoggingTest/multiple-params`** - Test multiple parameter logging
- **`POST /api/LoggingTest/upload`** - Test file upload logging
- **`POST /api/LoggingTest/sensitive-data`** - Test sensitive data redaction

### 2. Middleware Testing (`/api/MiddlewareTest`)

#### Correlation & Tracing Tests
- **`GET /api/MiddlewareTest/correlation`** - Test correlation ID tracking and distributed tracing
- **`GET /api/MiddlewareTest/headers`** - Test header logging and sensitive data redaction

#### Performance Monitoring Tests
- **`GET /api/MiddlewareTest/performance/{delayMs}`** - Test performance monitoring with specific delays
- **`GET /api/MiddlewareTest/memory-allocation`** - Test memory allocation monitoring
- **`GET /api/MiddlewareTest/concurrent/{requestId}`** - Test concurrent request handling

#### Request/Response Logging Tests
- **`POST /api/MiddlewareTest/request-body`** - Test request body logging
- **`POST /api/MiddlewareTest/form-data`** - Test form data and file upload logging
- **`GET /api/MiddlewareTest/query-params`** - Test query parameter logging
- **`GET /api/MiddlewareTest/large-response`** - Test large response body logging

#### Error Handling Tests
- **`GET /api/MiddlewareTest/error/{statusCode}`** - Test error response logging with various status codes

### 3. System Tests

#### Health & Diagnostics
- **`GET /health`** - Basic health check (excluded from detailed logging)
- **`GET /test-correlation`** - Simple correlation ID test

## 🧪 Test Scenarios

### Scenario 1: Basic Exception Handling
```bash
# Test basic exception logging
curl -X GET "https://localhost:7xxx/api/LoggingTest/exception?message=Custom%20error%20message"

# Check logs for:
# - Structured exception data
# - Correlation ID tracking
# - Stack trace information
# - Function parameter capture
```

### Scenario 2: Performance Monitoring
```bash
# Test slow request detection
curl -X GET "https://localhost:7xxx/api/LoggingTest/slow-request?delayMs=200"

# Check logs for:
# - Request timing information
# - Performance threshold alerts
# - Memory usage tracking
```

### Scenario 3: Correlation Tracking
```bash
# Send request with custom correlation ID
curl -X GET "https://localhost:7xxx/api/MiddlewareTest/correlation" \
     -H "X-Correlation-Id: test-12345"

# Check logs for:
# - Correlation ID propagation
# - Distributed tracing integration
# - Activity context information
```

### Scenario 4: Sensitive Data Redaction
```bash
# Test sensitive data handling
curl -X POST "https://localhost:7xxx/api/LoggingTest/sensitive-data" \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer secret-token" \
     -d '{"userId": 123, "password": "secret123", "apiKey": "api-key-456"}'

# Check logs for:
# - Redacted sensitive fields
# - Safe header handling
# - User context preservation
```

### Scenario 5: File Upload Logging
```bash
# Test file upload with form data
curl -X POST "https://localhost:7xxx/api/LoggingTest/upload" \
     -F "file=@test-file.txt" \
     -F "description=Test file upload"

# Check logs for:
# - File metadata logging
# - Form data capture
# - Upload progress tracking
```

### Scenario 6: Concurrent Request Testing
```bash
# Test concurrent requests (run multiple times simultaneously)
for i in {1..5}; do
  curl -X GET "https://localhost:7xxx/api/MiddlewareTest/concurrent/$i?delayMs=100" &
done
wait

# Check logs for:
# - Individual correlation IDs
# - Performance monitoring per request
# - Thread safety validation
```

## 📊 Log Analysis

### Using Seq (Recommended)
1. Start Seq: `docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest`
2. Visit http://localhost:5341
3. Use queries like:
   - `Type = "Exception"` - View all exceptions
   - `@mt like '%slow%'` - Find slow requests
   - `CorrelationId = "specific-id"` - Trace specific requests
   - `Level = "Error"` - View all errors

### Using File Logs
Logs are written to:
- `logs/ErrorsLogs/error-YYYY-MM-DD.log` - Exception logs
- `logs/RequestsLogs/request-YYYY-MM-DD.log` - Request logs
- `logs/test-YYYY-MM-DD.log` - All logs

### Using Console Output
Real-time log viewing with structured data and correlation tracking.

## 🔧 Configuration

### Key Configuration Sections

#### Logger Configuration
```json
{
  "LoggerConfig": {
    "GenericError": false,
    "LogRequest": true,
    "LogResponse": true
  }
}
```

#### Enricher Configuration
```json
{
  "Enrichers": {
    "HttpContext": {
      "Enabled": true,
      "IncludeRequestHeaders": true,
      "SensitiveHeaders": ["Authorization", "Cookie"]
    },
    "Environment": {
      "Enabled": true,
      "IncludeSystemInfo": true
    },
    "Correlation": {
      "Enabled": true,
      "GenerateIfMissing": true
    }
  }
}
```

#### Middleware Configuration
```json
{
  "Middleware": {
    "RequestLogging": {
      "Enabled": true,
      "LogBody": true,
      "MaxBodySize": 1048576
    },
    "PerformanceMonitoring": {
      "Enabled": true,
      "SlowRequestThresholdMs": 100
    },
    "Security": {
      "RedactSensitiveData": true,
      "SensitiveHeaders": ["Authorization", "Cookie"]
    }
  }
}
```

## 🎯 What to Look For

### Successful Implementation Indicators

1. **Structured Logging**: All logs contain structured data, not concatenated strings
2. **Correlation Tracking**: Each request has a unique correlation ID that persists across all log entries
3. **Performance Monitoring**: Slow requests are automatically detected and logged
4. **Sensitive Data Protection**: Passwords, tokens, and API keys are redacted in logs
5. **Rich Context**: Logs include environment info, HTTP context, and user details
6. **Error Resilience**: Logging continues to work even when individual components fail
7. **Memory Efficiency**: No memory leaks during extended testing

### Common Test Patterns

1. **Make a request** → **Check correlation ID** → **Verify structured data**
2. **Send sensitive data** → **Confirm redaction** → **Validate security**
3. **Trigger slow request** → **Check performance alerts** → **Verify monitoring**
4. **Generate exception** → **Review stack trace** → **Confirm parameter capture**

## 🚨 Troubleshooting

### Common Issues

1. **Seq not receiving logs**: Check if Seq is running on port 5341
2. **Missing correlation IDs**: Verify middleware is properly registered
3. **Sensitive data not redacted**: Check configuration for sensitive field names
4. **Performance alerts not firing**: Adjust SlowRequestThresholdMs in configuration

### Debug Mode
Set `"Serilog:MinimumLevel:Default": "Debug"` in appsettings.json for verbose logging.

## 📈 Performance Benchmarks

Expected performance characteristics:
- **Request overhead**: < 5ms for typical requests
- **Memory overhead**: < 100KB per request
- **Log throughput**: > 10,000 events/second
- **Correlation tracking**: < 1ms overhead

## 🔍 Advanced Testing

### Load Testing
Use tools like Apache Bench or k6 to test under load:
```bash
# Test 1000 requests with 10 concurrent users
ab -n 1000 -c 10 https://localhost:7xxx/api/MiddlewareTest/performance/50
```

### Integration Testing
The test project can be used as a reference for integrating the logger into existing applications.

---

This test suite provides comprehensive coverage of all enhanced logging features. Use it to validate functionality, performance, and integration in your environment. 
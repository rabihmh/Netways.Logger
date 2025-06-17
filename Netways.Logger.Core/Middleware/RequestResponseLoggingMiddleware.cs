using Microsoft.AspNetCore.Http;
using Netways.Logger.Model.Configurations;
using Netways.Logger.Model.Logger;
using Serilog;
using Serilog.Context;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Netways.Logger.Core.Middleware
{
    public class RequestResponseLoggingMiddleware(RequestDelegate next, ILoggerConfig loggerConfig)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var traceId = Guid.NewGuid().ToString();

            // Set correlation ID in context and headers
            context.Items["X-Correlation-Id"] = traceId;
            context.Response.Headers.Append("X-Correlation-Id", traceId);
            context.Response.Headers.Append("Access-Control-Expose-Headers", "X-Correlation-Id");

            var stopwatch = Stopwatch.StartNew();

            var originalResponseBodyStream = context.Response.Body;

            using (LogContext.PushProperty("CorrelationId", traceId))
            {
                try
                {
                    // Log request details
                    var requestDetails = await CaptureRequestDetails(context, loggerConfig);

                    using (var responseBody = new MemoryStream())
                    {
                        context.Response.Body = responseBody;

                        await next(context);

                        stopwatch.Stop();

                        // Log response details
                        var responseDetails = await CaptureResponseDetails(context, stopwatch.ElapsedMilliseconds, loggerConfig);

                        // Log the complete request-response cycle
                        LogRequestResponseCycle(requestDetails, responseDetails);

                        responseBody.Position = 0;
                        await responseBody.CopyToAsync(originalResponseBodyStream);
                    }
                }
                catch (Exception)
                {
                    stopwatch.Stop();

                    if (context.Response.Body is MemoryStream memoryStream && memoryStream.CanSeek)
                    {
                        memoryStream.Position = 0;
                    }

                    throw; // Allow global exception handler to process
                }
                finally
                {
                    context.Response.Body = originalResponseBodyStream;
                }
            }
        }

        private async Task<RequestLogDetails> CaptureRequestDetails(HttpContext context, ILoggerConfig loggerConfig)
        {
            if (!loggerConfig.LogRequest)
            {
                context.Request.EnableBuffering();
                context.Request.Body.Seek(0, SeekOrigin.Begin);
                return new RequestLogDetails();
            }

            context.Request.EnableBuffering();

            // Capture request body
            string requestBody = string.Empty;
            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            // Capture form files and form data
            var fileNames = new List<string>();
            var formData = new Dictionary<string, string>();

            if (context.Request.HasFormContentType)
            {
                fileNames = context.Request.Form.Files
                    .Select(f => f.FileName)
                    .ToList();

                //formData = context.Request.Form
                //    .Where(f => f.Value.Count > 0)
                //    .ToDictionary(k => k.Key, v => v.Value.ToString());
            }

            return new RequestLogDetails
            {
                Method = context.Request.Method,
                Path = context.Request.Path,
                //Headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                Body = requestBody,
                FileNames = fileNames,
                //FormData = formData
            };
        }

        private async Task<ResponseLogDetails> CaptureResponseDetails(HttpContext context, long elapsedMilliseconds, ILoggerConfig loggerConfig)
        {
            if (!loggerConfig.LogResponse)
            {
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                return new ResponseLogDetails();
            }

            context.Response.Body.Seek(0, SeekOrigin.Begin);

            // Capture response body
            string responseBody = string.Empty;
            if (!IsBodyTypeExcluded(context.Response.Headers))
            {
                responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
            }

            context.Response.Body.Seek(0, SeekOrigin.Begin);

            return new ResponseLogDetails
            {
                StatusCode = context.Response.StatusCode,
                ElapsedMilliseconds = elapsedMilliseconds,
                Headers = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                Body = responseBody
            };
        }

        private bool IsBodyTypeExcluded(IHeaderDictionary headers)
        {
            return headers.TryGetValue("Content-Type", out var contentType) &&
                   contentType.ToString().Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
        }

        private void LogRequestResponseCycle(RequestLogDetails requestDetails, ResponseLogDetails responseDetails)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping  // تجنب تحويل الأحرف الخاصة إلى Unicode

            };
            string requestJson = JsonSerializer.Serialize(requestDetails, jsonOptions);
            string responseJson = JsonSerializer.Serialize(responseDetails, jsonOptions);

            Log.ForContext("Source", "Middleware")
               .ForContext("EventType", "Request")
               .ForContext("RequestDetails", requestJson, true)
               .ForContext("ResponseDetails", responseJson, true)
               .Information("Request-Response Cycle");
        }
    }

}
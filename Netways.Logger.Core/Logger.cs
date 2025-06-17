namespace Netways.Logger.Core
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Xrm.Sdk;
    using Netways.Logger.Model.Common;
    using Netways.Logger.Model.Configurations;
    using Netways.Logger.Model.StructuredLogs;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Serilog;
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.ServiceModel;
    using System.Text;

    public class Logger(Func<IHttpContextAccessor> httpContextAccessorFactory, ILoggerConfig config) : ILogger
    {
        private bool disposed = false;

        public void LogError(Exception ex, object instance, object?[] paramValues, [CallerMemberName] string? methodName = null)
        {
            var logData = CreateExceptionLogData(instance, paramValues, ex, methodName);

            Log.ForContext("Type", logData.LogType)
               .ForContext("IsCrmValidation", logData.IsCrmValidation)
               .Error("Exception occurred in {Source}.{FunctionName}", logData.Source, logData.FunctionName, logData);
        }

        public T LogErrorAndReturn<T>(Exception ex, object instance, object?[] paramValues, [CallerMemberName] string? methodName = null)
        {
            var logData = CreateExceptionLogData(instance, paramValues, ex, methodName);

            Log.ForContext("Type", logData.LogType)
               .ForContext("IsCrmValidation", logData.IsCrmValidation)
               .Error("Exception occurred in {Source}.{FunctionName}", logData.Source, logData.FunctionName, logData);

            return default!;
        }

        public DefaultResponse<T> LogErrorAndReturnDefaultResponse<T>(Exception ex, object instance, object?[] paramValues, [CallerMemberName] string? methodName = null)
        {
            var response = config.GenericError
                    ? new DefaultResponse<T>("An error occurred while processing your request. Please contact the administrator for more information.", (int)ErrorCodes.SystemError)
                    : new DefaultResponse<T>(ex.Message, (int)ErrorCodes.SystemError);

            var logData = CreateExceptionLogData(instance, paramValues, ex, methodName);

            Log.ForContext("Type", logData.LogType)
               .ForContext("IsCrmValidation", logData.IsCrmValidation)
               .Error("Exception occurred in {Source}.{FunctionName}", logData.Source, logData.FunctionName, logData);

            return response;
        }

        public void LogCustomMessage(string message, object instance, [CallerMemberName] string? methodName = null)
        {
            var httpContext = httpContextAccessorFactory()?.HttpContext;
            string className = instance?.GetType().Name ?? "UnknownClass";

            var logData = new CustomMessageLogData
            {
                Message = message,
                Source = className,
                FunctionName = methodName ?? "UnknownMethod",
                CorrelationId = httpContext?.Items["X-Correlation-Id"]?.ToString(),
                Route = httpContext?.Request.Path
            };

            Log.ForContext("Type", logData.LogType)
               .Information("Custom message from {Source}.{FunctionName}: {Message}", logData.Source, logData.FunctionName, logData.Message, logData);
        }

        public DefaultResponse<bool> LogByApi(Dictionary<string, string?> payload, bool isException)
        {
            var logData = new ApiLogData(payload, isException);

            if (isException)
            {
                Log.ForContext("Type", logData.LogType)
                   .Error("API Exception logged with payload", logData);
            }
            else
            {
                Log.ForContext("Type", logData.LogType)
                   .Information("API Request logged with payload", logData);
            }

            return new DefaultResponse<bool>
            {
                IsSuccess = true,
                Result = true
            };
        }

        private ExceptionLogData CreateExceptionLogData(object instance, object?[] values, Exception ex, string? methodName)
        {
            string className = instance?.GetType().Name ?? "UnknownClass";
            var type = instance?.GetType();
            Type[] parameterTypes = values.Select(v => v?.GetType() ?? typeof(object)).ToArray();

            MethodInfo? method = null;

            if (!string.IsNullOrEmpty(methodName))
            {
                try
                {
                    method = type?.GetMethod(methodName, parameterTypes);
                }
                catch (AmbiguousMatchException)
                {
                    // Handle the AmbiguousMatchException by logging or trying alternative resolution
                    method = type?.GetMethods()
                                  .FirstOrDefault(m => m.Name == methodName &&
                                                       m.GetParameters().Length == parameterTypes.Length);
                }
            }

            ParameterInfo[] parameters = method?.GetParameters() ?? Array.Empty<ParameterInfo>();
            var functionParameters = new JObject();

            int parameterCount = parameters.Length;
            int valueCount = values.Length;
            int count = Math.Min(parameterCount, valueCount);

            for (int i = 0; i < count; i++)
            {
                functionParameters[parameters[i].Name ?? $"Param{i}"] = values[i] != null ? JToken.FromObject(values[i]!) : JValue.CreateNull();
            }

            var httpContext = httpContextAccessorFactory()?.HttpContext;

            return new ExceptionLogData
            {
                Message = ex.Message,
                Source = className,
                FunctionName = methodName ?? "UnknownMethod",
                FunctionParameters = functionParameters,
                Trace = ex.StackTrace ?? string.Empty,
                InnerMessage = ex.InnerException?.Message ?? string.Empty,
                InnerStackTrace = ex.InnerException?.StackTrace ?? string.Empty,
                CorrelationId = httpContext?.Items["X-Correlation-Id"]?.ToString(),
                Route = httpContext?.Request.Path,
                IsCrmValidation = IsCrmValidation(ex)
            };
        }

        private static bool IsCrmValidation(Exception exception)
        {
            if (exception is FaultException<OrganizationServiceFault>)
            {
                if (CommonExtensions.IsJson(exception.Message))
                {
                    try
                    {
                        var obj = JsonConvert.DeserializeObject<DefaultResponse<object>>(exception.Message);
                        if (obj != null && !obj.IsSystemError)
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            return false;
        }


        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {

            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

    }
}

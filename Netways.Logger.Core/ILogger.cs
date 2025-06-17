namespace Netways.Logger.Core
{
    using Netways.Logger.Model.Common;
    using System.Runtime.CompilerServices;

    public interface ILogger : IDisposable
    {
        DefaultResponse<T> LogErrorAndReturnDefaultResponse<T>(Exception ex, object instance, object?[] paramValues, [CallerMemberName] string? methodName = null);

        T LogErrorAndReturn<T>(Exception ex, object instance, object?[] paramValues, [CallerMemberName] string? methodName = null);

        void LogError(Exception ex, object instance, object?[] paramValues, [CallerMemberName] string? methodName = null);

        void LogCustomMessage(string message, object instance, [CallerMemberName] string? methodName = null);

        DefaultResponse<bool> LogByApi(Dictionary<string, string?> payload, bool isException);

    }
}

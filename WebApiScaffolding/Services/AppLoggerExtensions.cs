using Microsoft.Extensions.Logging;

namespace WebApiScaffolding.Services;

public static partial class AppLoggerExtensions
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "{Parameter}")]
    public static partial void LogInfo(this ILogger logger, string parameter);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "{Parameter}")]
    public static partial void LogWarn(this ILogger logger, string parameter);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "{Parameter}")]
    public static partial void LogError(this ILogger logger, Exception exception, string parameter);
}
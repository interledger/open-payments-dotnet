namespace OpenPaymentsSDK.Logging;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provides a static logger factory for the SDK.
/// </summary>
internal static class LoggerProvider
{
    private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddConsole();
    });
    
    /// <summary>
    /// Creates a logger for the specified type.
    /// </summary>
    public static ILogger<T> CreateLogger<T>()
    {
        return _loggerFactory.CreateLogger<T>();
    }
}
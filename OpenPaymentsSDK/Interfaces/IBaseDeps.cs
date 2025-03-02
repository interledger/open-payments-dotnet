using Microsoft.Extensions.Logging;

namespace OpenPaymentsSDK.Interfaces;

/// <summary>
/// Represents the base dependencies required by the SDK.
/// </summary>
public interface IBaseDeps
{
    HttpClient HttpClient { get; }
    ILogger Logger { get; }
    bool UseHttps { get; }
}
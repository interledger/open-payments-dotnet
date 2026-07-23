namespace OpenPayments.Sdk;

/// <summary>
/// The single exception type thrown by every OpenPayments.Sdk client (authenticated,
/// unauthenticated, auth-server, resource-server, wallet-address) when a request fails
/// or returns an unexpected response. Replaces the per-generated-namespace
/// <c>ApiException</c>/<c>ApiException&lt;T&gt;</c> types and the ad-hoc
/// <see cref="InvalidOperationException"/> previously thrown by <c>UnauthenticatedClient</c>.
/// </summary>
public class OpenPaymentsApiException : Exception
{
    /// <summary>
    /// The HTTP status code returned by the server, or the status code that triggered
    /// this exception (e.g. a successful status whose body could not be parsed).
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// The machine-readable error code from the server's error body, if the server
    /// returned one (e.g. <c>"invalid_request"</c>). <c>null</c> when the response body
    /// could not be parsed as an error, or carried no code.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// The raw response body, if any was read before this exception was constructed.
    /// </summary>
    public string? RawResponse { get; }

    /// <summary>
    /// The response headers, if any were captured before this exception was constructed.
    /// </summary>
    public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }

    public OpenPaymentsApiException(
        string message,
        int statusCode,
        string? errorCode,
        string? rawResponse,
        IReadOnlyDictionary<string, IEnumerable<string>> headers
    )
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        RawResponse = rawResponse;
        Headers = headers;
    }

    public override string ToString() =>
        $"HTTP Response ({StatusCode}):\n\n{RawResponse}\n\n{base.ToString()}";
}

/// <summary>
/// Constructs <see cref="OpenPaymentsApiException"/> instances. Used by every generated
/// client's response-handling code instead of duplicating exception construction per
/// status-code branch.
/// </summary>
internal static class OpenPaymentsExceptionFactory
{
    public static OpenPaymentsApiException Create(
        string message,
        int statusCode,
        string? errorCode,
        string? rawResponse,
        IReadOnlyDictionary<string, IEnumerable<string>> headers
    ) => new(message, statusCode, errorCode, rawResponse, headers);
}

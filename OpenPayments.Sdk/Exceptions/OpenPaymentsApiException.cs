namespace OpenPayments.Sdk.Exceptions;

/// <summary>
/// The single exception thrown by every Open Payments client method when the server reports a
/// failure.
/// </summary>
/// <remarks>
/// A request "fails" when the server returns a non-2xx status, or when it returns a 2xx whose body
/// is empty, null, or cannot be deserialized into the expected model. In both cases
/// <see cref="StatusCode"/> is the status the server actually returned and <see cref="ResponseBody"/>
/// holds the body verbatim. Transport-level failures that occur before a response arrives (DNS,
/// connection, TLS, timeout) are not wrapped by this type — they still surface as
/// <see cref="HttpRequestException"/> or <see cref="TaskCanceledException"/> from
/// <see cref="HttpClient"/>.
/// </remarks>
public sealed class OpenPaymentsApiException : Exception
{
    private static readonly IReadOnlyDictionary<string, IEnumerable<string>> NoHeaders =
        new Dictionary<string, IEnumerable<string>>();

    /// <param name="statusCode">The HTTP status code the server returned.</param>
    /// <param name="errorCode">
    /// The machine-readable code from the response body (for example <c>invalid_request</c>), or
    /// <see langword="null"/> when the server did not return a recognisable Open Payments error body.
    /// </param>
    /// <param name="description">The human-readable description from the response body, if any.</param>
    /// <param name="responseBody">The raw response body, verbatim and untruncated.</param>
    /// <param name="headers">The response headers. An empty dictionary is used when omitted.</param>
    /// <param name="retryAfter">The parsed <c>Retry-After</c> hint, if the server sent one.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public OpenPaymentsApiException(
        int statusCode,
        string? errorCode,
        string? description,
        string? responseBody,
        IReadOnlyDictionary<string, IEnumerable<string>>? headers = null,
        TimeSpan? retryAfter = null,
        Exception? innerException = null
    )
        : base(BuildMessage(statusCode, errorCode, description), innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Description = description;
        ResponseBody = responseBody;
        Headers = headers ?? NoHeaders;
        RetryAfter = retryAfter;
    }

    /// <summary>The HTTP status code the server returned.</summary>
    public int StatusCode { get; }

    /// <summary>
    /// The machine-readable error code from the response body, or <see langword="null"/> when the
    /// server returned no recognisable Open Payments error body. Inspect <see cref="ResponseBody"/>
    /// in that case.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>The human-readable description from the response body, if the server sent one.</summary>
    public string? Description { get; }

    /// <summary>The raw response body, verbatim and untruncated.</summary>
    public string? ResponseBody { get; }

    /// <summary>The response headers. Never <see langword="null"/>.</summary>
    public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }

    /// <summary>
    /// How long to wait before retrying, parsed from the <c>Retry-After</c> header. Commonly set on
    /// 429 and 503 responses; <see langword="null"/> when the server sent no usable hint. The SDK
    /// does not retry on the caller's behalf.
    /// </summary>
    public TimeSpan? RetryAfter { get; }

    private static string BuildMessage(int statusCode, string? errorCode, string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return $"The Open Payments request failed with HTTP {statusCode}.";

        return string.IsNullOrWhiteSpace(errorCode)
            ? $"{description} (HTTP {statusCode})"
            : $"{description} (HTTP {statusCode}, code: {errorCode})";
    }
}

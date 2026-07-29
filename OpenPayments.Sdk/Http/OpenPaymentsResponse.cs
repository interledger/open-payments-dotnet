using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenPayments.Sdk.Exceptions;
using OpenPayments.Sdk.Generated;

namespace OpenPayments.Sdk.Http;

/// <summary>
/// The single response-processing path for every Open Payments client method. Maps any failed
/// response onto <see cref="OpenPaymentsApiException"/>.
/// </summary>
internal static class OpenPaymentsResponse
{
    /// <summary>
    /// Returns without doing anything when the response is 2xx. Otherwise reads the body and throws
    /// <see cref="OpenPaymentsApiException"/>.
    /// </summary>
    public static async Task ThrowIfErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
        var (code, description) = TryReadError(body);

        throw Build(response, code, description, body);
    }

    /// <summary>
    /// Deserializes a success response into <typeparamref name="T"/>. Throws
    /// <see cref="OpenPaymentsApiException"/> — carrying the 2xx status the server actually returned
    /// and the raw body — when the body is empty, deserializes to null, or is malformed.
    /// </summary>
    /// <param name="response">The HTTP response to read.</param>
    /// <param name="settings">
    /// The calling client's serializer settings, or <see langword="null"/> to use the
    /// <see cref="JsonConvert"/> defaults.
    /// </param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    public static async Task<T> ReadRequiredAsync<T>(
        HttpResponseMessage response,
        JsonSerializerSettings? settings,
        CancellationToken cancellationToken
    )
    {
        var body = await ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);

        T? model;
        try
        {
            model = JsonConvert.DeserializeObject<T>(body, settings);
        }
        catch (JsonException exception)
        {
            throw Build(
                response,
                null,
                $"Could not deserialize the response body as {typeof(T).FullName}.",
                body,
                exception
            );
        }

        return model
            ?? throw Build(
                response,
                null,
                "The server returned an empty or null response body.",
                body
            );
    }

    private static async Task<string> ReadBodyAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        if (response.Content is null)
            return string.Empty;

        return await response
            .Content.ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Best-effort probe for the Open Payments error shape. Never throws: a body that is not JSON,
    /// or is JSON of an unexpected shape, yields <c>(null, null)</c> and the caller keeps the raw
    /// body instead. Throwing here would replace a useful HTTP error with a parse error.
    /// </summary>
    private static (string? Code, string? Description) TryReadError(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return (null, null);

        try
        {
            var error = JObject.Parse(body)["error"];

            return error switch
            {
                // GNAP permits a bare string in place of the error object.
                JValue { Type: JTokenType.String } value => ((string?)value, null),
                JObject errorObject => (
                    TryGetStringValue(errorObject["code"]),
                    TryGetStringValue(errorObject["description"])
                ),
                _ => (null, null),
            };
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    /// <summary>
    /// Safely extracts a string value from a JToken, returning null if the token is not a string
    /// or if the cast throws (e.g., when the token is an object or array).
    /// </summary>
    private static string? TryGetStringValue(JToken? token)
    {
        if (token is null)
            return null;

        try
        {
            // Only attempt conversion if the token is a scalar type.
            if (token.Type == JTokenType.String)
                return (string?)token;

            // For other scalar types (Null, Boolean, etc.), return null.
            return null;
        }
        catch (ArgumentException)
        {
            // If conversion fails for any reason, return null.
            return null;
        }
    }

    /// <summary>
    /// Reads <c>Retry-After</c> in either permitted form. A date already in the past is clamped to
    /// zero so a caller never derives a negative delay from a stale response.
    /// </summary>
    private static TimeSpan? ParseRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;

        if (retryAfter is null)
            return null;

        if (retryAfter.Delta is { } delta)
            return delta < TimeSpan.Zero ? TimeSpan.Zero : delta;

        if (retryAfter.Date is { } date)
        {
            var remaining = date - DateTimeOffset.UtcNow;
            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }

        return null;
    }

    private static OpenPaymentsApiException Build(
        HttpResponseMessage response,
        string? code,
        string? description,
        string? body,
        Exception? innerException = null
    ) =>
        new(
            (int)response.StatusCode,
            code,
            description,
            body,
            Helpers.ExtractHeaders(response),
            ParseRetryAfter(response),
            innerException
        );
}

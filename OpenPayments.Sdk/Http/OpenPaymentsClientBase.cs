using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace OpenPayments.Sdk.Http;

/// <summary>
/// Shared HTTP plumbing for the hand-owned Open Payments transport clients. Owns the
/// <see cref="HttpClient"/> and the per-client serializer settings, builds every request the
/// same way, and funnels every response through <see cref="OpenPaymentsResponse"/> so all
/// failures surface as <see cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException"/>.
/// </summary>
public abstract class OpenPaymentsClientBase
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerSettings _serializerSettings;

    /// <param name="httpClient">The client to send requests with; not disposed by this class.</param>
    /// <param name="serializerSettings">
    /// Settings used for every serialization in this client. Created once and never mutated
    /// afterwards, so a client instance is safe to share across threads.
    /// </param>
    protected OpenPaymentsClientBase(HttpClient httpClient, JsonSerializerSettings serializerSettings)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(serializerSettings);

        _httpClient = httpClient;
        _serializerSettings = serializerSettings;
    }

    /// <summary>
    /// Sends a request and deserializes the success body into <typeparamref name="TResponse"/>.
    /// </summary>
    /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
    protected async Task<TResponse> SendAsync<TResponse>(
        HttpMethod method,
        Uri url,
        object? body,
        string? accessToken,
        CancellationToken cancellationToken
    )
    {
        var response = await SendCoreAsync(method, url, body, accessToken, acceptJson: true, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await OpenPaymentsResponse
                .ThrowIfErrorAsync(response, cancellationToken)
                .ConfigureAwait(false);

            return await OpenPaymentsResponse
                .ReadRequiredAsync<TResponse>(response, _serializerSettings, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            response.Dispose();
        }
    }

    /// <summary>
    /// Sends a request whose success response carries no body (cancel, revoke).
    /// </summary>
    /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
    protected async Task SendAsync(
        HttpMethod method,
        Uri url,
        object? body,
        string? accessToken,
        CancellationToken cancellationToken
    )
    {
        var response = await SendCoreAsync(method, url, body, accessToken, acceptJson: false, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await OpenPaymentsResponse
                .ThrowIfErrorAsync(response, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            response.Dispose();
        }
    }

    private async Task<HttpResponseMessage> SendCoreAsync(
        HttpMethod method,
        Uri url,
        object? body,
        string? accessToken,
        bool acceptJson,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(url);

        using var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            var json = JsonConvert.SerializeObject(body, _serializerSettings);
            var content = new StringContent(json);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            request.Content = content;
        }
        else if (method == HttpMethod.Post)
        {
            // Bodyless POSTs (token rotation, payment completion) send an empty JSON
            // body with charset, matching the servers' expectations to date.
            request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        }

        if (acceptJson)
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));

        if (accessToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("GNAP", accessToken);

        return await _httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
    }
}

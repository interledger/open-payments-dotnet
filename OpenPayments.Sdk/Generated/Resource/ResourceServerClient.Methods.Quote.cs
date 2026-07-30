using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using OpenPayments.Sdk.Http;

namespace OpenPayments.Sdk.Generated.Resource;

public partial class ResourceServerClient
{
    /// <summary>
    /// Create a Quote
    /// </summary>
    /// <remarks>
    /// A **quote** is a sub-resource of a wallet address. It represents a quote for a payment from the wallet address.
    /// </remarks>
    /// <param name="resourceServerUrl">The resource server URL to which the quotes segment will be appended.</param>
    /// <param name="body">A subset of the quotes schema is accepted as input to create a new quote.
    /// <br/>
    /// <br/>The quote must be created with a (`debitAmount` xor `receiveAmount`) unless the `receiver` is an Incoming Payment which has an `incomingAmount`.</param>
    /// <param name="accessToken">Access Token.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>Quote Created</returns>
    /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
    public async Task<QuoteResponse> PostQuoteAsync(
        Uri resourceServerUrl,
        QuoteBody body,
        string accessToken,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var client = _httpClient;
        using var request = new HttpRequestMessage();
        var json = JsonConvert.SerializeObject(body, JsonSerializerSettings);
        var content = new StringContent(json);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
        request.Content = content;
        request.Method = new HttpMethod("POST");
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("GNAP", $"{accessToken}");

        var urlBuilder = new StringBuilder(AppendPath(resourceServerUrl, "quotes"));

        PrepareRequest(client, request, urlBuilder);

        var url = urlBuilder.ToString();
        request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);

        PrepareRequest(client, request, url);

        var response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await OpenPaymentsResponse
                .ThrowIfErrorAsync(response, cancellationToken)
                .ConfigureAwait(false);

            return await OpenPaymentsResponse
                .ReadRequiredAsync<QuoteResponse>(
                    response,
                    JsonSerializerSettings,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        finally
        {
            response.Dispose();
        }
    }

    /// <summary>
    /// Get a Quote
    /// </summary>
    /// <remarks>
    /// A client can fetch the latest state of a quote.
    /// </remarks>
    /// <param name="quoteUrl">The absolute URL of the quote to retrieve.</param>
    /// <param name="accessToken">Access Token.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>Quote Found</returns>
    /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
    public virtual async Task<QuoteResponse> GetQuoteAsync(
        Uri quoteUrl,
        string accessToken,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var client = _httpClient;
        using var request = new HttpRequestMessage();
        request.Method = new HttpMethod("GET");
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("GNAP", $"{accessToken}");

        var urlBuilder = new StringBuilder(quoteUrl.ToString());

        PrepareRequest(client, request, urlBuilder);

        var url = urlBuilder.ToString();
        request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);

        PrepareRequest(client, request, url);

        var response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await OpenPaymentsResponse
                .ThrowIfErrorAsync(response, cancellationToken)
                .ConfigureAwait(false);

            return await OpenPaymentsResponse
                .ReadRequiredAsync<QuoteResponse>(
                    response,
                    JsonSerializerSettings,
                    cancellationToken
                )
                .ConfigureAwait(false);
        }
        finally
        {
            response.Dispose();
        }
    }
}

using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace OpenPayments.Sdk.Generated.Resource;

public partial class ResourceServerClient
{
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <summary>
    /// Create a Quote
    /// </summary>
    /// <remarks>
    /// A **quote** is a sub-resource of a wallet address. It represents a quote for a payment from the wallet address.
    /// </remarks>
    /// <param name="body">A subset of the quotes schema is accepted as input to create a new quote.
    /// <br/>
    /// <br/>The quote must be created with a (`debitAmount` xor `receiveAmount`) unless the `receiver` is an Incoming Payment which has an `incomingAmount`.</param>
    /// <param name="accessToken">Access Token.</param>
    /// <returns>Quote Created</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    public async Task<QuoteResponse> PostQuoteAsync(
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

        var urlBuilder = new StringBuilder();
        if (!string.IsNullOrEmpty(_baseUrl))
            urlBuilder.Append(_baseUrl);
        urlBuilder.Append("quotes");

        PrepareRequest(client, request, urlBuilder);

        var url = urlBuilder.ToString();
        request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);

        PrepareRequest(client, request, url);

        var response = await client
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var headers = Helpers.ExtractHeaders(response);

            ProcessResponse(client, response);

            var status = (int)response.StatusCode;
            switch (status)
            {
                case 201:
                {
                    var objectResponse = await ReadObjectResponseAsync<QuoteResponse>(
                            response,
                            headers,
                            cancellationToken
                        )
                        .ConfigureAwait(false);
                    if (objectResponse.Object == null)
                    {
                        throw new ApiException(
                            "Response was null which was not expected.",
                            status,
                            objectResponse.Text,
                            headers,
                            null
                        );
                    }

                    return objectResponse.Object;
                }
                case 400:
                case 401:
                case 403:
                {
                    var objectResponse = await ReadObjectResponseAsync<ErrorResponse>(
                            response,
                            headers,
                            cancellationToken
                        )
                        .ConfigureAwait(false);
                    if (objectResponse.Object == null)
                    {
                        throw new ApiException(
                            "Response was null which was not expected.",
                            status,
                            objectResponse.Text,
                            headers,
                            null
                        );
                    }

                    throw new ApiException<ErrorResponse>(
                        Helpers.StatusAsText(status),
                        status,
                        objectResponse.Text,
                        headers,
                        objectResponse.Object,
                        null
                    );
                }
                default:
                {
                    var responseData = await ReadAsStringAsync(response.Content, cancellationToken)
                        .ConfigureAwait(false);
                    throw new ApiException(
                        "The HTTP status code of the response was not expected (" + status + ").",
                        status,
                        responseData,
                        headers,
                        null
                    );
                }
            }
        }
        finally
        {
            response.Dispose();
        }
    }
}

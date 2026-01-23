using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace OpenPayments.Sdk.Generated.Resource;

public partial class ResourceServerClient
{
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <summary>
    /// Create an Incoming Payment
    /// </summary>
    /// <remarks>
    /// A client MUST create an **incoming payment** resource before it is possible to send any payments to the wallet address.
    /// <br/>
    /// <br/>When a client creates an **incoming payment** the receiving Account Servicing Entity generates unique payment details that can be used to address payments to the account and returns these details to the client as properties of the new **incoming payment**. Any payments received using those details are then associated with the **incoming payment**.
    /// <br/>
    /// <br/>All of the input parameters are _optional_.
    /// <br/>
    /// <br/>For example, the client could use the `metadata` property to store an external reference on the **incoming payment** and this can be shared with the account holder to assist with reconciliation.
    /// <br/>
    /// <br/>If `incomingAmount` is specified and the total received using the payment details equals or exceeds the specified `incomingAmount`, then the receiving Account Servicing Entity MUST reject any further payments and set `completed` to `true`.
    /// <br/>
    /// <br/>If an `expiresAt` value is defined, and the current date and time on the receiving Account Servicing Entity's systems exceeds that value, the receiving Account Servicing Entity MUST reject any further payments.
    /// </remarks>
    /// <param name="body">A subset of the incoming payments schema is accepted as input to create a new incoming payment.
    /// <br/>
    /// <br/>The `incomingAmount` must use the same `assetCode` and `assetScale` as the wallet address.</param>
    /// <param name="accessToken">Access Token.</param>
    /// <returns>Incoming Payment Created</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    public async Task<IncomingPaymentResponse> PostIncomingPaymentAsync(Body body,
        string accessToken,
        CancellationToken cancellationToken)
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
        if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder.Append(_baseUrl);
        // Operation Path: "incoming-payments"
        urlBuilder.Append("incoming-payments");

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
                    var objectResponse =
                        await ReadObjectResponseAsync<IncomingPaymentResponse>(response, headers,
                            cancellationToken).ConfigureAwait(false);
                    if (objectResponse.Object == null)
                    {
                        throw new ApiException("Response was null which was not expected.", status,
                            objectResponse.Text, headers, null);
                    }

                    return objectResponse.Object;
                }
                case 401:
                case 403:
                {
                    var objectResponse =
                        await ReadObjectResponseAsync<ErrorResponse>(response, headers, cancellationToken)
                            .ConfigureAwait(false);
                    if (objectResponse.Object == null)
                    {
                        throw new ApiException("Response was null which was not expected.", status,
                            objectResponse.Text, headers, null);
                    }

                    throw new ApiException<ErrorResponse>(Helpers.StatusAsText(status), status, objectResponse.Text,
                        headers,
                        objectResponse.Object, null);
                }
                default:
                {
                    var responseData = await ReadAsStringAsync(response.Content, cancellationToken)
                        .ConfigureAwait(false);
                    throw new ApiException(
                        "The HTTP status code of the response was not expected (" + status + ").", status,
                        responseData, headers, null);
                }
            }
        }
        finally
        {
            response.Dispose();
        }
    }
}
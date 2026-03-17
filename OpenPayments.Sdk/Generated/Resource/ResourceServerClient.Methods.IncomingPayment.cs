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
    public async Task<IncomingPaymentResponse> PostIncomingPaymentAsync(
        Body body,
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
                    var objectResponse = await ReadObjectResponseAsync<IncomingPaymentResponse>(
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
                        objectResponse.Object.Error.Description,
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

    /// <summary>
    /// Get an Incoming Payment
    /// </summary>
    /// <remarks>
    /// A client can fetch the latest state of an incoming payment to determine the amount received into the wallet address.
    /// </remarks>
    /// <param name="accessToken">Access Token.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>Incoming Payment Found</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    public virtual async Task<IncomingPaymentResponse> GetIncomingPaymentAsync(
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

        var urlBuilder = new StringBuilder(_baseUrl);

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
                case 200:
                {
                    var objectResponse = await ReadObjectResponseAsync<IncomingPaymentResponse>(
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
                case 401:
                case 403:
                case 404:
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
                        objectResponse.Object.Error.Description,
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

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <summary>
    /// List Incoming Payments
    /// </summary>
    /// <remarks>
    /// List all incoming payments on the wallet address
    /// </remarks>
    /// <param name="accessToken">Access Token</param>
    /// <param name="walletAddress">URL of a wallet address hosted by a Rafiki instance.</param>
    /// <param name="cursor">The cursor key to list from.</param>
    /// <param name="first">The number of items to return after the cursor.</param>
    /// <param name="last">The number of items to return before the cursor.</param>
    /// <returns>OK</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    public virtual async Task<ListIncomingPaymentsResponse> ListIncomingPaymentsAsync(
        string accessToken,
        string walletAddress,
        string? cursor,
        int? first,
        int? last,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(walletAddress);

        var client = _httpClient;
        using var request = new HttpRequestMessage();
        request.Method = new HttpMethod("GET");
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("GNAP", $"{accessToken}");

        var urlBuilder = new StringBuilder();
        if (!string.IsNullOrEmpty(_baseUrl))
            urlBuilder.Append(_baseUrl);
        urlBuilder.Append("incoming-payments");
        urlBuilder.Append('?');
        urlBuilder
            .Append(Uri.EscapeDataString("wallet-address"))
            .Append('=')
            .Append(
                Uri.EscapeDataString(
                    ConvertToString(
                        walletAddress,
                        System.Globalization.CultureInfo.InvariantCulture
                    )
                )
            );
        if (cursor != null)
        {
            urlBuilder
                .Append('&')
                .Append(Uri.EscapeDataString("cursor"))
                .Append('=')
                .Append(
                    Uri.EscapeDataString(
                        ConvertToString(cursor, System.Globalization.CultureInfo.InvariantCulture)
                    )
                );
        }

        if (first != null)
        {
            urlBuilder
                .Append('&')
                .Append(Uri.EscapeDataString("first"))
                .Append('=')
                .Append(
                    Uri.EscapeDataString(
                        ConvertToString(first, System.Globalization.CultureInfo.InvariantCulture)
                    )
                );
        }

        if (last != null)
        {
            urlBuilder
                .Append('&')
                .Append(Uri.EscapeDataString("last"))
                .Append('=')
                .Append(
                    Uri.EscapeDataString(
                        ConvertToString(last, System.Globalization.CultureInfo.InvariantCulture)
                    )
                );
        }

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
                case 200:
                {
                    var objectResponse =
                        await ReadObjectResponseAsync<ListIncomingPaymentsResponse>(
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
                        objectResponse.Object.Error.Description,
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

    /// <summary>
    /// Complete an Incoming Payment
    /// </summary>
    /// <remarks>
    /// A client with the appropriate permissions MAY mark a non-expired **incoming payment** as `completed` indicating that the client is not going to make any further payments toward this **incoming payment**, even though the full `incomingAmount` may not have been received.
    /// <br/>
    /// <br/>This indicates to the receiving Account Servicing Entity that it can begin any post processing of the payment such as generating account statements or notifying the account holder of the completed payment.
    /// </remarks>
    /// <param name="accessToken"></param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>OK</returns>
    /// <exception cref="ApiException">A server side error occurred.</exception>
    public virtual async Task<IncomingPaymentResponse> CompleteIncomingPaymentAsync(
        string accessToken,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var client = _httpClient;
        using var request = new HttpRequestMessage();
        request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        request.Method = new HttpMethod("POST");
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("GNAP", $"{accessToken}");

        var urlBuilder = new StringBuilder(_baseUrl);
        urlBuilder.Append("/complete");

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
                case 200:
                {
                    var objectResponse = await ReadObjectResponseAsync<IncomingPaymentResponse>(
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
                case 401:
                case 403:
                case 404:
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
                        objectResponse.Object.Error.Description,
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

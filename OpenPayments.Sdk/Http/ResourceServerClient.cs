using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using OpenPayments.Sdk.Generated.Resource;

namespace OpenPayments.Sdk.Http;

public class ResourceServerClient : OpenPaymentsClientBase
{
    public ResourceServerClient(HttpClient httpClient)
        : base(httpClient, new JsonSerializerSettings { ContractResolver = new ResourceContractResolver() })
    {
    }

    /// <param name="resourceServerUrl">The resource server URL to which the incoming-payments segment will be appended.</param>
    /// <param name="body">A subset of the incoming payments schema is accepted as input to create a new incoming payment.
    /// <br/>
    /// <br/>The `incomingAmount` must use the same `assetCode` and `assetScale` as the wallet address.</param>
    /// <param name="accessToken">Access Token.</param>
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
    /// <returns>Incoming Payment Created</returns>
    /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
    public async Task<IncomingPaymentResponse> PostIncomingPaymentAsync(
        Uri resourceServerUrl,
        Body body,
        string accessToken,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var url = new Uri(AppendPath(resourceServerUrl, "incoming-payments"), UriKind.RelativeOrAbsolute);

        return await SendAsync<IncomingPaymentResponse>(
                HttpMethod.Post, url, body, accessToken, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get an Incoming Payment
    /// </summary>
    /// <remarks>
    /// A client can fetch the latest state of an incoming payment to determine the amount received into the wallet address.
    /// </remarks>
    /// <param name="incomingPaymentUrl">The absolute URL of the incoming payment to retrieve.</param>
    /// <param name="accessToken">Access Token.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>Incoming Payment Found</returns>
    /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
    public virtual async Task<IncomingPaymentResponse> GetIncomingPaymentAsync(
        Uri incomingPaymentUrl,
        string accessToken,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await SendAsync<IncomingPaymentResponse>(
                HttpMethod.Get, incomingPaymentUrl, body: null, accessToken, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <summary>
    /// List Incoming Payments
    /// </summary>
    /// <remarks>
    /// List all incoming payments on the wallet address
    /// </remarks>
    /// <param name="resourceServerUrl">The resource server URL to which the incoming-payments segment will be appended.</param>
    /// <param name="accessToken">Access Token</param>
    /// <param name="walletAddress">URL of a wallet address hosted by a Rafiki instance.</param>
    /// <param name="cursor">The cursor key to list from.</param>
    /// <param name="first">The number of items to return after the cursor.</param>
    /// <param name="last">The number of items to return before the cursor.</param>
    /// <returns>OK</returns>
    /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
    public virtual async Task<ListIncomingPaymentsResponse> ListIncomingPaymentsAsync(
        Uri resourceServerUrl,
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

        var url = BuildListUrl(resourceServerUrl, "incoming-payments", walletAddress, cursor, first, last);

        return await SendAsync<ListIncomingPaymentsResponse>(
                HttpMethod.Get, url, body: null, accessToken, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Complete an Incoming Payment
    /// </summary>
    /// <remarks>
    /// A client with the appropriate permissions MAY mark a non-expired **incoming payment** as `completed` indicating that the client is not going to make any further payments toward this **incoming payment**, even though the full `incomingAmount` may not have been received.
    /// <br/>
    /// <br/>This indicates to the receiving Account Servicing Entity that it can begin any post processing of the payment such as generating account statements or notifying the account holder of the completed payment.
    /// </remarks>
    /// <param name="incomingPaymentUrl">The absolute URL of the incoming payment to complete.</param>
    /// <param name="accessToken"></param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>OK</returns>
    /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
    public virtual async Task<IncomingPaymentResponse> CompleteIncomingPaymentAsync(
        Uri incomingPaymentUrl,
        string accessToken,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var url = new Uri(AppendPath(incomingPaymentUrl, "complete"), UriKind.RelativeOrAbsolute);

        return await SendAsync<IncomingPaymentResponse>(
                HttpMethod.Post, url, body: null, accessToken, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Create an Outgoing Payment
    /// </summary>
    /// <remarks>
    /// An **outgoing payment** is a sub-resource of a wallet address. It represents a payment from the wallet address.
    /// <br/>
    /// <br/>Once created, it is already authorized and SHOULD be processed immediately. If payment fails, the Account Servicing Entity must mark the **outgoing payment** as `failed`.
    /// </remarks>
    /// <param name="resourceServerUrl">The resource server URL to which the outgoing-payments segment will be appended.</param>
    /// <param name="body">A subset of the outgoing payments schema is accepted as input to create a new outgoing payment.
    /// <br/>
    /// <br/>The `debitAmount` must use the same `assetCode` and `assetScale` as the wallet address.
    /// <br/>
    /// <br/>Either provide a `quoteId` to create an outgoing payment based on a quote or provide `incomingPayment` and `debitAmount` to create an outgoing payment directly from an incoming payment.</param>
    /// <param name="accessToken">Access Token.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>Outgoing Payment Created</returns>
    /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
    public async Task<OutgoingPaymentWithSpentAmountsResponse> PostOutgoingPaymentAsync(
        Uri resourceServerUrl,
        OutgoingPaymentBody body,
        string accessToken,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(body);

        var url = new Uri(AppendPath(resourceServerUrl, "outgoing-payments"), UriKind.RelativeOrAbsolute);

        return await SendAsync<OutgoingPaymentWithSpentAmountsResponse>(
                HttpMethod.Post, url, body, accessToken, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Get an Outgoing Payment
    /// </summary>
    /// <remarks>
    /// A client can fetch the latest state of an outgoing payment.
    /// </remarks>
    /// <param name="outgoingPaymentUrl">The absolute URL of the outgoing payment to retrieve.</param>
    /// <param name="accessToken">Access Token</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>Outgoing Payment Found</returns>
    /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
    public virtual async Task<OutgoingPaymentResponse> GetOutgoingPaymentAsync(
        Uri outgoingPaymentUrl,
        string accessToken,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        return await SendAsync<OutgoingPaymentResponse>(
                HttpMethod.Get, outgoingPaymentUrl, body: null, accessToken, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// List Outgoing Payments
    /// </summary>
    /// <remarks>
    /// List all outgoing payments on the wallet address
    /// </remarks>
    /// <param name="resourceServerUrl">The resource server URL to which the outgoing-payments segment will be appended.</param>
    /// <param name="accessToken">Access Token</param>
    /// <param name="walletAddress">URL of a wallet address hosted by a Rafiki instance.</param>
    /// <param name="cursor">The cursor key to list from.</param>
    /// <param name="first">The number of items to return after the cursor.</param>
    /// <param name="last">The number of items to return before the cursor.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>OK</returns>
    /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
    public virtual async Task<ListOutgoingPaymentsResponse> ListOutgoingPaymentsAsync(
        Uri resourceServerUrl,
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

        var url = BuildListUrl(resourceServerUrl, "outgoing-payments", walletAddress, cursor, first, last);

        return await SendAsync<ListOutgoingPaymentsResponse>(
                HttpMethod.Get, url, body: null, accessToken, cancellationToken)
            .ConfigureAwait(false);
    }

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

        var url = new Uri(AppendPath(resourceServerUrl, "quotes"), UriKind.RelativeOrAbsolute);

        return await SendAsync<QuoteResponse>(
                HttpMethod.Post, url, body, accessToken, cancellationToken)
            .ConfigureAwait(false);
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

        return await SendAsync<QuoteResponse>(
                HttpMethod.Get, quoteUrl, body: null, accessToken, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Joins <paramref name="segment"/> onto <paramref name="baseUrl"/> with exactly one
    /// separating slash, whether or not the caller's URL carries a trailing one.
    /// </summary>
    private static string AppendPath(Uri baseUrl, string segment) =>
        $"{baseUrl.ToString().TrimEnd('/')}/{segment}";

    private static Uri BuildListUrl(
        Uri resourceServerUrl,
        string segment,
        string walletAddress,
        string? cursor,
        int? first,
        int? last
    )
    {
        var urlBuilder = new StringBuilder(AppendPath(resourceServerUrl, segment));
        urlBuilder.Append("?wallet-address=").Append(Uri.EscapeDataString(walletAddress));

        if (cursor != null)
            urlBuilder.Append("&cursor=").Append(Uri.EscapeDataString(cursor));

        if (first != null)
            urlBuilder.Append("&first=").Append(first.Value.ToString(CultureInfo.InvariantCulture));

        if (last != null)
            urlBuilder.Append("&last=").Append(last.Value.ToString(CultureInfo.InvariantCulture));

        return new Uri(urlBuilder.ToString(), UriKind.RelativeOrAbsolute);
    }
}

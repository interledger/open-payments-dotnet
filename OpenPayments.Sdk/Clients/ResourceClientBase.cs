using OpenPayments.Sdk.Generated.Resource;
using OpenPayments.Sdk.Http;

namespace OpenPayments.Sdk.Clients;

/// <summary>
/// Default <see cref="IResourceClientBase"/> implementation. Wraps a <see cref="ResourceServerClient"/>,
/// passing the target URL from <see cref="RequestArgs.Url"/> on each call since a single resource server
/// client is shared across requests to different resource servers.
/// </summary>
internal sealed class ResourceClientBase : IResourceClientBase
{
    private readonly ResourceServerClient _client;

    /// <summary>
    /// Creates a new <see cref="ResourceClientBase"/>.
    /// </summary>
    /// <param name="http"><see cref="HttpClient"/> whose pipeline signs outgoing requests. Its <see cref="HttpClient.BaseAddress"/> is ignored; absolute request URIs are used instead.</param>
    public ResourceClientBase(HttpClient http)
    {
        _client = new ResourceServerClient(http);
    }

    /// <inheritdoc/>
    public Task<IncomingPaymentResponse> CreateIncomingPaymentAsync(
        AuthRequestArgs requestArgs,
        Body body,
        CancellationToken cancellationToken = default
    )
    {
        return _client.PostIncomingPaymentAsync(
            requestArgs.Url,
            body,
            requestArgs.AccessToken,
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public Task<IncomingPaymentResponse> GetIncomingPaymentAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    )
    {
        return _client.GetIncomingPaymentAsync(
            requestArgs.Url,
            requestArgs.AccessToken,
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public Task<IncomingPaymentResponse> CompleteIncomingPaymentAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    )
    {
        return _client.CompleteIncomingPaymentAsync(
            requestArgs.Url,
            requestArgs.AccessToken,
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public Task<ListIncomingPaymentsResponse> ListIncomingPaymentsAsync(
        AuthRequestArgs requestArgs,
        ListIncomingPaymentQuery query,
        CancellationToken cancellationToken = default
    )
    {
        return _client.ListIncomingPaymentsAsync(
            requestArgs.Url,
            requestArgs.AccessToken,
            query.WalletAddress,
            query.Cursor,
            query.First,
            query.Last,
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public Task<QuoteResponse> CreateQuoteAsync(
        AuthRequestArgs requestArgs,
        QuoteBody body,
        CancellationToken cancellationToken = default
    )
    {
        return _client.PostQuoteAsync(
            requestArgs.Url,
            body,
            requestArgs.AccessToken,
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public Task<QuoteResponse> GetQuoteAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    )
    {
        return _client.GetQuoteAsync(requestArgs.Url, requestArgs.AccessToken, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<OutgoingPaymentWithSpentAmountsResponse> CreateOutgoingPaymentAsync(
        AuthRequestArgs requestArgs,
        OutgoingPaymentBody body,
        CancellationToken cancellationToken = default
    )
    {
        return _client.PostOutgoingPaymentAsync(
            requestArgs.Url,
            body,
            requestArgs.AccessToken,
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public Task<OutgoingPaymentResponse> GetOutgoingPaymentAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    )
    {
        return _client.GetOutgoingPaymentAsync(
            requestArgs.Url,
            requestArgs.AccessToken,
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public Task<ListOutgoingPaymentsResponse> ListOutgoingPaymentAsync(
        AuthRequestArgs requestArgs,
        ListOutgoingPaymentQuery query,
        CancellationToken cancellationToken = default
    )
    {
        return _client.ListOutgoingPaymentsAsync(
            requestArgs.Url,
            requestArgs.AccessToken,
            query.WalletAddress,
            query.Cursor,
            query.First,
            query.Last,
            cancellationToken
        );
    }
}

/// <summary>
/// Resource-server operations (incoming payments, quotes, outgoing payments) available to an authenticated
/// Open Payments client.
/// </summary>
internal interface IResourceClientBase
{
    /// <summary>
    /// Creates an incoming payment on the resource server.
    /// </summary>
    /// <param name="requestArgs">The resource server URL and access token to use for the request.</param>
    /// <param name="body">The incoming payment properties to create.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The created incoming payment.</returns>
    public Task<IncomingPaymentResponse> CreateIncomingPaymentAsync(
        AuthRequestArgs requestArgs,
        Body body,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retrieves an existing incoming payment from the resource server.
    /// </summary>
    /// <param name="requestArgs">The incoming payment URL and access token to use for the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The requested incoming payment.</returns>
    public Task<IncomingPaymentResponse> GetIncomingPaymentAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Marks an incoming payment as completed, indicating no further funds will be received against it.
    /// </summary>
    /// <param name="requestArgs">The incoming payment URL and access token to use for the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The completed incoming payment.</returns>
    public Task<IncomingPaymentResponse> CompleteIncomingPaymentAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Lists the incoming payments for a wallet address.
    /// </summary>
    /// <param name="requestArgs">The resource server URL and access token to use for the request.</param>
    /// <param name="query">Filtering and pagination parameters for the list.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A page of matching incoming payments.</returns>
    public Task<ListIncomingPaymentsResponse> ListIncomingPaymentsAsync(
        AuthRequestArgs requestArgs,
        ListIncomingPaymentQuery query,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Creates a quote on the resource server.
    /// </summary>
    /// <param name="requestArgs">The resource server URL and access token to use for the request.</param>
    /// <param name="body">The quote properties to create.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The created quote.</returns>
    public Task<QuoteResponse> CreateQuoteAsync(
        AuthRequestArgs requestArgs,
        QuoteBody body,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retrieves an existing quote from the resource server.
    /// </summary>
    /// <param name="requestArgs">The quote URL and access token to use for the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The requested quote.</returns>
    public Task<QuoteResponse> GetQuoteAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Creates an outgoing payment on the resource server.
    /// </summary>
    /// <param name="requestArgs">The resource server URL and access token to use for the request.</param>
    /// <param name="body">The outgoing payment properties to create.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The created outgoing payment, including the amounts spent against it so far.</returns>
    public Task<OutgoingPaymentWithSpentAmountsResponse> CreateOutgoingPaymentAsync(
        AuthRequestArgs requestArgs,
        OutgoingPaymentBody body,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retrieves an existing outgoing payment from the resource server.
    /// </summary>
    /// <param name="requestArgs">The outgoing payment URL and access token to use for the request.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The requested outgoing payment.</returns>
    public Task<OutgoingPaymentResponse> GetOutgoingPaymentAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Lists the outgoing payments for a wallet address.
    /// </summary>
    /// <param name="requestArgs">The resource server URL and access token to use for the request.</param>
    /// <param name="query">Filtering and pagination parameters for the list.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>A page of matching outgoing payments.</returns>
    public Task<ListOutgoingPaymentsResponse> ListOutgoingPaymentAsync(
        AuthRequestArgs requestArgs,
        ListOutgoingPaymentQuery query,
        CancellationToken cancellationToken = default
    );
}

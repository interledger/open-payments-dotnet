using Interledger.OpenPayments.Generated.Resource;

namespace Interledger.OpenPayments.Clients;

/// <summary>Default <see cref="IResourceClientBase"/> implementation over <see cref="ResourceServerClient"/>.</summary>
public class ResourceClientBase : IResourceClientBase
{
    private readonly ResourceServerClient _client;

    /// <summary>Creates the client. Signing must already be configured on <paramref name="http"/>'s handler pipeline.</summary>
    /// <param name="http">The HTTP client used for all requests.</param>
    /// <param name="clientUrl">Client wallet address URL of the SDK consumer, set on the underlying <see cref="ResourceServerClient.ClientUrl"/>.</param>
    public ResourceClientBase(HttpClient http, Uri clientUrl)
    {
        _client = new ResourceServerClient(http);
        _client.ClientUrl = clientUrl;
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
        return _client.PostQuoteAsync(requestArgs.Url, body, requestArgs.AccessToken, cancellationToken);
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
    public Task<ListOutgoingPaymentsResponse> ListOutgoingPaymentsAsync(
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
/// Low-level client for the Open Payments resource server: incoming payments, quotes, and
/// outgoing payments. Wrapped by <see cref="IAuthenticatedClient"/>, which is the surface
/// most consumers should use.
/// </summary>
public interface IResourceClientBase
{
    /// <summary>Creates an incoming payment on the receiving wallet address.</summary>
    /// <param name="requestArgs">Resource server incoming-payments endpoint URL and access token.</param>
    /// <param name="body">The incoming payment request (expiry, metadata).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public Task<IncomingPaymentResponse> CreateIncomingPaymentAsync(
        AuthRequestArgs requestArgs,
        Body body,
        CancellationToken cancellationToken = default
    );

    /// <summary>Fetches the latest state of an incoming payment.</summary>
    /// <param name="requestArgs">Incoming payment resource URL and access token.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public Task<IncomingPaymentResponse> GetIncomingPaymentAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    /// <summary>Marks an incoming payment as completed.</summary>
    /// <param name="requestArgs">Incoming payment resource URL and access token.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public Task<IncomingPaymentResponse> CompleteIncomingPaymentAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists incoming payments on a wallet address, one page at a time.</summary>
    /// <param name="requestArgs">Resource server incoming-payments endpoint URL and access token.</param>
    /// <param name="query">Wallet address and paging parameters (cursor, first, last).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public Task<ListIncomingPaymentsResponse> ListIncomingPaymentsAsync(
        AuthRequestArgs requestArgs,
        ListIncomingPaymentQuery query,
        CancellationToken cancellationToken = default
    );

    /// <summary>Creates a quote for a future outgoing payment.</summary>
    /// <param name="requestArgs">Resource server quotes endpoint URL and access token.</param>
    /// <param name="body">The quote request (receiver and either a debit or receive amount).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public Task<QuoteResponse> CreateQuoteAsync(
        AuthRequestArgs requestArgs,
        QuoteBody body,
        CancellationToken cancellationToken = default
    );

    /// <summary>Fetches a quote.</summary>
    /// <param name="requestArgs">Quote resource URL and access token.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public Task<QuoteResponse> GetQuoteAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    /// <summary>Creates an outgoing payment.</summary>
    /// <param name="requestArgs">Resource server outgoing-payments endpoint URL and access token.</param>
    /// <param name="body">The outgoing payment request, sourced from a quote or an incoming payment.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public Task<OutgoingPaymentWithSpentAmountsResponse> CreateOutgoingPaymentAsync(
        AuthRequestArgs requestArgs,
        OutgoingPaymentBody body,
        CancellationToken cancellationToken = default
    );

    /// <summary>Fetches the latest state of an outgoing payment.</summary>
    /// <param name="requestArgs">Outgoing payment resource URL and access token.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public Task<OutgoingPaymentResponse> GetOutgoingPaymentAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists outgoing payments on a wallet address, one page at a time.</summary>
    /// <param name="requestArgs">Resource server outgoing-payments endpoint URL and access token.</param>
    /// <param name="query">Wallet address and paging parameters (cursor, first, last).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public Task<ListOutgoingPaymentsResponse> ListOutgoingPaymentsAsync(
        AuthRequestArgs requestArgs,
        ListOutgoingPaymentQuery query,
        CancellationToken cancellationToken = default
    );
}

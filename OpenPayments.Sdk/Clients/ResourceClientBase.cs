using System.Runtime.CompilerServices;
using OpenPayments.Sdk.Generated.Resource;

namespace OpenPayments.Sdk.Clients;

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
    public async IAsyncEnumerable<IncomingPayment> ListIncomingPaymentsAllAsync(
        AuthRequestArgs requestArgs,
        ListIncomingPaymentQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        if (query.Last is not null)
            throw new ArgumentException(
                "Backward paging (Last) is not supported by auto-paging; use ListIncomingPaymentsAsync for page-at-a-time access.",
                nameof(query)
            );

        var cursor = query.Cursor;
        while (true)
        {
            var page = await _client
                .ListIncomingPaymentsAsync(
                    requestArgs.Url,
                    requestArgs.AccessToken,
                    query.WalletAddress,
                    cursor,
                    query.First,
                    null,
                    cancellationToken
                )
                .ConfigureAwait(false);

            foreach (var payment in page.Result ?? [])
                yield return payment;

            if (
                page.Pagination is not { HasNextPage: true } pageInfo
                || string.IsNullOrEmpty(pageInfo.EndCursor)
            )
            {
                yield break;
            }

            if (pageInfo.EndCursor == cursor)
                throw new InvalidOperationException(
                    "The server returned the same pagination cursor twice; aborting to avoid an infinite paging loop."
                );

            cursor = pageInfo.EndCursor;
        }
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

    /// <inheritdoc/>
    public async IAsyncEnumerable<OutgoingPayment> ListOutgoingPaymentsAllAsync(
        AuthRequestArgs requestArgs,
        ListOutgoingPaymentQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        if (query.Last is not null)
            throw new ArgumentException(
                "Backward paging (Last) is not supported by auto-paging; use ListOutgoingPaymentsAsync for page-at-a-time access.",
                nameof(query)
            );

        var cursor = query.Cursor;
        while (true)
        {
            var page = await _client
                .ListOutgoingPaymentsAsync(
                    requestArgs.Url,
                    requestArgs.AccessToken,
                    query.WalletAddress,
                    cursor,
                    query.First,
                    null,
                    cancellationToken
                )
                .ConfigureAwait(false);

            foreach (var payment in page.Result ?? [])
                yield return payment;

            if (
                page.Pagination is not { HasNextPage: true } pageInfo
                || string.IsNullOrEmpty(pageInfo.EndCursor)
            )
            {
                yield break;
            }

            if (pageInfo.EndCursor == cursor)
                throw new InvalidOperationException(
                    "The server returned the same pagination cursor twice; aborting to avoid an infinite paging loop."
                );

            cursor = pageInfo.EndCursor;
        }
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

    /// <summary>
    /// Enumerates <b>all</b> incoming payments on a wallet address, transparently following
    /// <c>pageInfo</c> cursors across pages. <see cref="ListIncomingPaymentQuery.First"/> sets the
    /// per-page size and <see cref="ListIncomingPaymentQuery.Cursor"/> the starting position;
    /// <see cref="ListIncomingPaymentQuery.Last"/> must be unset (backward paging is not supported —
    /// use <see cref="ListIncomingPaymentsAsync"/> instead).
    /// </summary>
    /// <param name="requestArgs">Resource server URL and access token.</param>
    /// <param name="query">Wallet address filter, page size, and optional starting cursor.</param>
    /// <param name="cancellationToken">Optional cancellation token, observed between and during page requests.</param>
    public IAsyncEnumerable<IncomingPayment> ListIncomingPaymentsAllAsync(
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

    /// <summary>
    /// Enumerates <b>all</b> outgoing payments on a wallet address, transparently following
    /// <c>pageInfo</c> cursors across pages. <see cref="ListOutgoingPaymentQuery.First"/> sets the
    /// per-page size and <see cref="ListOutgoingPaymentQuery.Cursor"/> the starting position;
    /// <see cref="ListOutgoingPaymentQuery.Last"/> must be unset (backward paging is not supported —
    /// use <see cref="ListOutgoingPaymentsAsync"/> instead).
    /// </summary>
    /// <param name="requestArgs">Resource server URL and access token.</param>
    /// <param name="query">Wallet address filter, page size, and optional starting cursor.</param>
    /// <param name="cancellationToken">Optional cancellation token, observed between and during page requests.</param>
    public IAsyncEnumerable<OutgoingPayment> ListOutgoingPaymentsAllAsync(
        AuthRequestArgs requestArgs,
        ListOutgoingPaymentQuery query,
        CancellationToken cancellationToken = default
    );
}

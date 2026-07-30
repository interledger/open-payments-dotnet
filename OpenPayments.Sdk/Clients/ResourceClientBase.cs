using OpenPayments.Sdk.Generated.Resource;
using OpenPayments.Sdk.Http;

namespace OpenPayments.Sdk.Clients;

public class ResourceClientBase : IResourceClientBase
{
    private readonly ResourceServerClient _client;

    public ResourceClientBase(HttpClient http)
    {
        _client = new ResourceServerClient(http);
    }

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

    public Task<QuoteResponse> GetQuoteAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    )
    {
        return _client.GetQuoteAsync(requestArgs.Url, requestArgs.AccessToken, cancellationToken);
    }

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

public interface IResourceClientBase
{
    public Task<IncomingPaymentResponse> CreateIncomingPaymentAsync(
        AuthRequestArgs requestArgs,
        Body body,
        CancellationToken cancellationToken = default
    );

    public Task<IncomingPaymentResponse> GetIncomingPaymentAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    public Task<IncomingPaymentResponse> CompleteIncomingPaymentAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    public Task<ListIncomingPaymentsResponse> ListIncomingPaymentsAsync(
        AuthRequestArgs requestArgs,
        ListIncomingPaymentQuery query,
        CancellationToken cancellationToken = default
    );

    public Task<QuoteResponse> CreateQuoteAsync(
        AuthRequestArgs requestArgs,
        QuoteBody body,
        CancellationToken cancellationToken = default
    );

    public Task<QuoteResponse> GetQuoteAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    public Task<OutgoingPaymentWithSpentAmountsResponse> CreateOutgoingPaymentAsync(
        AuthRequestArgs requestArgs,
        OutgoingPaymentBody body,
        CancellationToken cancellationToken = default
    );

    public Task<OutgoingPaymentResponse> GetOutgoingPaymentAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );

    public Task<ListOutgoingPaymentsResponse> ListOutgoingPaymentAsync(
        AuthRequestArgs requestArgs,
        ListOutgoingPaymentQuery query,
        CancellationToken cancellationToken = default
    );
}

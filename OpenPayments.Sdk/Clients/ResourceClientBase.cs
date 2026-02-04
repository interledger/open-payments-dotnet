using NSec.Cryptography;
using OpenPayments.Sdk.Generated.Resource;

namespace OpenPayments.Sdk.Clients;

public class ResourceClientBase : IResourceClientBase
{
    private readonly ResourceServerClient _client;
    private readonly HttpClient _httpClient;

    public ResourceClientBase(HttpClient http, Key privateKey, string keyId, Uri clientUrl)
    {
        _httpClient = http;
        _client = new ResourceServerClient(http);
        _client.AddSigningKey(privateKey, keyId);
        _client.ClientUrl = clientUrl;
    }

    public async Task<IncomingPaymentResponse> CreateIncomingPaymentAsync(AuthRequestArgs requestArgs, Body body,
        CancellationToken cancellationToken = default)
    {
        _client.BaseUrl = requestArgs.Url.ToString();

        return await _client.PostIncomingPaymentAsync(body, requestArgs.AccessToken, cancellationToken);
    }

    public async Task<IncomingPaymentResponse> GetIncomingPaymentAsync(AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default)
    {
        _client.BaseUrl = requestArgs.Url.ToString();

        return await _client.GetIncomingPaymentAsync(requestArgs.AccessToken, cancellationToken);
    }

    public async Task<IncomingPaymentResponse> CompleteIncomingPaymentAsync(AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default)
    {
        _client.BaseUrl = requestArgs.Url.ToString();

        return await _client.CompleteIncomingPaymentAsync(requestArgs.AccessToken, cancellationToken);
    }

    public async Task<ListIncomingPaymentsResponse> ListIncomingPaymentsAsync(AuthRequestArgs requestArgs,
        ListIncomingPaymentQuery query, CancellationToken cancellationToken = default)
    {
        _client.BaseUrl = requestArgs.Url.ToString();

        return await _client.ListIncomingPaymentsAsync(requestArgs.AccessToken, query.WalletAddress, query.Cursor,
            query.First, query.Last, cancellationToken);
    }

    public async Task<QuoteResponse> CreateQuoteAsync(AuthRequestArgs requestArgs, QuoteBody body,
        CancellationToken cancellationToken = default)
    {
        _client.BaseUrl = requestArgs.Url.ToString();

        return await _client.PostQuoteAsync(body, requestArgs.AccessToken, cancellationToken);
    }

    public async Task<OutgoingPaymentResponse> CreateOutgoingPaymentAsync(AuthRequestArgs requestArgs,
        OutgoingPaymentBody body, CancellationToken cancellationToken = default)
    {
        _client.BaseUrl = requestArgs.Url.ToString();

        return await _client.PostOutgoingPaymentAsync(body, requestArgs.AccessToken, cancellationToken);
    }
}

public interface IResourceClientBase
{
    public Task<IncomingPaymentResponse> CreateIncomingPaymentAsync(AuthRequestArgs requestArgs, Body body,
        CancellationToken cancellationToken = default);

    public Task<IncomingPaymentResponse> GetIncomingPaymentAsync(AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default);

    public Task<IncomingPaymentResponse> CompleteIncomingPaymentAsync(AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default);

    public Task<ListIncomingPaymentsResponse> ListIncomingPaymentsAsync(AuthRequestArgs requestArgs,
        ListIncomingPaymentQuery query, CancellationToken cancellationToken = default);

    public Task<QuoteResponse> CreateQuoteAsync(AuthRequestArgs requestArgs, QuoteBody body,
        CancellationToken cancellationToken = default);

    public Task<OutgoingPaymentResponse> CreateOutgoingPaymentAsync(AuthRequestArgs requestArgs,
        OutgoingPaymentBody body,
        CancellationToken cancellationToken = default);
}
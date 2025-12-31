using NSec.Cryptography;
using OpenPayments.Sdk.Generated.Resource;

namespace OpenPayments.Sdk.Clients;

/// <remarks>
/// Create a new AuthenticatedClient wrapping an existing <see cref="HttpClient"/>.
/// </remarks>
/// <param name="http">Pre-configured <see cref="HttpClient"/> instance. Its <see cref="HttpClient.BaseAddress"/> is ignored; absolute request URIs are used instead.</param>
internal sealed class AuthenticatedClient(HttpClient http, Key privateKey, string keyId, Uri clientUrl)
    : WalletAddressClientBase(http), IAuthenticatedClient
{
    private readonly AuthClientBase _authClient = new(http, privateKey, keyId, clientUrl);
    private readonly ResourceClientBase _resClient = new(http, privateKey, keyId, clientUrl);

    public Task<Generated.Auth.AuthResponse> RequestGrantAsync(RequestArgs requestArgs,
        Generated.Auth.GrantCreateBody body,
        CancellationToken cancellationToken = default)
    {
        return _authClient.RequestGrantAsync(requestArgs, body, cancellationToken);
    }

    public Task<Generated.Auth.AuthResponse> ContinueGrantAsync(RequestArgs requestArgs,
        Generated.Auth.GrantContinueBody body,
        CancellationToken cancellationToken = default)
    {
        return _authClient.ContinueGrantAsync(requestArgs, body, cancellationToken);
    }

    public Task CancelGrantAsync(RequestArgs requestArgs,
        CancellationToken cancellationToken = default)
    {
        return _authClient.CancelGrantAsync(requestArgs, cancellationToken);
    }

    public Task<IncomingPaymentResponse> CreateIncomingPaymentAsync(RequestArgs requestArgs, IncomingPaymentBody body,
        CancellationToken cancellationToken = default)
    {
        return _resClient.CreateIncomingPaymentAsync(requestArgs, body, cancellationToken);
    }

    public Task<QuoteResponse> CreateQuoteAsync(RequestArgs requestArgs, QuoteBody body,
        CancellationToken cancellationToken = default)
    {
        return _resClient.CreateQuoteAsync(requestArgs, body, cancellationToken);
    }

    public Task<OutgoingPaymentResponse> CreateOutgoingPaymentAsync(RequestArgs requestArgs, OutgoingPaymentBody body,
        CancellationToken cancellationToken = default)
    {
        return _resClient.CreateOutgoingPaymentAsync(requestArgs, body, cancellationToken);
    }
}

public class RequestArgs
{
    public required Uri Url { get; set; }
    public string? AccessToken { get; set; }
}


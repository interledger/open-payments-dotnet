using NSec.Cryptography;
using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Generated.Resource;

namespace OpenPayments.Sdk.Clients;

/// <remarks>
/// Create a new AuthenticatedClient wrapping an existing <see cref="UnauthenticatedClient"/> instance.
/// </remarks>
/// <param name="http">Pre-configured <see cref="HttpClient"/> instance. It's <see cref="HttpClient.BaseAddress"/> is ignored; absolute request URIs are used instead.</param>
/// <param name="privateKey">Private key used to sign requests.</param>
/// <param name="keyId">Key ID used to sign requests.</param>
/// <param name="clientUrl">Client Wallet URL Address (e.g. <c>https://wallet.example</c>).</param>
internal sealed class AuthenticatedClient(HttpClient http, Key privateKey, string keyId, Uri clientUrl)
    : UnauthenticatedClient(http), IAuthenticatedClient
{
    private readonly IAuthClientBase _authClient = new AuthClientBase(http, privateKey, keyId, clientUrl);
    private readonly IResourceClientBase _resClient = new ResourceClientBase(http, privateKey, keyId, clientUrl);

    public Task<AuthResponse> RequestGrantAsync(RequestArgs requestArgs,
        GrantCreateBody body,
        CancellationToken cancellationToken = default)
    {
        return _authClient.RequestGrantAsync(requestArgs, body, cancellationToken);
    }

    public Task<AuthResponse> ContinueGrantAsync(AuthRequestArgs requestArgs,
        GrantContinueBody? body,
        CancellationToken cancellationToken = default)
    {
        body ??= new GrantContinueBody();
        return _authClient.ContinueGrantAsync(requestArgs, body, cancellationToken);
    }

    public Task CancelGrantAsync(AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default)
    {
        return _authClient.CancelGrantAsync(requestArgs, cancellationToken);
    }

    public Task<RotateTokenResponse> RotateTokenAsync(AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default)
    {
        return _authClient.RotateTokenAsync(requestArgs, cancellationToken);
    }
    
    public Task RevokeTokenAsync(AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default)
    {
        return _authClient.RevokeTokenAsync(requestArgs, cancellationToken);
    }

    public Task<IncomingPaymentResponse> CreateIncomingPaymentAsync(AuthRequestArgs requestArgs, IncomingPaymentBody body,
        CancellationToken cancellationToken = default)
    {
        return _resClient.CreateIncomingPaymentAsync(requestArgs, body, cancellationToken);
    }

    public Task<QuoteResponse> CreateQuoteAsync(AuthRequestArgs requestArgs, QuoteBody body,
        CancellationToken cancellationToken = default)
    {
        return _resClient.CreateQuoteAsync(requestArgs, body, cancellationToken); 
    }

    public Task<OutgoingPaymentResponse> CreateOutgoingPaymentAsync(AuthRequestArgs requestArgs, OutgoingPaymentBody body,
        CancellationToken cancellationToken = default)
    {
        return _resClient.CreateOutgoingPaymentAsync(requestArgs, body, cancellationToken);
    }
}

public class RequestArgs
{
    public required Uri Url { get; set; }
}

public class AuthRequestArgs : RequestArgs
{
    public required string AccessToken { get; set; }
}


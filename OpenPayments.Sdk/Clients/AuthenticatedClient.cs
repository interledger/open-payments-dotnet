using Newtonsoft.Json;
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

    /// <inheritdoc/>
    public Task<AuthResponse> RequestGrantAsync(RequestArgs requestArgs,
        GrantCreateBody body,
        CancellationToken cancellationToken = default)
    {
        return _authClient.RequestGrantAsync(requestArgs, body, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<AuthResponse> ContinueGrantAsync(AuthRequestArgs requestArgs,
        GrantContinueBody? body,
        CancellationToken cancellationToken = default)
    {
        body ??= new GrantContinueBody();
        return _authClient.ContinueGrantAsync(requestArgs, body, cancellationToken);
    }

    /// <inheritdoc/>
    public Task CancelGrantAsync(AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default)
    {
        return _authClient.CancelGrantAsync(requestArgs, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<RotateTokenResponse> RotateTokenAsync(AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default)
    {
        return _authClient.RotateTokenAsync(requestArgs, cancellationToken);
    }

    /// <inheritdoc/>
    public Task RevokeTokenAsync(AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default)
    {
        return _authClient.RevokeTokenAsync(requestArgs, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IncomingPaymentResponse> CreateIncomingPaymentAsync(AuthRequestArgs requestArgs,
        IncomingPaymentBody body,
        CancellationToken cancellationToken = default)
    {
        return _resClient.CreateIncomingPaymentAsync(requestArgs, body, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IncomingPaymentResponse> GetIncomingPaymentAsync(AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default)
    {
        return _resClient.GetIncomingPaymentAsync(requestArgs, cancellationToken);
    }

    /// <inheritdoc cref="UnauthenticatedClient.GetIncomingPaymentAsync"/>
    public Task<PublicIncomingPayment> GetPublicIncomingPaymentAsync(RequestArgs requestArgs, CancellationToken cancellationToken = default)
    {
        return base.GetIncomingPaymentAsync(requestArgs.Url.ToString(), cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ListIncomingPaymentsResponse> ListIncomingPaymentsAsync(AuthRequestArgs requestArgs,
        ListIncomingPaymentQuery query, CancellationToken cancellationToken = default)
    {
        return _resClient.ListIncomingPaymentsAsync(requestArgs, query, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IncomingPaymentResponse> CompleteIncomingPaymentsAsync(AuthRequestArgs requestArgs, CancellationToken cancellationToken = default)
    {
        return _resClient.CompleteIncomingPaymentAsync(requestArgs, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<QuoteResponse> CreateQuoteAsync(AuthRequestArgs requestArgs, QuoteBody body,
        CancellationToken cancellationToken = default)
    {
        return _resClient.CreateQuoteAsync(requestArgs, body, cancellationToken);
    }
    
    /// <inheritdoc/>
    public Task<QuoteResponse> CreateQuoteAsync(AuthRequestArgs requestArgs, QuoteBodyWithDebitAmount body,
        CancellationToken cancellationToken = default)
    {
        return _resClient.CreateQuoteAsync(requestArgs, body, cancellationToken);
    }
    
    /// <inheritdoc/>
    public Task<QuoteResponse> CreateQuoteAsync(AuthRequestArgs requestArgs, QuoteBodyWithReceiveAmount body,
        CancellationToken cancellationToken = default)
    {
        return _resClient.CreateQuoteAsync(requestArgs, body, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<QuoteResponse> GetQuoteAsync(AuthRequestArgs requestArgs, CancellationToken cancellationToken = default)
    {
        return _resClient.GetQuoteAsync(requestArgs, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<OutgoingPaymentWithSpentAmountsResponse> CreateOutgoingPaymentAsync(AuthRequestArgs requestArgs,
        OutgoingPaymentBodyFromQuote body,
        CancellationToken cancellationToken = default)
    {
        return _resClient.CreateOutgoingPaymentAsync(requestArgs, body, cancellationToken);
    }
    
    /// <inheritdoc/>
    public Task<OutgoingPaymentWithSpentAmountsResponse> CreateOutgoingPaymentAsync(AuthRequestArgs requestArgs,
        OutgoingPaymentBodyFromIncomingPayment body,
        CancellationToken cancellationToken = default)
    {
        return _resClient.CreateOutgoingPaymentAsync(requestArgs, body, cancellationToken);
    }

    public Task<OutgoingPaymentResponse> GetOutgoingPaymentAsync(AuthRequestArgs requestArgs, CancellationToken cancellationToken = default)
    {
        return _resClient.GetOutgoingPaymentAsync(requestArgs, cancellationToken);
    }

    public Task<ListOutgoingPaymentsResponse> ListOutgoingPaymentsAsync(AuthRequestArgs requestArgs, ListOutgoingPaymentQuery query,
        CancellationToken cancellationToken = default)
    {
        return _resClient.ListOutgoingPaymentAsync(requestArgs, query, cancellationToken);
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
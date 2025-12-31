using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Generated.Resource;

namespace OpenPayments.Sdk.Clients;

/// <summary>
/// Represents a client used to interact with Open Payments endpoints
/// that require authentication.
/// </summary>
public interface IAuthenticatedClient : IUnauthenticatedClient
{
    /// <summary>
    /// Resolve a wallet-address URL (or payment pointer) and return its public metadata.
    /// </summary>
    /// <param name="requestArgs">Auth Server URL Address (e.g. <c>https://auth.wallet.example</c>) and access token.</param>
    /// <param name="body">Request body</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public Task<AuthResponse> RequestGrantAsync(RequestArgs requestArgs,
        GrantCreateBody body,
        CancellationToken cancellationToken = default);

    public Task<AuthResponse> ContinueGrantAsync(AuthRequestArgs requestArgs,
        GrantContinueBody? body = null,
        CancellationToken cancellationToken = default);

    public Task CancelGrantAsync(AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default);
    
    public Task<RotateTokenResponse> RotateTokenAsync(AuthRequestArgs requestArgs, CancellationToken cancellationToken = default);

    public Task RevokeTokenAsync(AuthRequestArgs requestArgs, CancellationToken cancellationToken = default);

    public Task<IncomingPaymentResponse> CreateIncomingPaymentAsync(AuthRequestArgs requestArgs,
        IncomingPaymentBody body, CancellationToken cancellationToken = default);

    public Task<QuoteResponse> CreateQuoteAsync(AuthRequestArgs requestArgs, QuoteBody body,
        CancellationToken cancellationToken = default);

    public Task<OutgoingPaymentResponse> CreateOutgoingPaymentAsync(AuthRequestArgs requestArgs, OutgoingPaymentBody body,
        CancellationToken cancellationToken = default);
}
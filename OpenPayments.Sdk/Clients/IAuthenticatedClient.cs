using OpenPayments.Sdk.Generated.Resource;

namespace OpenPayments.Sdk.Clients;

/// <summary>
/// Represents a client used to interact with Open Payments endpoints
/// that require authentication.
/// </summary>
public interface IAuthenticatedClient
{
    /// <summary>
    /// Resolve a wallet-address URL (or payment pointer) and return its public metadata.
    /// </summary>
    /// <param name="requestArgs">Auth Server URL Address (e.g. <c>https://auth.wallet.example</c>) and access token.</param>
    /// <param name="body">Request body</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public Task<Generated.Auth.AuthResponse> RequestGrantAsync(RequestArgs requestArgs,
        Generated.Auth.GrantCreateBody body,
        CancellationToken cancellationToken = default);

    public Task<Generated.Auth.AuthResponse> ContinueGrantAsync(RequestArgs requestArgs,
        Generated.Auth.GrantContinueBody body,
        CancellationToken cancellationToken = default);

    public Task CancelGrantAsync(RequestArgs requestArgs,
        CancellationToken cancellationToken = default);

    public Task<IncomingPaymentResponse> CreateIncomingPaymentAsync(RequestArgs requestArgs,
        IncomingPaymentBody body, CancellationToken cancellationToken = default);

    public Task<QuoteResponse> CreateQuoteAsync(RequestArgs requestArgs, QuoteBody body,
        CancellationToken cancellationToken = default);

    public Task<OutgoingPaymentResponse> CreateOutgoingPaymentAsync(RequestArgs requestArgs, OutgoingPaymentBody body,
        CancellationToken cancellationToken = default);
}
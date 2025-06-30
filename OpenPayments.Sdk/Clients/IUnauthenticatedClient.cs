using OpenPayments.Sdk.Generated.Resource;
using OpenPayments.Sdk.Generated.Wallet;

namespace OpenPayments.Sdk.Clients;

/// <summary>
/// Represents a client used to interact with Open Payments endpoints
/// that do not require authentication.
/// </summary>
public interface IUnauthenticatedClient
{
    /// <summary>
    /// Resolve a wallet-address URL (or payment pointer) and return its public metadata.
    /// </summary>
    /// <param name="walletAddressOrPaymentPointer">Absolute wallet-address URL (e.g. <c>https://wallet.example/alice</c>) OR payment pointer (<c>$wallet.example/alice</c>).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    Task<WalletAddress> GetWalletAddressAsync(string walletAddressOrPaymentPointer, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch an <b>incoming payment</b> resource by its absolute URL.
    /// </summary>
    /// <param name="incomingPaymentUrl">Absolute <c>incoming-payments/&lt;id&gt;</c> URL.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    Task<PublicIncomingPayment> GetIncomingPaymentAsync(string incomingPaymentUrl, CancellationToken cancellationToken = default);
}
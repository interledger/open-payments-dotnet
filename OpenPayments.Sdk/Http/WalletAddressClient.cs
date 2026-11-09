using Newtonsoft.Json;
using OpenPayments.Sdk.Generated.Wallet;

namespace OpenPayments.Sdk.Http;

/// <summary>
/// Low-level transport client for public wallet address endpoints. These requests are unsigned.
/// Most callers should use <see cref="OpenPayments.Sdk.Clients.IUnauthenticatedClient"/> instead.
/// </summary>
public class WalletAddressClient : OpenPaymentsClientBase
{
    /// <param name="httpClient">The client to send requests with; not disposed by this class.</param>
    public WalletAddressClient(HttpClient httpClient)
        : base(httpClient, new JsonSerializerSettings())
    {
    }

    /// <summary>
    /// Retrieves the public information of a wallet address.
    /// </summary>
    /// <param name="walletAddress">The absolute URL of the wallet address.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The wallet address's public information.</returns>
    /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
    public async Task<WalletAddress> GetWalletAddressAsync(
        string walletAddress,
        CancellationToken cancellationToken
    )
    {
        return await SendAsync<WalletAddress>(
                HttpMethod.Get,
                new Uri(walletAddress, UriKind.RelativeOrAbsolute),
                body: null,
                accessToken: null,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the JSON Web Key Set (JWKS) bound to a wallet address.
    /// </summary>
    /// <param name="walletAddress">The absolute URL of the wallet address.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The wallet address's public keys.</returns>
    /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
    public async Task<JsonWebKeySet> GetWalletAddressKeysAsync(
        string walletAddress,
        CancellationToken cancellationToken
    )
    {
        return await SendAsync<JsonWebKeySet>(
                HttpMethod.Get,
                new Uri(walletAddress.TrimEnd('/') + "/jwks.json", UriKind.RelativeOrAbsolute),
                body: null,
                accessToken: null,
                cancellationToken)
            .ConfigureAwait(false);
    }
}

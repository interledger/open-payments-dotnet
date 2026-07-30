using Newtonsoft.Json;
using OpenPayments.Sdk.Generated.Wallet;

namespace OpenPayments.Sdk.Http;

public class WalletAddressClient : OpenPaymentsClientBase
{
    public WalletAddressClient(HttpClient httpClient)
        : base(httpClient, new JsonSerializerSettings())
    {
    }

    /// <param name="walletAddress">The absolute URL of the wallet address.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
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

    /// <param name="walletAddress">The absolute URL of the wallet address.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
    public async Task<JsonWebKeySet> GetWalletAddressKeysAsync(
        string walletAddress,
        CancellationToken cancellationToken
    )
    {
        return await SendAsync<JsonWebKeySet>(
                HttpMethod.Get,
                new Uri(walletAddress + "jwks.json", UriKind.RelativeOrAbsolute),
                body: null,
                accessToken: null,
                cancellationToken)
            .ConfigureAwait(false);
    }
}

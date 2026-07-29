using System.Text;
using OpenPayments.Sdk.Http;

namespace OpenPayments.Sdk.Generated.Wallet
{
    public partial class WalletAddressClient
    {
        /// <param name="walletAddress">The absolute URL of the wallet address.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
        public async Task<WalletAddress> GetWalletAddressAsync(
            string walletAddress,
            CancellationToken cancellationToken
        )
        {
            var client = _httpClient;
            using var request = new HttpRequestMessage();
            request.Method = new HttpMethod("GET");
            request.Headers.Accept.Add(
                System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json")
            );

            var urlBuilder = new StringBuilder(walletAddress);

            PrepareRequest(client, request, urlBuilder);

            var url = urlBuilder.ToString();
            request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);

            PrepareRequest(client, request, url);

            var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                await OpenPaymentsResponse
                    .ThrowIfErrorAsync(response, cancellationToken)
                    .ConfigureAwait(false);

                return await OpenPaymentsResponse
                    .ReadRequiredAsync<WalletAddress>(
                        response,
                        JsonSerializerSettings,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }
            finally
            {
                response.Dispose();
            }
        }

        /// <param name="walletAddress">The absolute URL of the wallet address.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <exception cref="OpenPayments.Sdk.Exceptions.OpenPaymentsApiException">The request failed.</exception>
        public async Task<JsonWebKeySet> GetWalletAddressKeysAsync(
            string walletAddress,
            CancellationToken cancellationToken
        )
        {
            var client = _httpClient;
            using var request = new HttpRequestMessage();
            request.Method = new HttpMethod("GET");
            request.Headers.Accept.Add(
                System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json")
            );

            var urlBuilder = new StringBuilder(walletAddress);
            // Operation Path: "jwks.json"
            urlBuilder.Append("jwks.json");

            PrepareRequest(client, request, urlBuilder);

            var url = urlBuilder.ToString();
            request.RequestUri = new Uri(url, UriKind.RelativeOrAbsolute);

            PrepareRequest(client, request, url);

            var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                await OpenPaymentsResponse
                    .ThrowIfErrorAsync(response, cancellationToken)
                    .ConfigureAwait(false);

                return await OpenPaymentsResponse
                    .ReadRequiredAsync<JsonWebKeySet>(
                        response,
                        JsonSerializerSettings,
                        cancellationToken
                    )
                    .ConfigureAwait(false);
            }
            finally
            {
                response.Dispose();
            }
        }
    }
}

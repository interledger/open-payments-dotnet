using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using OpenPayments.Sdk.Generated.Resource;
using OpenPayments.Sdk.Generated.Wallet;
using OpenPayments.Sdk.Http;

[assembly: InternalsVisibleTo("OpenPayments.Sdk.Tests")]

namespace OpenPayments.Sdk.Clients;

/// <inheritdoc/>
/// <remarks>
/// Create a new UnauthenticatedClient wrapping an existing <see cref="HttpClient"/>.
/// </remarks>
/// <param name="http">Pre-configured <see cref="HttpClient"/> instance. Its <see cref="HttpClient.BaseAddress"/> is ignored; absolute request URIs are used instead.</param>
internal class UnauthenticatedClient(HttpClient http)
    : WalletAddressClientBase(http),
        IUnauthenticatedClient
{
    /// <inheritdoc/>
    public async Task<WalletAddress> GetWalletAddressAsync(
        string walletAddressOrPaymentPointer,
        CancellationToken cancellationToken = default
    )
    {
        return await GetWalletAddressInternalAsync(walletAddressOrPaymentPointer, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<JsonWebKeySet> GetWalletAddressKeysAsync(
        string walletAddressOrPaymentPointer,
        CancellationToken cancellationToken = default
    )
    {
        return await GetWalletAddressKeysInternalAsync(
                walletAddressOrPaymentPointer,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<PublicIncomingPayment> GetIncomingPaymentAsync(
        string incomingPaymentUrl,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(incomingPaymentUrl))
            throw new ArgumentException(
                "Value cannot be null or whitespace.",
                nameof(incomingPaymentUrl)
            );

        using var request = new HttpRequestMessage(HttpMethod.Get, incomingPaymentUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        await OpenPaymentsResponse
            .ThrowIfErrorAsync(response, cancellationToken)
            .ConfigureAwait(false);

        // settings: null preserves this method's existing default-JsonConvert behaviour.
        // Unifying serializer settings across clients is issue #27.
        return await OpenPaymentsResponse
            .ReadRequiredAsync<PublicIncomingPayment>(response, null, cancellationToken)
            .ConfigureAwait(false);
    }
}

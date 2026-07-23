using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using OpenPayments.Sdk;
using OpenPayments.Sdk.Generated.Resource;
using OpenPayments.Sdk.Generated.Wallet;

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

        var json = await response
            .Content.ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);
        var responseHeaders = new Dictionary<string, IEnumerable<string>>();
        foreach (var item in response.Headers)
            responseHeaders[item.Key] = item.Value;
        foreach (var item in response.Content.Headers)
            responseHeaders[item.Key] = item.Value;

        if (!response.IsSuccessStatusCode)
        {
            throw OpenPaymentsExceptionFactory.Create(
                $"The HTTP status code of the response was not expected ({(int)response.StatusCode}).",
                (int)response.StatusCode,
                null,
                json,
                responseHeaders
            );
        }

        var model = JsonConvert.DeserializeObject<PublicIncomingPayment>(json);

        return model
            ?? throw OpenPaymentsExceptionFactory.Create(
                "Server returned empty or invalid IncomingPayment JSON.",
                (int)response.StatusCode,
                null,
                json,
                responseHeaders
            );
    }
}

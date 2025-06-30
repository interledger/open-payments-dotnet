using System.Net.Http.Headers;
using Newtonsoft.Json;
using OpenPayments.Sdk.Generated.Wallet;
using OpenPayments.Sdk.Generated.Resource;

namespace OpenPayments.Sdk.Clients;

/// <inheritdoc/>
/// <remarks>
/// Create a new UnauthenticatedClient wrapping an existing <see cref="HttpClient"/>.
/// </remarks>
/// <param name="http">Pre-configured <see cref="HttpClient"/> instance. Its <see cref="HttpClient.BaseAddress"/> is ignored; absolute request URIs are used instead.</param>
public sealed class UnauthenticatedClient(HttpClient http) : IUnauthenticatedClient
{
    private readonly HttpClient _http = http;

    /// <summary>
    /// Create a new UnauthenticatedClient
    /// </summary>
    public UnauthenticatedClient() : this(new HttpClient()) { }

    /// <inheritdoc/>
    public async Task<WalletAddress> GetWalletAddressAsync(string walletAddressOrPaymentPointer, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddressOrPaymentPointer))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(walletAddressOrPaymentPointer));

        var url = NormalizeWalletAddress(walletAddressOrPaymentPointer);

        // NSwag-generated client requires a base URL ending with '/'.
        var httpClient = _http;
        var client = new WalletAddressClient(httpClient) { BaseUrl = url }; // setter adds trailing '/'
        return await client.GetWalletAddressAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<PublicIncomingPayment> GetIncomingPaymentAsync(string incomingPaymentUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(incomingPaymentUrl))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(incomingPaymentUrl));

        using var request = new HttpRequestMessage(HttpMethod.Get, incomingPaymentUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var model = JsonConvert.DeserializeObject<PublicIncomingPayment>(json);
        if (model == null)
            throw new InvalidOperationException("Server returned empty or invalid IncomingPayment JSON.");
        return model;
    }

    private static string NormalizeWalletAddress(string input)
    {
        // If it's already a URI – return as-is
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
            return uri.ToString();

        // Otherwise treat as payment pointer ($example.com/alice)
        if (input.StartsWith('$'))
        {
            var withoutDollar = input.Substring(1);
            return $"https://{withoutDollar}"; // Payment-pointer → HTTPS URL
        }

        throw new ArgumentException("Input must be an absolute URL or a payment pointer string starting with '$'.", nameof(input));
    }
} 
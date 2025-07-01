using System.Text;
using OpenPayments.Sdk.Generated.Wallet;

namespace OpenPayments.Sdk.Clients;

internal abstract class WalletAddressClientBase
{
    protected readonly HttpClient _http;

    protected WalletAddressClientBase(HttpClient http)
    {
        _http = http;
    }

    private WalletAddressClient GetWalletAddressClient(string walletAddressOrPaymentPointer, string suffix = "") {
        if (string.IsNullOrWhiteSpace(walletAddressOrPaymentPointer))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(walletAddressOrPaymentPointer));

        var url = NormalizeWalletAddress(walletAddressOrPaymentPointer);
        StringBuilder builder = new(NormalizeWalletAddress(walletAddressOrPaymentPointer));

        if (!string.IsNullOrWhiteSpace(suffix))
        {
            builder.Append(suffix);   
        }

        var client = new WalletAddressClient(_http) { BaseUrl = builder.ToString() };

        return client;
    }

    protected async Task<WalletAddress> GetWalletAddressInternalAsync(string walletAddressOrPaymentPointer, CancellationToken cancellationToken = default)
    {
        return await GetWalletAddressClient(walletAddressOrPaymentPointer)
            .GetWalletAddressAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    protected async Task<JsonWebKeySet> GetWalletAddressKeysInternalAsync(string walletAddressOrPaymentPointer, CancellationToken cancellationToken = default)
    {
        return await GetWalletAddressClient(walletAddressOrPaymentPointer, "/jwks.json")
            .GetWalletAddressKeysAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static string NormalizeWalletAddress(string input)
    {
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
            return uri.ToString();

        if (input.StartsWith('$'))
        {
            var withoutDollar = input.Substring(1);
            return $"https://{withoutDollar}";
        }

        throw new ArgumentException("Input must be an absolute URL or a payment pointer string starting with '$'.", nameof(input));
    }
}

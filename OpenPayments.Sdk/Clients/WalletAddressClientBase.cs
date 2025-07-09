using System.Text;
using OpenPayments.Sdk.Generated.Wallet;

namespace OpenPayments.Sdk.Clients;

internal abstract class WalletAddressClientBase
{
    protected readonly WalletAddressClient _client;
    protected readonly HttpClient _httpClient;

    protected WalletAddressClientBase(HttpClient http)
    {
        _httpClient = http;
        _client = new WalletAddressClient(http);
    }

    private string GetWalletAddressUrl(string walletAddressOrPaymentPointer, string suffix = "")
    {
        if (string.IsNullOrWhiteSpace(walletAddressOrPaymentPointer))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(walletAddressOrPaymentPointer));

        var url = NormalizeWalletAddress(walletAddressOrPaymentPointer);
        StringBuilder builder = new(NormalizeWalletAddress(walletAddressOrPaymentPointer));

        if (!string.IsNullOrWhiteSpace(suffix))
        {
            builder.Append(suffix);
        }

        return builder.ToString();
    }

    protected async Task<WalletAddress> GetWalletAddressInternalAsync(string walletAddressOrPaymentPointer, CancellationToken cancellationToken = default)
    {
        string walletAddress = GetWalletAddressUrl(walletAddressOrPaymentPointer);
        return await _client
            .GetWalletAddressAsync(walletAddress, cancellationToken)
            .ConfigureAwait(false);
    }

    protected async Task<JsonWebKeySet> GetWalletAddressKeysInternalAsync(string walletAddressOrPaymentPointer, CancellationToken cancellationToken = default)
    {
        string walletAddress = GetWalletAddressUrl(walletAddressOrPaymentPointer);
        return await _client
            .GetWalletAddressKeysAsync(walletAddress, cancellationToken)
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

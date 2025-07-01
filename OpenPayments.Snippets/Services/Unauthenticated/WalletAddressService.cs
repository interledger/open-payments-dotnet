using OpenPayments.Sdk.Clients;

namespace OpenPayments.Snippets.Services.Unauthenticated;

public class WalletAddressService(IUnauthenticatedClient client)
{
    private readonly IUnauthenticatedClient _client = client;

    public async Task DisplayWalletAddressKeysAsync(string address)
    {
        var walletAddressKeys = await _client.GetWalletAddressKeysAsync(address);

        Console.WriteLine("=== Wallet Keys ===");
        foreach (var key in walletAddressKeys.Keys)
        {
            Console.WriteLine("Alg: {0}", key.Alg);
            Console.WriteLine("Crv: {0}", key.Crv);
            Console.WriteLine("Kid: {0}", key.Kid);
            Console.WriteLine("Kty: {0}", key.Kty);
            Console.WriteLine("X: {0}", key.X);
            Console.WriteLine("===================");
        }
    }

    public async Task DisplayWalletInfoAsync(string address)
    {
        var walletAddressData = await client.GetWalletAddressAsync(address);
        Console.WriteLine("===Wallet Info===");
        Console.WriteLine("PublicName: {0}", walletAddressData.PublicName);
        Console.WriteLine("AssetCode: {0}", walletAddressData.AssetCode);
        Console.WriteLine("AssetScale: {0}", walletAddressData.AssetScale);
        Console.WriteLine("AuthServer: {0}", walletAddressData.AuthServer);
        Console.WriteLine("ResourceServer: {0}", walletAddressData.ResourceServer);
    }
}
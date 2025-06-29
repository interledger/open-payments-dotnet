using System.CommandLine;
using System.CommandLine.Parsing;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using OpenPayments.Sdk;
using OpenPayments.Sdk.Clients;

var services = new ServiceCollection();

Option<string> modelOption = new("--model", "-m") {
    Description = "The model to operate on (e.g. 'wallet-address')"
};

Option<string> addressOption = new("--address", "-a")
{
    Description = "The wallet address URL",
    Required = true
};

var rootCommand = new RootCommand("OpenPayments CLI");
var walletAddressCommand = new Command("WalletAddress")
{
    addressOption
};

walletAddressCommand.SetAction(async result =>
{
    var address = result.GetRequiredValue(addressOption);
    services.UseOpenPayments(opts => opts.UseUnauthenticatedClient());

    var provider = services.BuildServiceProvider();
    var client = provider.GetRequiredService<UnauthenticatedClient>();

    var walletAddressData = await client.GetWalletAddressAsync(address);
    Console.WriteLine("===Wallet Info===");
    Console.WriteLine("PublicName: {0}", walletAddressData.PublicName);
    Console.WriteLine("AssetCode: {0}", walletAddressData.AssetCode);
    Console.WriteLine("AssetScale: {0}", walletAddressData.AssetScale);
    Console.WriteLine("AuthServer: {0}", walletAddressData.AuthServer);
    Console.WriteLine("ResourceServer: {0}", walletAddressData.ResourceServer);
});

var walletKeysCommand = new Command("keys", "Fetch wallet keys") {
    addressOption
};

walletAddressCommand.Add(walletKeysCommand);
rootCommand.Add(walletAddressCommand);

var config = new CommandLineConfiguration(rootCommand);
return await config.InvokeAsync(args);
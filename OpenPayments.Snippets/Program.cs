using System.CommandLine;
using System.CommandLine.Parsing;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using OpenPayments.Sdk;
using OpenPayments.Sdk.Clients;

var services = new ServiceCollection();

Option<string> resourceUrlOption = new("--resource", "-r")
{
    Description = "The URL of the resource",
    Required = true
};

var rootCommand = new RootCommand("OpenPayments CLI");
var walletAddressCommand = new Command("WalletAddress")
{
    resourceUrlOption
};

var incomingPaymentCommand = new Command("IncomingPayment") {
    resourceUrlOption
};

incomingPaymentCommand.SetAction(async result =>
{
    var incomingPaymentUrl = result.GetRequiredValue(resourceUrlOption);

    services.UseOpenPayments(opts => opts.UseUnauthenticatedClient());

    var provider = services.BuildServiceProvider();
    var client = provider.GetRequiredService<UnauthenticatedClient>();

    var incomingPayment = await client.GetIncomingPaymentAsync(incomingPaymentUrl);
    Console.WriteLine("===Incoming Payment Info===");
    Console.WriteLine("AssetCode: {0}", incomingPayment.ReceivedAmount.AssetCode);
    Console.WriteLine("AssetScale: {0}", incomingPayment.ReceivedAmount.AssetScale);
    Console.WriteLine("Value: {0}", incomingPayment.ReceivedAmount.Value);
});

walletAddressCommand.SetAction(async result =>
{
    var address = result.GetRequiredValue(resourceUrlOption);
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

rootCommand.Add(walletAddressCommand);
rootCommand.Add(incomingPaymentCommand);

var config = new CommandLineConfiguration(rootCommand);
return await config.InvokeAsync(args);
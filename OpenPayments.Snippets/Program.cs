using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using OpenPayments.Sdk.Extensions;
using OpenPayments.Snippets.Services.Unauthenticated;

var services = new ServiceCollection();

// let's register OP SDK here
services.UseOpenPayments(opts => opts.UseUnauthenticatedClient());
services.AddTransient<WalletAddressService>();
services.AddTransient<IncomingPaymentService>();

var provider = services.BuildServiceProvider();

Option<string> resourceUrlOption = new("--resource", "-r")
{
    Description = "The URL of the resource",
    Required = true
};

Option<bool> walletAddressKeysOption = new("--keys", "-k")
{
    Description = "If specified, returns only the wallet address keys (JWKS)."
};


var rootCommand = new RootCommand("OpenPayments CLI");
var walletAddressCommand = new Command("WalletAddress")
{
    resourceUrlOption,
    walletAddressKeysOption
};

var incomingPaymentCommand = new Command("IncomingPayment") {
    resourceUrlOption
};

incomingPaymentCommand.SetAction(async result =>
{
    var incomingPaymentUrl = result.GetRequiredValue(resourceUrlOption);
    var incomingPaymentService = provider.GetRequiredService<IncomingPaymentService>();

    await incomingPaymentService.DisplayIncomingPaymentInfoAsync(incomingPaymentUrl);
});

walletAddressCommand.SetAction(async result =>
{
    var keys = result.GetValue(walletAddressKeysOption);
    var address = result.GetRequiredValue(resourceUrlOption);
    var walletService = provider.GetRequiredService<WalletAddressService>();

    if (keys)
    {
        await walletService.DisplayWalletAddressKeysAsync(address);
    }
    else
    {
        await walletService.DisplayWalletInfoAsync(address);
    }
});

rootCommand.Add(walletAddressCommand);
rootCommand.Add(incomingPaymentCommand);

var config = new CommandLineConfiguration(rootCommand);
return await config.InvokeAsync(args);
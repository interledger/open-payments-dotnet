using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using OpenPayments.Sdk.Extensions;
using OpenPayments.Sdk.HttpSignatureUtils;
using OpenPayments.Snippets.Services.Authenticated;
using OpenPayments.Snippets.Services.Unauthenticated;
using IncomingPaymentService = OpenPayments.Snippets.Services.Authenticated.IncomingPaymentService;
using PublicIncomingPaymentService = OpenPayments.Snippets.Services.Unauthenticated.IncomingPaymentService;

var services = new ServiceCollection();

// let's register OP SDK here
services.UseOpenPayments(opts =>
{
    opts.UseUnauthenticatedClient = false;
    opts.KeyId = Environment.GetEnvironmentVariable("CLIENT_ID");
    opts.PrivateKey = KeyUtils.LoadPem(Environment.GetEnvironmentVariable("CLIENT_SECRET")!.Replace("\\n", "\n"));
    opts.ClientUrl = new Uri(Environment.GetEnvironmentVariable("CLIENT_URL")!);
});
services.AddTransient<WalletAddressService>();
services.AddTransient<PublicIncomingPaymentService>();
services.AddTransient<IncomingPaymentService>();
services.AddTransient<QuoteService>();
services.AddTransient<OutgoingPaymentService>();

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

Option<string> senderWalletAddressOption = new("--sender", "-s")
{
    Description = "The wallet address of the sender."
};

Option<string> receiverWalletAddressOption = new("--receiver", "-r")
{
    Description = "The wallet address of the receiver.",
    Required = true
};

Option<string> amountOption = new("--amount", "-a")
{
    Description = "The amount to send. Eg: 1000 (10 Euro)",
    Required = true
};

Option<string> incomingPaymentIdOption = new("--incomingPaymentId", "-i")
{
    Description = "The incoming payment ID.",
    Required = true
};

Option<string> quoteUrlOption = new("--quoteUrl", "-q")
{
    Description = "The URL of the quote defining this payment's amounts.",
    Required = true
};


var rootCommand = new RootCommand("OpenPayments CLI");
var walletAddressCommand = new Command("WalletAddress")
{
    resourceUrlOption,
    walletAddressKeysOption
};

var getIncomingPaymentCommand = new Command("GetIncomingPayment")
{
    resourceUrlOption
};

var createIncomingPaymentCommand = new Command("CreateIncomingPayment")
{
    receiverWalletAddressOption,
    amountOption
};
var createQuoteCommand = new Command("CreateQuote")
{
    senderWalletAddressOption,  
    incomingPaymentIdOption
};
var createOutgoingPaymentCommand = new Command("CreateOutgoingPayment")
{
    senderWalletAddressOption,  
    quoteUrlOption,  
    amountOption
};

getIncomingPaymentCommand.SetAction(async result =>
{
    var incomingPaymentUrl = result.GetRequiredValue(resourceUrlOption);
    var incomingPaymentService = provider.GetRequiredService<PublicIncomingPaymentService>();

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

createIncomingPaymentCommand.SetAction(async result =>
{
    var receiver = result.GetValue(receiverWalletAddressOption)!;
    var amount = result.GetValue(amountOption)!;
    
    var service = provider.GetRequiredService<IncomingPaymentService>();
    await service.CreateIncomingPaymentAsync(receiver, amount);
});

createQuoteCommand.SetAction(async result =>
{
    var incomingPaymentUrl = result.GetValue(incomingPaymentIdOption)!;
    var sender = result.GetValue(senderWalletAddressOption)!;
    
    var service = provider.GetRequiredService<QuoteService>();
    await service.CreateQuoteAsync(sender, incomingPaymentUrl);
});

createOutgoingPaymentCommand.SetAction(async result =>
{
    var sender = result.GetValue(senderWalletAddressOption)!;
    var quoteUrl = result.GetValue(quoteUrlOption)!;
    var debitAmount = result.GetValue(amountOption)!;
    
    var service = provider.GetRequiredService<OutgoingPaymentService>();
    await service.CreateOutgoingPaymentAsync(sender, quoteUrl, debitAmount);
});

rootCommand.SetAction(async result =>
{
    
});

// Unauthenticated
rootCommand.Add(walletAddressCommand);
rootCommand.Add(getIncomingPaymentCommand);

// Authenticated
rootCommand.Add(createIncomingPaymentCommand);
rootCommand.Add(createQuoteCommand);
rootCommand.Add(createOutgoingPaymentCommand);


var config = new CommandLineConfiguration(rootCommand);
return await config.InvokeAsync(args);
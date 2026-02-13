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
    opts.UseAuthenticatedClient = true;
    opts.KeyId = Environment.GetEnvironmentVariable("CLIENT_ID");
    opts.PrivateKey = KeyUtils.LoadPem(Environment.GetEnvironmentVariable("CLIENT_SECRET")!.Replace("\\n", "\n"));
    opts.ClientUrl = new Uri(Environment.GetEnvironmentVariable("CLIENT_URL")!);
});
services.AddTransient<WalletAddressService>();
services.AddTransient<PublicIncomingPaymentService>();
services.AddTransient<IncomingPaymentService>();
services.AddTransient<QuoteService>();
services.AddTransient<OutgoingPaymentService>();
services.AddTransient<TokenService>();

var provider = services.BuildServiceProvider();

// Options
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
};
Option<string> debitAmountOption = new("--debitAmount", "-da")
{
    Description = "The amount to send. Eg: 1000 (10 Euro)",
};
Option<string> receiveAmountOption = new("--receiveAmount", "-ra")
{
    Description = "The amount to send. Eg: 1000 (10 Euro)",
};
Option<string> incomingPaymentIdOption = new("--incomingPaymentId", "-i")
{
    Description = "The incoming payment ID.",
};
Option<string> quoteUrlOption = new("--quoteUrl", "-q")
{
    Description = "The URL of the quote defining this payment's amounts.",
};
Option<string> outgoingUrlOption = new("--outgoingUrl", "-o")
{
    Description = "The URL of the outgoing payment.",
};
Option<string> accessTokenValue = new("--accessToken", "-t")
{
    Description = "The access token to use for authentication.",
};
Option<string> tokenAction = new("--action", "-a")
{
    Description = "The action to perform on the token: 'rotate' or 'revoke'.",
};

// Commands
var rootCommand = new RootCommand("OpenPayments CLI");
var walletAddressCommand = new Command("WalletAddress")
{
    resourceUrlOption,
    walletAddressKeysOption
};
var manageTokenCommand = new Command("ManageToken")
{
    resourceUrlOption,
    accessTokenValue,
    tokenAction
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
var listIncomingPaymentsCommand = new Command("ListIncomingPayments")
{
    receiverWalletAddressOption
};
var createQuoteCommand = new Command("CreateQuote")
{
    senderWalletAddressOption,
    incomingPaymentIdOption,
    debitAmountOption,
    receiveAmountOption
};
var getQuoteCommand = new Command("GetQuote")
{
    senderWalletAddressOption,
    quoteUrlOption
};
var createOutgoingPaymentCommand = new Command("CreateOutgoingPayment")
{
    senderWalletAddressOption,
    quoteUrlOption,
    incomingPaymentIdOption,
    amountOption
};
var getOutgoingPaymentCommand = new Command("GetOutgoingPayment")
{
    senderWalletAddressOption,
    outgoingUrlOption
};
var listOutgoingPaymentsCommand = new Command("ListOutgoingPayments")
{
    senderWalletAddressOption
};

// Actions
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
listIncomingPaymentsCommand.SetAction(async result =>
{
    var receiver = result.GetValue(receiverWalletAddressOption)!;
    var service = provider.GetRequiredService<IncomingPaymentService>();

    await service.ListIncomingPaymentsAsync(receiver);
});
createQuoteCommand.SetAction(async result =>
{
    var sender = result.GetValue(senderWalletAddressOption)!;
    var incomingPaymentUrl = result.GetValue(incomingPaymentIdOption)!;
    var debitAmount = result.GetValue(debitAmountOption);
    var receiveAmount = result.GetValue(receiveAmountOption);
    
    var service = provider.GetRequiredService<QuoteService>();
    await service.CreateQuoteAsync(sender, incomingPaymentUrl, debitAmount, receiveAmount);
});
getQuoteCommand.SetAction(async result =>
{
    var quoteUrl = result.GetValue(quoteUrlOption)!;
    var sender = result.GetValue(senderWalletAddressOption)!;

    var service = provider.GetRequiredService<QuoteService>();
    await service.GetQuoteAsync(sender, quoteUrl);
});
createOutgoingPaymentCommand.SetAction(async result =>
{
    var sender = result.GetValue(senderWalletAddressOption)!;
    var quoteUrl = result.GetValue(quoteUrlOption);
    var iPaymentUrl = result.GetValue(incomingPaymentIdOption);
    var debitAmount = result.GetValue(amountOption)!;
    var service = provider.GetRequiredService<OutgoingPaymentService>();
    
    await service.CreateOutgoingPaymentAsync(sender, debitAmount, quoteUrl, iPaymentUrl);
});
getOutgoingPaymentCommand.SetAction(async result =>
{
    var sender = result.GetValue(senderWalletAddressOption)!;
    var outgoingPaymentUrl = result.GetValue(outgoingUrlOption)!;

    var service = provider.GetRequiredService<OutgoingPaymentService>();
    await service.GetOutgoingPaymentAsync(sender, outgoingPaymentUrl);
});
listOutgoingPaymentsCommand.SetAction(async result =>
{
    var sender = result.GetValue(senderWalletAddressOption)!;

    var service = provider.GetRequiredService<OutgoingPaymentService>();
    await service.ListOutgoingPaymentAsync(sender);
});
manageTokenCommand.SetAction(async result =>
{
    var tokenUrl = result.GetValue(resourceUrlOption)!;
    var accessToken = result.GetValue(accessTokenValue)!;
    var manageAction = result.GetValue(tokenAction);
    var service = provider.GetRequiredService<TokenService>();

    switch (manageAction)
    {
        case "revoke":
            await service.RevokeTokenAsync(tokenUrl, accessToken);
            return;
        case "rotate":
            await service.RotateTokenAsync(tokenUrl, accessToken);
            break;
    }
});

rootCommand.SetAction(_ => { });

// Unauthenticated
rootCommand.Add(walletAddressCommand);
rootCommand.Add(getIncomingPaymentCommand);

// Authenticated
rootCommand.Add(createIncomingPaymentCommand);
rootCommand.Add(listIncomingPaymentsCommand);
//
rootCommand.Add(createQuoteCommand);
rootCommand.Add(getQuoteCommand);
//
rootCommand.Add(createOutgoingPaymentCommand);
rootCommand.Add(getOutgoingPaymentCommand);
rootCommand.Add(listOutgoingPaymentsCommand);

var config = new CommandLineConfiguration(rootCommand);
return await config.InvokeAsync(args);
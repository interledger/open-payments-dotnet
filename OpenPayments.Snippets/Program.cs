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
Option<string> accessTokenValue = new("--accessToken", "-t")
{
    Description = "The access token to use for authentication.",
};
Option<string> tokenAction = new("--action", "-a")
{
    Description = "The action to perform on the token: 'rotate' or 'revoke'.",
};


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
    incomingPaymentIdOption
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
    var quoteUrl = result.GetValue(quoteUrlOption)!;
    var debitAmount = result.GetValue(amountOption)!;

    var service = provider.GetRequiredService<OutgoingPaymentService>();
    await service.CreateOutgoingPaymentAsync(sender, quoteUrl, debitAmount);
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

listIncomingPaymentsCommand.SetAction(async result =>
{
    var receiver = result.GetValue(receiverWalletAddressOption)!;
    var service = provider.GetRequiredService<IncomingPaymentService>();

    await service.ListIncomingPaymentsAsync(receiver);
});

rootCommand.SetAction(async _ =>
{
    var service = provider.GetRequiredService<QuoteService>();
    await service.GetQuoteAsync("https://ilp.interledger-test.dev/cozmin-eur",
        "https://ilp.interledger-test.dev/f537937b-7016-481b-b655-9f0d1014822c/quotes/817b0bf1-12a9-43a8-a6e0-38cb3b05f6c0");
});

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

var config = new CommandLineConfiguration(rootCommand);
return await config.InvokeAsync(args);
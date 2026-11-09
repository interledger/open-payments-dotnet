# Open Payments .NET SDK

.NET SDK for [Open Payments](https://openpayments.dev/), an open API standard implemented by account
servicing entities (banks, digital wallet providers, mobile money providers) to enable interoperable setup
and completion of payments — one-time payments, recurring remittances, tipping, and more.

## Supported frameworks

Targets `net9.0`. For HTTP Message Signatures usable standalone on `net8.0`, see
[`Interledger.OpenPayments.HttpSignatureUtils`](https://www.nuget.org/packages/Interledger.OpenPayments.HttpSignatureUtils),
which this package depends on.

## Install

```bash
dotnet add package Interledger.OpenPayments
```

## Quickstart

Register the SDK with dependency injection, then resolve an `IAuthenticatedClient`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Extensions;
using OpenPayments.Sdk.HttpSignatureUtils;

var client = new ServiceCollection()
    .UseOpenPayments(opts =>
    {
        opts.UseAuthenticatedClient = true;
        opts.KeyId = CLIENT_ID;
        opts.PrivateKey = KeyUtils.LoadPem(CLIENT_SECRET);
        opts.ClientUrl = new Uri(CLIENT_WALLET_ADDRESS);
    })
    .BuildServiceProvider()
    .GetRequiredService<IAuthenticatedClient>();

var walletAddress = await client.GetWalletAddressAsync("https://wallet.example/alice");
```

For complete end-to-end flows — one-time payments, recurring remittances, grant continuation, and more —
see the runnable guides in
[`OpenPayments.Snippets/Guides`](https://github.com/interledger/open-payments-dotnet/tree/main/OpenPayments.Snippets/Guides).

## Error handling

Generated client methods throw `ApiException` — or its generic subtype `ApiException<TResult>` — when the
server returns an unexpected status code. `ApiException` carries the response `StatusCode`, `Response`
body, and `Headers`. A distinct `ApiException` type exists per generated namespace
(`OpenPayments.Sdk.Generated.Auth`, `OpenPayments.Sdk.Generated.Resource`,
`OpenPayments.Sdk.Generated.Wallet`), since each is generated independently from its own OpenAPI spec.

The unauthenticated client's `GetIncomingPaymentAsync` throws `InvalidOperationException` if the server
responds successfully but returns an empty or unparsable body.

A unified exception type that wraps all of the above into a single public type is planned for a future
release.

## Versioning

This package follows [Semantic Versioning](https://semver.org/). See the project's
[GitHub Releases](https://github.com/interledger/open-payments-dotnet/releases) for change history —
release notes are generated automatically from merged pull requests.

## Learn more

Visit [openpayments.dev](https://openpayments.dev/sdk/before-you-begin/) for a detailed guide.

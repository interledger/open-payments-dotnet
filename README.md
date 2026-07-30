# Open Payments .NET SDK

<p align="center">
  <img src="https://raw.githubusercontent.com/interledger/open-payments/main/docs/public/img/logo.svg" width="700" alt="Open Payments">
</p>

[![Status](https://img.shields.io/badge/status-active-success.svg)]()
[![GitHub Issues](https://img.shields.io/github/issues/interledger/open-payments-dotnet.svg)](https://github.com/kylelobo/open-payments-dotnet/issues)
[![GitHub Pull Requests](https://img.shields.io/github/issues-pr/interledger/open-payments-dotnet.svg)](https://github.com/interledger/open-payments-dotnet/pulls)

## What is Open Payments?

Open Payments is an open API standard that can be implemented by account servicing entities (e.g. banks, digital wallet providers, and mobile money providers) to facilitate interoperability in the setup and completion of payments for different use cases including:

- [Web Monetization](https://webmonetization.org)
- Tipping/Donations (low value/low friction)
- eCommerce checkout
- P2P transfers
- Subscriptions
- Invoice Payments

The Open Payments APIs are a collection of three sub-systems:

- A **wallet address server** which exposes public information about Open Payments-enabled accounts called "wallet addresses"
- A **resource server** which exposes APIs for performing functions against the underlying accounts
- A **authorisation server** which exposes APIs compliant with the [GNAP](https://datatracker.ietf.org/doc/html/draft-ietf-gnap-core-protocol) standard for getting grants to access the resource server APIs

This repository contains contains a .NET Open Payments SDK to make requests via the Open Payments API.

### New to Interledger?

Never heard of Interledger before? Or would you like to learn more? Here are some excellent places to start:

- [Interledger Website](https://interledger.org/)
- [Interledger Specification](https://interledger.org/developers/rfcs/interledger-protocol/)
- [Interledger Explainer Video](https://twitter.com/Interledger/status/1567916000074678272)
- [Open Payments](https://openpayments.dev/)
- [Web monetization](https://webmonetization.org/)

## Error handling

Every client method throws a single exception type, `OpenPaymentsApiException`, whenever a request
fails — on any non-2xx response, and on a 2xx whose body is empty or cannot be deserialized.

```csharp
using OpenPayments.Sdk.Exceptions;

try
{
    var quote = await client.CreateQuoteAsync(requestArgs, quoteBody);
}
catch (OpenPaymentsApiException ex) when (ex.StatusCode == 429)
{
    // The server asked us to slow down. RetryAfter is null if it sent no usable hint.
    await Task.Delay(ex.RetryAfter ?? TimeSpan.FromSeconds(5));
}
catch (OpenPaymentsApiException ex)
{
    logger.LogError(
        "Open Payments call failed: {Status} {Code} {Description}. Body: {Body}",
        ex.StatusCode,
        ex.ErrorCode,
        ex.Description,
        ex.ResponseBody
    );
    throw;
}
```

| Property | Type | Notes |
|---|---|---|
| `StatusCode` | `int` | The status the server returned. On a malformed success response this is the 2xx it sent. |
| `ErrorCode` | `string?` | The machine-readable code from the body, e.g. `invalid_request`, `too_fast`, `unauthorized`. |
| `Description` | `string?` | The human-readable description from the body. |
| `ResponseBody` | `string?` | The raw body, verbatim and untruncated. |
| `Headers` | `IReadOnlyDictionary<string, IEnumerable<string>>` | Response headers. Never null. |
| `RetryAfter` | `TimeSpan?` | Parsed from `Retry-After`. Commonly present on 429 and 503. |

`ErrorCode` and `Description` are `null` when the server returns something other than the Open
Payments error shape — an HTML page from a gateway, an empty body, a rate-limit response with no
payload. `ResponseBody` is always the place to look in that case. The SDK never retries on your
behalf; `RetryAfter` is provided so you can.

`OpenPaymentsApiException` covers every failure the *server* reports. Transport-level failures
before a response arrives (DNS, connection, TLS, timeout) still surface as `HttpRequestException` /
`TaskCanceledException` from `HttpClient`.

> **Breaking change.** `OpenPaymentsApiException` replaces the generated `ApiException` and
> `ApiException<ErrorResponse>` types, the `HttpRequestException` that `EnsureSuccessStatusCode` used
> to throw on non-2xx responses, and the `InvalidOperationException` that unauthenticated calls used
> to throw. None of those escape client methods any more — transport-level failures (see above) are
> a separate case and still propagate as-is.

## Contributing

Please read the [contribution guidelines](.github/contributing.md) before submitting contributions. All contributions must adhere to our [code of conduct](.github/code_of_conduct.md).

## Open Payments Catchup Call

Our catchup calls are open to our community. We have them every other Wednesday at 13:00 GMT, via Google Meet.

Video call link: https://meet.google.com/htd-eefo-ovn

Or dial: (DE) +49 30 300195061 and enter this PIN: 105 520 503#

More phone numbers: https://tel.meet/htd-eefo-ovn?hs=5

[Add to Google Calendar](https://calendar.google.com/calendar/event?action=TEMPLATE&tmeid=MDNjYTdhYmE5MTgwNGJhMmIxYmU0YWFkMzI2NTFmMjVfMjAyNDA1MDhUMTIwMDAwWiBjX2NqMDI3Z21oc3VqazkxZXZpMjRkOXB2bXQ0QGc&tmsrc=c_cj027gmhsujk91evi24d9pvmt4%40group.calendar.google.com&scp=ALL)

## Local Development Environment

This repository contains a Git submodule, which contains the Open Payments OpenAPI specifications.
After cloning, make sure to initialize and update it:

```bash
git submodule update --init
```

Alternatively, clone the repository with submodules in one step:

```bash
git clone --recurse-submodules git@github.com:interledger/open-payments-node.git
```

### Prerequisites

- [.NET 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

### Environment Setup

Generated DTOs are committed, so you can build and test with nothing but the
.NET SDK. Regenerating them (only needed when the OpenAPI specs change)
requires the pinned NSwag CLI:

```bash
dotnet tool install --global NSwag.ConsoleCore --version 14.6.2
```

```bash
make models
```

CI regenerates with the same pinned version and fails on any diff against the
committed output.

## 🔧 Running the tests

```bash
dotnet test
```

## 🎈 Usage

To use in your project, just add the package using the command line

```bash
dotnet add package Interledger.OpenPayments
```

Then add it to your project code

```csharp
// Import dependencies
using Microsoft.Extensions.DependencyInjection;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Extensions;
using OpenPayments.Sdk.HttpSignatureUtils;

// Initialize client
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
```

Or bind the options from configuration instead of a delegate:

```csharp
// Import dependencies
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Extensions;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

var client = new ServiceCollection()
    .UseOpenPayments(configuration.GetSection("OpenPayments"))
    .BuildServiceProvider()
    .GetRequiredService<IAuthenticatedClient>();
```

```json
{
  "OpenPayments": {
    "UseAuthenticatedClient": true,
    "KeyId": "your-key-id",
    "ClientUrl": "https://wallet.example",
    "PrivateKeyPath": "/run/secrets/private-key.pem"
  }
}
```

Exactly one signing key source must be set. `PrivateKey` takes an `NSec.Cryptography.Key` and is
only reachable from the delegate overload; `PrivateKeyPem` takes PEM-encoded PKCS#8 text, and
`PrivateKeyPath` takes a path to a PEM or raw key file. Options are validated when
`UseOpenPayments` is called, so a missing or ambiguous setting fails at startup rather than on the
first request.

Please visit [OpenPayments Docs](https://openpayments.dev/sdk/before-you-begin/) for a detailed guide.

## ✍️ Authors

- [@golobitch](https://github.com/golobitch) - Initial work
- [@cozminu](https://github.com/cozminu) - Maintainer

See also the list of [contributors](https://github.com/interledger/open-payments-dotnet/contributors) who participated in this project.

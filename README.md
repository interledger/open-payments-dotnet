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

- [NVM](https://github.com/nvm-sh/nvm)
- [.NET 9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

### Environment Setup

```bash
npm install -g swagger-cli && \
dotnet tool install --global NSwag.ConsoleCore
```

Now generate models from the OpenAPI specs. You can generate all of them by running the command below:

```bash
make models
```

However, you can generate them one by one (please check the `Makefile` for all supported commands).

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

Please visit [OpenPayments Docs](https://openpayments.dev/sdk/before-you-begin/) for a detailed guide.

## ✍️ Authors

- [@golobitch](https://github.com/golobitch) - Initial work
- [@cozminu](https://github.com/cozminu) - Maintainer

See also the list of [contributors](https://github.com/interledger/open-payments-dotnet/contributors) who participated in this project.

# Open Payments .NET HTTP Signature Utils

Standalone utilities implementing [HTTP Message Signatures (RFC 9421)](https://datatracker.ietf.org/doc/html/rfc9421)
with the Ed25519 algorithm, as required by the [Open Payments](https://openpayments.dev/) authentication
scheme. Usable on its own, independent of the rest of the Open Payments .NET SDK.

## Supported frameworks

Targets `net8.0`. This is independent of `Interledger.OpenPayments` (which targets `net9.0` and depends on
this package) — you can use this package standalone on an older runtime.

## Install

```bash
dotnet add package Interledger.OpenPayments.HttpSignatureUtils
```

## Signing a request

```csharp
using System.Text;
using OpenPayments.Sdk.HttpSignatureUtils;

var privateKey = KeyUtils.LoadPem(privateKeyPem);
var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/incoming-payments")
{
    Content = new StringContent("{\"walletAddress\":\"https://example.com/alice\"}", Encoding.UTF8, "application/json"),
};

var headers = await HttpRequestSigner.SignHttpRequestAsync(request, privateKey, keyId);
request.Headers.TryAddWithoutValidation("Signature", headers.Signature);
request.Headers.TryAddWithoutValidation("Signature-Input", headers.SignatureInput);
```

## Verifying a request

```csharp
using OpenPayments.Sdk.HttpSignatureUtils;

var validator = new HttpSignatureValidator(
    new SignatureInputParser(),
    new SignatureInputValidator(),
    new SignatureInputBuilder());

var clientKey = new Jwk { Kid = keyId, X = base64UrlEncodedPublicKey };

if (validator.AreSignatureHeadersPresent(incomingRequest))
{
    bool isValid = await validator.ValidateSignatureAsync(incomingRequest, clientKey);
}
```

## Versioning

This package follows [Semantic Versioning](https://semver.org/). See the project's
[GitHub Releases](https://github.com/interledger/open-payments-dotnet/releases) for change history —
release notes are generated automatically from merged pull requests.

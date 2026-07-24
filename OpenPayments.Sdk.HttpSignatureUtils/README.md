# Interledger.OpenPayments.HttpSignatureUtils

HTTP Message Signatures (RFC 9421) utilities for the [Open Payments](https://openpayments.dev/) APIs:

- Ed25519 key generation, loading (raw, Base64, PKCS#8 PEM), and persistence — `KeyUtils`
- JWK (`kty: OKP`, `crv: Ed25519`) export for publishing client keys — `KeyUtils.GenerateJwk`
- Request signing (`Signature` / `Signature-Input` headers) — `HttpRequestSigner`, `SigningHttpMessageHandler`
- Signature validation for servers and tests — `HttpSignatureValidator`

This package is consumed by [`Interledger.OpenPayments`](https://www.nuget.org/packages/Interledger.OpenPayments),
the Open Payments .NET SDK — install that package instead if you want the full client. Install this package
directly when you only need key management or HTTP signature primitives.

```csharp
using Interledger.OpenPayments.HttpSignatureUtils;

var key = KeyUtils.LoadOrGenerateKey("private-key.pem");
var jwk = KeyUtils.GenerateJwk("my-key-id", key);
```

Source, issues, and license (Apache-2.0): https://github.com/interledger/open-payments-dotnet

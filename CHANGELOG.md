# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- **BREAKING:** The low-level server clients are no longer NSwag-generated. They moved namespace
  and changed shape:
  - `OpenPayments.Sdk.Generated.Auth.AuthServerClient` is now `OpenPayments.Sdk.Http.AuthServerClient`.
  - `OpenPayments.Sdk.Generated.Resource.ResourceServerClient` is now
    `OpenPayments.Sdk.Http.ResourceServerClient`.
  - `OpenPayments.Sdk.Generated.Wallet.WalletAddressClient` is now
    `OpenPayments.Sdk.Http.WalletAddressClient`.
  - `AddSigningKey` and the `ClientUrl` property are gone from the auth and resource clients.
    Signing now happens in the `HttpClient` pipeline via `SigningHttpMessageHandler`.
  - Methods take the target URL as a parameter (`CreateGrantAsync(authServerUrl, ...)`,
    `GetQuoteAsync(quoteUrl, ...)`, `PostIncomingPaymentAsync(resourceServerUrl, ...)` and so on)
    instead of reading it from a mutable `BaseUrl`.
  - The model types (`GrantCreateBody`, `QuoteResponse`, etc.) keep their
    `OpenPayments.Sdk.Generated.*` namespaces.
- **BREAKING:** Client methods throw `OpenPayments.Sdk.Exceptions.OpenPaymentsApiException` when
  the server reports a failure. The generated `ApiException` / `ApiException<TResult>` types in
  `OpenPayments.Sdk.Generated.Auth`, `.Resource` and `.Wallet` are removed. Catch blocks need to
  switch to `OpenPaymentsApiException`, which exposes `StatusCode`, `ErrorCode`, `Description`,
  `ResponseBody`, `Headers` and `RetryAfter`. Transport failures still surface as
  `HttpRequestException` / `TaskCanceledException`.
- **BREAKING:** `AuthClientBase` and `ResourceClientBase` constructors no longer take a signing
  key, key id or (for `ResourceClientBase`) client URL. They expect an `HttpClient` whose pipeline
  already signs requests: `AuthClientBase(HttpClient, Uri clientUrl)` and
  `ResourceClientBase(HttpClient)`.
- `UseOpenPayments` registers `IAuthenticatedClient` and `IUnauthenticatedClient` as transient
  typed `HttpClient` clients instead of singletons. Missing or invalid options (`KeyId`,
  `ClientUrl`, signing key) now throw at registration time instead of on first resolve, and
  `ClientUrl` must be absolute.
- **BREAKING:** `IAuthenticatedClient.CompleteIncomingPaymentsAsync` is renamed to
  `CompleteIncomingPaymentAsync`. The method completes a single incoming payment, and the
  resource layer already used the singular name. Callers must rename the call.
- **BREAKING:** `IResourceClientBase.ListOutgoingPaymentAsync` and
  `ResourceClientBase.ListOutgoingPaymentAsync` are renamed to `ListOutgoingPaymentsAsync`.
  The method lists many payments, and the authenticated layer already used the plural name.
  Callers using `ResourceClientBase` directly must rename the call;
  `AuthenticatedClient.ListOutgoingPaymentsAsync` is unaffected.

### Added

- The public API surface of both packages is now tracked in committed
  `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` baselines and enforced at build time.
  An undeclared change to public API fails the build.
- `OpenPaymentsOptions.PrivateKeyPem` and `OpenPaymentsOptions.PrivateKeyPath` as alternatives to
  `PrivateKey`.
- `UseOpenPayments(IConfiguration)` overload that binds `OpenPaymentsOptions` from configuration.
- `OpenPayments.Sdk.HttpSignatureUtils.SigningHttpMessageHandler`, a `DelegatingHandler` that
  signs outgoing requests.
- `OpenPayments.Sdk.Http.OpenPaymentsClientBase`, the shared request and error-mapping base for the
  server clients.

### Fixed

- `GetWalletAddressKeysAsync` built `.../alicejwks.json` when the wallet address had no trailing
  slash. It now joins with exactly one `/`.
- Clients no longer mutate a shared `BaseUrl` per call, so concurrent requests to different hosts
  through one client don't race.
- `HttpSignatureValidator` returns `false` instead of throwing on a malformed or missing
  `Signature` header, and `SignatureInputParser` returns `null` instead of throwing on malformed
  `Signature-Input`.
- Signature components with uppercase names are rejected, as RFC 9421 requires.

### Security

- `HttpSignatureValidator` now checks `Content-Digest` against the actual request body, and
  rejects a request with a non-empty body that the signature doesn't cover. Before, a signed
  request's body could be swapped without failing validation.

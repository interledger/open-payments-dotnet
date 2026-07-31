# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

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

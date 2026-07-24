# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
Versions are derived from git tags (`vX.Y.Z`) via MinVer.

## [Unreleased]

### Added
- `IAsyncEnumerable` auto-paging: `ListIncomingPaymentsAllAsync` / `ListOutgoingPaymentsAllAsync` on both the authenticated client and the resource client follow `pageInfo` cursors automatically (rejecting backward-paging queries and repeated server cursors).

### Changed
- **Breaking:** all namespaces renamed from `OpenPayments.Sdk.*` to `Interledger.OpenPayments.*`, matching the package ID.
- **Breaking:** all client errors now throw a single `OpenPaymentsApiException` (status code, error code, raw body) instead of per-namespace `ApiException` types.
- **Breaking:** `CompleteIncomingPaymentsAsync` → `CompleteIncomingPaymentAsync`; `ListOutgoingPaymentAsync` → `ListOutgoingPaymentsAsync`.
- Request signing moved to an async `SigningHttpMessageHandler` on the HTTP pipeline (no more sync-over-async blocking).
- NSwag now generates DTOs only; HTTP plumbing is hand-owned and shared.
- Packages multi-target `net8.0;net9.0`, ship SourceLink + snupkg symbols, and version from git tags (MinVer).
- `Interledger.OpenPayments.HttpSignatureUtils` graduated from placeholder package metadata to full NuGet metadata (description, license, README, repository URL, icon, authors).

### Fixed
- Thread-safety: concurrent requests through a singleton client no longer race on a shared `BaseUrl`.
- Eager validation of `OpenPaymentsOptions` at registration time with clear messages.
- `HttpSignatureValidator` now builds the same signature base as the signer (uppercase `@method`, LF separators, SHA-512 content-digest fallback), so requests signed by `HttpRequestSigner` validate successfully.

### Removed
- Dependencies `Portable.BouncyCastle` (replaced by `PemEncoding` + minimal PKCS#8 handling) and `Sodium.Core` (unused).

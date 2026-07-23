# Open Payments .NET SDK — 10 Improvements for an A-Grade Library

## Context

`Interledger.OpenPayments` is a .NET SDK for the Open Payments spec (OpenAPI submodule at `open-payments-specifications/`). Types are generated via NSwag (`Makefile`), but endpoint methods were hand-written in partial-class files next to the generated code. The library works, is tested (53 unit tests), and publishes to NuGet on tag — but has several correctness, architecture, and packaging gaps that separate it from an A-grade library. The improvements below are ordered by impact: correctness bugs first, then architecture, then polish.

## The 10 Improvements

### 1. Fix thread-safety: singleton clients mutate shared `BaseUrl` per call (correctness bug)
`AuthenticatedClient` is registered as a singleton but its inner generated clients have `BaseUrl` reassigned on every call (`OpenPayments.Sdk/Clients/ResourceClientBase.cs:25,35,45…`, `AuthClientBase.cs:26`). Two concurrent requests to different wallets race and can send a request to the wrong server. Fix: stop using the mutable `BaseUrl` property — build the absolute request URI per call (the hand-written methods already construct `HttpRequestMessage` themselves, so pass the full URL through instead of mutating shared state).

### 2. Replace the sync-over-async signing hook with an async `DelegatingHandler`
`PrepareRequest` blocks on `.Result` for every signed request (`Generated/Auth/AuthServerClient.Auth.cs:28`, `Generated/Resource/ResourceServerClient.Auth.cs:30`) — thread-pool starvation/deadlock risk on every call. NSwag's `PrepareRequest` can't be async, so move signing into a `SigningHttpMessageHandler : DelegatingHandler` registered on the named HttpClient pipeline via `IHttpClientFactory`. This makes signing fully async, removes the partial-class hack, fixes the Auth-vs-Resource asymmetry (one silently skips signing, the other throws), and fixes the double body read in `HttpSignatureUtils/HttpRequestSigner.cs`.

### 3. Unify error handling into one first-class exception model
Today there are two contracts: most methods throw generated `ApiException`/`ApiException<ErrorResponse>` (a *different type per generated namespace*), while `UnauthenticatedClient.GetIncomingPaymentAsync` uses `EnsureSuccessStatusCode()` + `InvalidOperationException`. Status coverage is inconsistent per endpoint (no 429 handling anywhere). Fix: introduce a single public `OpenPaymentsApiException` (status code, error code/description, raw body) in the SDK namespace, map every non-2xx to it via one shared response-processing helper, and remove the per-method copy-pasted status `switch` blocks (`Generated/*/*.Methods.*.cs`).

### 4. Generate types only — delete thousands of lines of dead generated client code
The NSwag endpoint methods in the `.g.cs` files (`PostRequestAsync`, `PostTokenAsync`, placeholder DTOs `Body2`, `Response3`…) are dead code re-implemented by hand, yet the hand-written methods still depend on generated *infrastructure* (`ReadObjectResponseAsync`, `ApiException`, serializer settings). Fix the Makefile to generate DTOs/contracts only (`/GenerateClientClasses:false`), apply consistent flags to all three specs (`/GenerateNullableReferenceTypes:true` is currently resource-server-only), and move the small shared HTTP plumbing into one hand-owned helper. Also removes the hard-coded test URLs baked into generated ctors (`BaseUrl = "https://ilp.interledger-test.dev"` in `ResourceServerClient.g.cs:44`, `WalletAddressClient.g.cs:42`). Decide one stance on committing generated code (currently committed *and* regenerated in CI — drift risk).

### 5. Align namespaces with the package identity
The package is `Interledger.OpenPayments` (with `RootNamespace=Interledger.OpenPayments`), but every file uses explicit `namespace OpenPayments.Sdk.*`. Consumers install `Interledger.OpenPayments` and must guess `using OpenPayments.Sdk.Extensions;`. Rename namespaces to `Interledger.OpenPayments.*` (breaking change — do it before adoption grows; the package is pre-1.0 in practice).

### 6. Harden DI registration and options validation
In `Extensions/ServiceCollectionExtensions.cs:33`, the unauthenticated singleton captures a factory-created default `HttpClient` for the app lifetime, defeating handler rotation (stale DNS); the authenticated branch uses `CreateClient("authenticated")` but nothing ever configures that named client. Fix: use typed/named clients consistently (`AddHttpClient<T>` with the signing handler from #2), validate `OpenPaymentsOptions` eagerly with clear messages (or `IValidateOptions`), and support the standard `IConfiguration` binding pattern. Fix `ClientUrl { get; set; }` non-nullable-uninitialized gaps in the `*.Auth.cs` partials.

### 7. A-grade packaging: `Directory.Build.props`, SourceLink, symbols, analyzers, one version source
Create `Directory.Build.props` with: `TreatWarningsAsErrors`, SourceLink (`Microsoft.SourceLink.GitHub`, `PublishRepositoryUrl`, `EmbedUntrackedSources`, `ContinuousIntegrationBuild`), `IncludeSymbols` + `SymbolPackageFormat=snupkg`, .NET analyzers + `Microsoft.CodeAnalysis.PublicApiAnalyzers`, shared package metadata. Fix `OpenPayments.Sdk.HttpSignatureUtils.csproj`: it ships to nuget.org with **no** description/license/readme/repo URL, `Authors` set to the package name, and an `InternalsVisibleTo` containing a file path instead of an assembly name. Unify TFMs (SDK is net9.0, HttpSignatureUtils net8.0 — multi-target both to `net8.0;net9.0`), pin `global.json`, adopt central package management (`Directory.Packages.props`), and make the git tag the single version source (MinVer) instead of tag + hardcoded `<Version>1.0.0</Version>`. Audit HttpSignatureUtils deps: it pulls three crypto stacks (NSec + Sodium.Core + Portable.BouncyCastle — the latter is unmaintained).

### 8. First-class pagination: `IAsyncEnumerable` auto-paging
List endpoints return raw `ListIncomingPaymentsResponse` with a cursor the caller must loop manually. Add `IAsyncEnumerable<IncomingPayment>`-returning overloads (e.g. `ListIncomingPaymentsAllAsync`) that follow `pageInfo` cursors automatically, alongside the existing page-at-a-time methods. This is the single biggest day-to-day ergonomics win for consumers.

### 9. Make serialization consistent (and plan the System.Text.Json migration)
`AuthContractResolver` relaxes `Required.Always` and ignores nulls while `ResourceContractResolver` is an empty pass-through — so resource-server responses can throw on spec-vs-server drift that auth responses tolerate. Short term: unify resolver behavior. Longer term: move generation and hand-written code to System.Text.Json (source-generated contexts) — drops the Newtonsoft dependency and enables trimming/AOT support, which modern .NET consumers increasingly expect.

### 10. Polish docs, tests, and CI to release quality
- README: Issues badge points at `kylelobo/open-payments-dotnet`, clone example points at the **node** repo, the second published package is never documented; add versioning/supported-frameworks/changelog sections.
- XML docs: drop `NoWarn=1591` and document the public surface (`OpenPaymentsOptions` members, `AuthClientBase`/`ResourceClientBase`, missing `<inheritdoc/>`); fix malformed comment at `Configuration/OpenPaymentsOptions.cs:12`.
- Tests: align xUnit versions between the two test projects (2.9.2 vs 2.4.2), rename `ServiceCollecitonExtenions_Tests.cs` (typo), add a concurrency test for #1 and a paging test for #8; fix naming inconsistencies (`CompleteIncomingPaymentsAsync` vs `CompleteIncomingPaymentAsync`, `ListOutgoingPaymentAsync` singular in `ResourceClientBase.cs:110`).
- CI: cache NuGet/npm, remove reference to nonexistent `dotnet.yml`, add coverage threshold; release workflow: push `.snupkg`, `--skip-duplicate`, dedupe step names, keep a committed `CHANGELOG.md`.

## Suggested execution order

1. Bugs/architecture first: #1, #2, #3, #6 (these change internals, not the public surface much).
2. Breaking-change window while pre-adoption: #5 (namespaces), #4 (generation strategy).
3. Infrastructure: #7, #10.
4. Features: #8, #9.

## Verification

- `dotnet build -warnaserror && dotnet test` green after each improvement.
- For #1: a unit test firing parallel requests at two mocked base URLs through one singleton client, asserting each request hit the right host.
- For #2: assert no `.Result`/`.Wait()` remains (`grep -rn "\.Result" OpenPayments.Sdk`), and signing tests still pass.
- For #7: `dotnet pack` and inspect the nupkg (metadata, README, snupkg present); validate with NuGet Package Explorer or `dotnet-validate`.
- End-to-end: run `OpenPayments.Snippets` guides against the Interledger test wallet.

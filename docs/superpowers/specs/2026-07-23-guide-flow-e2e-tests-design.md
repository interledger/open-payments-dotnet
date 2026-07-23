# Guide-Flow End-to-End Test Suite

## Goal

`OpenPayments.Snippets/Guides/` contains eight example programs that demonstrate
full Open Payments flows using the SDK. They are documentation as much as code:
developers copy them, and `docs/openpayments.dev` links to them. Nothing today
verifies they still compile against the SDK's current API or still produce a
protocol-correct sequence of calls, so they can silently rot as the SDK
evolves. `docs/IMPROVEMENTS.md` already flags this as a gap ("End-to-end: run
`OpenPayments.Snippets` guides against the Interledger test wallet").

This design covers stage 1: a fast, CI-friendly suite that runs the real
`Guide` classes against an in-process fake Open Payments server and asserts on
the resulting state. It catches guide/SDK drift on every PR without live
network calls or flakiness.

**Explicitly out of scope for this stage:** validating against the real,
hosted Interledger test wallet, including real interactive-grant consent
(which needs a headless-browser layer to drive the test wallet's UI). That
remains a separate, later effort, tracked as stage 2.

## Why an in-process fake server, not stub-per-request mocking

The guides chain calls where each response feeds the next request (an
incoming payment's `Id` becomes a quote's `Receiver`, a quote's `Id` becomes an
outgoing payment's `QuoteId`, etc.), and every guide but one drives an
interactive grant that needs something to auto-approve it since there's no
real user.

A canned request/response stub approach (e.g. WireMock.Net with fixed bodies)
would require keeping every stub's body in sync with whatever IDs the guide
generates, and breaks on minor reordering or field changes in the guide code.
Reusing the existing `Moq`-on-`HttpMessageHandler` pattern from
`OpenPayments.Sdk.Tests` has the same problem at a worse scale — eight
multi-step guides means eight long, brittle, ordered mock sequences.

Instead, the suite runs a small, real, **stateful** server in-process
(`WebApplicationFactory`/`TestServer` with a minimal API) that actually
implements the six operations the guides use. It creates real linked
resources and IDs, the way a real Open Payments deployment would, so guide
code runs completely unmodified.

## Architecture

Each test:

1. Spins up a fresh instance of the fake server (`WebApplicationFactory` +
   in-memory `TestServer`). Fresh per test — no shared fixture — so resource
   and grant state never leaks across tests.
2. Builds an `IAuthenticatedClient` through the SDK's normal
   `UseOpenPayments()` DI registration, with one addition: a
   `TestServerRoutingHandler : DelegatingHandler` inserted into the HTTP
   pipeline that rewrites every outgoing request's scheme/host/port to the
   `TestServer`'s in-memory client, leaving path and query untouched. This is
   what lets guide code run unmodified — the guides hard-code URLs like
   `https://cloudninebank.example.com/customer`, but nothing ever hits real
   DNS. The fake server doesn't need to distinguish "banks" by host; every
   guide-visible resource (wallet lookups, grants, payments, quotes) is
   already unique by path (`/customer` vs `/sender`, `/incoming-payments/{id}`,
   `/quotes/{id}`, …), so one backend transparently serves both simulated
   parties in a guide.
3. Constructs the real `Guide` class (e.g. `new OneTimePayment(client)`) and
   calls `Run()` — no reimplementation of guide logic. The test fails exactly
   when the guide stops matching the SDK's current API or the protocol's
   expected shapes.
4. Asserts on the fake server's observed state after the run (e.g. "an
   outgoing payment was created with the expected debit amount, linked to the
   expected quote") — not merely that `Run()` didn't throw. A guide that
   short-circuits or silently no-ops should still fail.

## Fake server behavior

A single in-memory backend implements the six operations the guides actually
use (`GetWalletAddress`, `RequestGrant`, `ContinueGrant`,
`CreateIncomingPayment`, `CreateQuote`, `CreateOutgoingPayment`), keyed by
GUID-based resource paths, reset fresh per test:

- **`GetWalletAddress`** — returns a canned `WalletAddressResponse` per
  requested path, with `AuthServer`/`ResourceServer` pointing back at the same
  fake server.
- **`RequestGrant`** — non-interactive access (Incoming, Quote grants) issues
  an `AccessToken` immediately. Interactive access (Outgoing grants with
  `Interact`) returns a `Continue` object with a continuation URI and *no*
  top-level `AccessToken`, matching what the guides expect
  (`if (...AccessToken == null) throw`).
- **`ContinueGrant`** — auto-approves any `interactRef` (the guides only ever
  pass a locally generated GUID, never a real one) and returns a real
  `AccessToken`, simulating completed user consent.
- **`CreateIncomingPayment` / `CreateQuote` / `CreateOutgoingPayment`** —
  validate the request body has the fields the protocol requires (wallet
  address, amounts/receiver references), store the resource, and return a
  response with a freshly generated `Id` for later steps to reference.
- Every authenticated call checks the bearer token against one this fake
  server actually issued and returns 401 otherwise — this catches a guide
  accidentally reusing a stale or wrong-scoped token, a realistic guide-rot
  failure mode beyond "did it throw."

## Test structure and project layout

New project `OpenPayments.Snippets.Tests`:

- References `OpenPayments.Snippets` (to run the real `Guide` classes) and
  `OpenPayments.Sdk`.
- Packages: `xunit`, `xunit.runner.visualstudio`, `FluentAssertions`,
  `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.NET.Test.Sdk` — matching
  versions used in `OpenPayments.Sdk.Tests` where applicable.
- `Infrastructure/FakeOpenPaymentsServer.cs` and
  `Infrastructure/TestServerRoutingHandler.cs` hold the shared fake server and
  routing handler, reused by every guide test. Each test builds its own fresh
  instance rather than sharing a fixture — the existing `*_Fixture`/
  `*_Collection` pattern in `OpenPayments.Sdk.Tests` is for sharing
  *expensive, stateless* setup, but here the state itself is what's under
  test, so isolation between tests wins over the small startup cost of an
  in-memory minimal API.
- One test class per guide, named to match and mirroring the `Guides/`
  folder: `OneTimePayment_Tests.cs`, `SendRemittanceWithFixedDebit_Tests.cs`,
  `SendRemittanceWithFixedReceive_Tests.cs`,
  `SetupRecurringRemittanceWithFixedIncoming_Tests.cs`,
  `SendRecurringRemittanceWithFixedDebit_Tests.cs`,
  `SendRecurringRemittanceWithFixedReceive_Tests.cs`,
  `SplitIncomingPayment_Tests.cs`, `GetGrantForFuturePayments_Tests.cs`.
- Each starts with a single happy-path test running the guide's `Run()`
  end-to-end and asserting on final server state. That's sufficient for
  guide-rot detection, since the guides themselves only ever exercise happy
  paths — there's no guide behavior to regress-test beyond "the documented
  flow still works."

## CI

No new CI wiring needed. The repo already runs `dotnet test` at the solution
level (`README.md`), so this project is picked up automatically once added to
`OpenPayments.sln`. No live network calls, so it's exactly as fast and
reliable as the existing unit tests.

## Future work (stage 2, not in this design)

Live validation against the real, hosted Interledger test wallet — including
driving actual interactive-grant consent through a headless browser — as
originally suggested in `docs/IMPROVEMENTS.md`. That's a materially different
effort (external dependency, network flakiness, real credentials/wallets to
provision) and deserves its own design when prioritized.

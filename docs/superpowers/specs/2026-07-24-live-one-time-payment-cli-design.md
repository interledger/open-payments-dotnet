# Live One-Time-Payment CLI Command

## Goal

`OpenPayments.Snippets/Guides/1_OneTimePayment.cs` documents the full
Open Payments one-time-payment flow, but steps 7/8 (starting and finishing the
customer's interactive grant) are stubbed with comments and a fake
`interactRef` — it can't actually run against a live Open Payments deployment.
`OpenPayments.Snippets/Program.cs` already exposes a `System.CommandLine`-based
CLI over individual operations (wallet lookup, incoming payment, quote,
outgoing payment) but has no command that chains them into one end-to-end
transaction.

This adds a `OneTimePayment` command that runs the whole flow against real
endpoints (e.g. the Interledger test network), including real interactive
consent, so it can be exercised from the terminal. This is the live-network
counterpart to the in-process guide tests added in
`2026-07-23-guide-flow-e2e-tests-design.md`, which explicitly scoped real
interactive-grant consent out as "stage 2" — this is that stage 2, scoped to
manual CLI use rather than CI.

## Flow

Implemented in a new `OneTimePaymentService` (in
`OpenPayments.Snippets/Services/Authenticated/`), using the existing
`IAuthenticatedClient`:

1. Resolve the sender (customer) and receiver (retailer) wallet addresses.
2. Request a non-interactive incoming-payment grant on the receiver's AS,
   then create the incoming payment. The amount is expressed in the
   **receiver's own asset code/scale** (looked up from its wallet address),
   matching the existing `IncomingPaymentService.CreateIncomingPaymentAsync`
   pattern — not hardcoded to a currency like the guide.
3. Request a non-interactive quote grant on the sender's AS, then create the
   quote for that incoming payment.
4. Start a local HTTP listener (`GrantInteractionListener`, below) on
   `http://localhost:{port}/callback`.
5. Request an **interactive** outgoing-payment grant with
   `Interact.Finish = { Method = Redirect, Uri = <callback>, Nonce = <generated> }`.
   The grant's `DebitAmount` limit is the **quote's debit amount**
   (`customerQuote.DebitAmount`), not the incoming amount — the two wallets
   may use different currencies, and the quote is the source of truth for
   what the sender will actually be debited.
6. Print the `Interact.Redirect` URL and attempt to open it in the system's
   default browser (`Process.Start` with `UseShellExecute = true`); print the
   URL either way so the flow works headlessly too.
7. Await the callback via `GrantInteractionListener` (5 minute timeout).
8. Verify the GNAP interaction hash from the callback's `hash` query
   parameter against one computed from the client nonce, the AS's
   `Interact.Finish` nonce, `interact_ref`, and the grant request URI, per the
   GNAP interaction-hash algorithm. Mismatches are **logged as a warning, not
   fatal** — some test-network ASes are known to be lenient here, and hard
   failing would make the command unusable against them for a demo/testing
   tool.
9. Continue the grant with the extracted `interact_ref`, then create the
   outgoing payment from the quote (`OutgoingPaymentBodyFromQuote`).
10. Print a summary at each stage (`===Section===` blocks), matching the
    existing service console style.

## `GrantInteractionListener`

A small new helper (`OpenPayments.Snippets/Services/GrantInteractionListener.cs`
— not under `Authenticated/`/`Unauthenticated/` since it's local transport
infrastructure, not an Open Payments resource client) wrapping
`System.Net.HttpListener`:

- `StartAsync(int port)` binds `http://localhost:{port}/callback/`.
- `WaitForCallbackAsync(TimeSpan timeout)` awaits exactly one GET request,
  parses its query string (`interact_ref`, `hash`), responds with a small
  static HTML page ("You can close this window and return to the terminal."),
  then stops listening. Throws on timeout.
- Disposable; the service wraps its use in a `using` block so the listener is
  always torn down, even if the flow throws.

## CLI surface

New `OneTimePayment` command added to `Program.cs`'s existing `RootCommand`,
reusing existing option instances where possible:

- `--sender/-s` (`senderWalletAddressOption`, required) — customer wallet
  address.
- `--receiver/-r` (`receiverWalletAddressOption`, required) — retailer wallet
  address.
- `--amount/-a` (`amountOption`, required) — incoming payment value, in the
  receiver's asset's minor units (e.g. `140000` for 1,400.00 in a scale-2
  currency).
- `--callbackPort` (new `Option<int>`, default `3300`) — local port for the
  redirect listener; overridable in case of a port conflict.

Registered in DI alongside the other services (`services.AddTransient<OneTimePaymentService>()`).

## Error handling

No new error-handling abstraction — consistent with every other command in
`Program.cs` today, exceptions (missing grants, listener timeout, SDK HTTP
failures) propagate uncaught and crash the CLI with the exception message.
The hash-mismatch case is the one deliberate exception to "fail loud": it's
logged and the flow continues, per step 8 above.

## Out of scope

- No changes to the `Guides/` sample files themselves — this is a new,
  separate CLI command, not a rewrite of the documentation guide.
- No automated test coverage requiring a live network (inherently manual/live
  by design). `GrantInteractionListener` is simple enough to unit test in
  isolation (bind, send a real HTTP GET with query params, assert the parsed
  result) without touching any Open Payments endpoint, and that unit test is
  in scope.

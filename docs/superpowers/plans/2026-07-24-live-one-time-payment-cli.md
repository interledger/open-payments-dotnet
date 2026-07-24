# Live One-Time-Payment CLI Command Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `OneTimePayment` CLI command to `OpenPayments.Snippets` that runs the full Open Payments one-time-payment flow against a live network, including real interactive GNAP consent via a local HTTP callback listener.

**Architecture:** A new `GrantInteractionListener` (`Services/GrantInteractionListener.cs`) wraps `HttpListener` to catch the GNAP redirect callback. A new `GnapInteractionHash` (`Services/GnapInteractionHash.cs`) computes the GNAP interaction hash as a pure, unit-testable function. A new `OneTimePaymentService` (`Services/Authenticated/OneTimePaymentService.cs`) chains wallet lookup → incoming payment → quote → interactive outgoing-payment grant → callback wait → hash check → grant continuation → outgoing payment, reusing `IAuthenticatedClient` exactly as the existing services and the `Guides/1_OneTimePayment.cs` guide do. `Program.cs` gets a new `OneTimePayment` command wired the same way as every other command in that file.

**Tech Stack:** .NET 9 (`net9.0`), `System.CommandLine` 2.0.0-beta5, `System.Net.HttpListener`, xunit + AwesomeAssertions for the new `OpenPayments.Snippets.Tests` project (created in this plan — it does not exist yet).

## Global Constraints

- No changes to any file under `OpenPayments.Snippets/Guides/` — this is a new, separate CLI command, not a rewrite of the guide.
- No automated test may require a live network. Only `GrantInteractionListener` and `GnapInteractionHash` get unit tests; `OneTimePaymentService` itself is verified by `dotnet build` only, per the spec's explicit scope.
- Hash mismatch between the computed and callback-provided GNAP interaction hash is logged via `Console.WriteLine` and the flow **continues** — never throw on mismatch.
- All other errors (missing grants, listener timeout, SDK HTTP failures) propagate uncaught, matching every existing command in `Program.cs` — do not add a new error-handling abstraction.
- `--callbackPort` defaults to `3300` and is overridable.
- CLI reuses the existing `senderWalletAddressOption` (`--sender`/`-s`), `receiverWalletAddressOption` (`--receiver`/`-r`), and `amountOption` (`--amount`/`-a`) instances from `Program.cs` — do not declare new ones for these.
- DI registration: `services.AddTransient<OneTimePaymentService>();`, following the existing registration block in `Program.cs`.
- `GrantInteractionListener.cs` and `GnapInteractionHash.cs` go directly under `OpenPayments.Snippets/Services/` (namespace `OpenPayments.Snippets.Services`), not under `Services/Authenticated/` or `Services/Unauthenticated/` — they are local transport/protocol infrastructure, not Open Payments resource clients.
- `OneTimePaymentService.cs` goes under `OpenPayments.Snippets/Services/Authenticated/` (namespace `OpenPayments.Snippets.Services.Authenticated`), matching every other authenticated service.
- The incoming payment amount must be built from the **receiver's** wallet address's `AssetCode`/`AssetScale` (looked up via `client.GetWalletAddressAsync`), never a hardcoded currency — matching `IncomingPaymentService.CreateIncomingPaymentAsync`'s pattern, not the guide's hardcoded `"MXN"`.
- The interactive outgoing-payment grant's `DebitAmount` limit must come from `quote.DebitAmount` (converted from `Resource.Amount` to `Auth.Amount` field-by-field — there is no implicit conversion between the two identically-shaped but distinct generated types), never the incoming amount.
- The whole repo builds with `TreatWarningsAsErrors=true` (set in `Directory.Build.props`) — new code must be warning-clean, including nullable-reference warnings.
- Package versions are centrally managed (`Directory.Packages.props`, `ManagePackageVersionsCentrally=true`) — new `PackageReference` entries in any new `.csproj` must omit the `Version` attribute; only add a new `PackageVersion` entry to `Directory.Packages.props` if a package isn't already listed there (all packages needed for this plan already are).
- New unit tests use `AwesomeAssertions` (`using AwesomeAssertions;` per file, matching `OpenPayments.Sdk.Tests` convention) — never `FluentAssertions`.

---

## File Structure

- **Create** `OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj` — new xunit test project (does not exist yet in the solution), single-targets `net9.0` to match `OpenPayments.Snippets`.
- **Create** `OpenPayments.Snippets.Tests/Services/GrantInteractionListener_Tests.cs` — unit tests for the callback listener (bind, real HTTP GET, parse, timeout).
- **Create** `OpenPayments.Snippets.Tests/Services/GnapInteractionHash_Tests.cs` — unit test for the pure hash function against a known GNAP spec example vector.
- **Create** `OpenPayments.Snippets/Services/GrantInteractionListener.cs` — `HttpListener` wrapper + `GrantInteractionCallback` DTO.
- **Create** `OpenPayments.Snippets/Services/GnapInteractionHash.cs` — GNAP interaction-hash computation.
- **Create** `OpenPayments.Snippets/Services/Authenticated/OneTimePaymentService.cs` — the end-to-end flow.
- **Modify** `OpenPayments.Snippets/Program.cs` — add `--callbackPort` option, `OneTimePayment` command, DI registration.
- **Modify** `OpenPayments.sln` — add the new test project (via `dotnet sln add`, not hand-edited).

---

### Task 1: `GrantInteractionListener` + new test project scaffold

**Files:**
- Create: `OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj`
- Create: `OpenPayments.Snippets/Services/GrantInteractionListener.cs`
- Test: `OpenPayments.Snippets.Tests/Services/GrantInteractionListener_Tests.cs`
- Modify: `OpenPayments.sln` (via `dotnet sln add`)

**Interfaces:**
- Produces: `namespace OpenPayments.Snippets.Services;` — `class GrantInteractionCallback { string? InteractRef; string? Hash; }` and `sealed class GrantInteractionListener : IDisposable { Task StartAsync(int port); Task<GrantInteractionCallback> WaitForCallbackAsync(TimeSpan timeout); }`. Task 3 (`OneTimePaymentService`) consumes both exactly as declared here.

- [ ] **Step 1: Scaffold the `OpenPayments.Snippets.Tests` project and add it to the solution**

Create `OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector"/>
    <PackageReference Include="AwesomeAssertions"/>
    <PackageReference Include="Microsoft.NET.Test.Sdk"/>
    <PackageReference Include="xunit"/>
    <PackageReference Include="xunit.runner.visualstudio"/>
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit"/>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\OpenPayments.Snippets\OpenPayments.Snippets.csproj" />
  </ItemGroup>

</Project>
```

Run:
```bash
dotnet sln OpenPayments.sln add OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj
```
Expected: `Project ... added to the solution.`

- [ ] **Step 2: Write the failing tests for `GrantInteractionListener`**

Create `OpenPayments.Snippets.Tests/Services/GrantInteractionListener_Tests.cs`:

```csharp
using AwesomeAssertions;
using OpenPayments.Snippets.Services;

namespace OpenPayments.Snippets.Tests.Services;

public class GrantInteractionListener_Tests
{
    [Fact]
    public async Task WaitForCallbackAsync_ParsesInteractRefAndHashFromCallbackRequest()
    {
        const int port = 34519;
        using var listener = new GrantInteractionListener();
        await listener.StartAsync(port);

        var waitTask = listener.WaitForCallbackAsync(TimeSpan.FromSeconds(5));

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(
            $"http://localhost:{port}/callback/?interact_ref=abc123&hash=deadbeef"
        );
        response.EnsureSuccessStatusCode();

        var callback = await waitTask;

        callback.InteractRef.Should().Be("abc123");
        callback.Hash.Should().Be("deadbeef");
    }

    [Fact]
    public async Task WaitForCallbackAsync_ThrowsTimeoutExceptionWhenNoCallbackArrives()
    {
        const int port = 34520;
        using var listener = new GrantInteractionListener();
        await listener.StartAsync(port);

        var act = async () => await listener.WaitForCallbackAsync(TimeSpan.FromMilliseconds(200));

        await act.Should().ThrowAsync<TimeoutException>();
    }
}
```

Note both tests start `WaitForCallbackAsync` (or its `Task.Delay`-bounded wait) without awaiting it before triggering/awaiting the concurrent action — this matters: awaiting the HTTP request first would deadlock, since the server-side response is only written after the listener task itself is awaited.

- [ ] **Step 3: Run the tests to verify they fail to build**

Run: `dotnet test OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj`
Expected: build FAILS — `error CS0246: The type or namespace name 'GrantInteractionListener' could not be found`.

- [ ] **Step 4: Implement `GrantInteractionListener`**

Create `OpenPayments.Snippets/Services/GrantInteractionListener.cs`:

```csharp
using System.Net;
using System.Text;

namespace OpenPayments.Snippets.Services;

public class GrantInteractionCallback
{
    public string? InteractRef { get; set; }
    public string? Hash { get; set; }
}

public sealed class GrantInteractionListener : IDisposable
{
    private const string CallbackHtml =
        "<html><body>You can close this window and return to the terminal.</body></html>";

    private HttpListener? _listener;

    public Task StartAsync(int port)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/callback/");
        _listener.Start();
        return Task.CompletedTask;
    }

    public async Task<GrantInteractionCallback> WaitForCallbackAsync(TimeSpan timeout)
    {
        if (_listener == null)
            throw new InvalidOperationException(
                "StartAsync must be called before WaitForCallbackAsync."
            );

        var contextTask = _listener.GetContextAsync();
        var timeoutTask = Task.Delay(timeout);
        var completed = await Task.WhenAny(contextTask, timeoutTask);

        if (completed == timeoutTask)
            throw new TimeoutException(
                $"Timed out after {timeout} waiting for the grant interaction callback."
            );

        var context = await contextTask;
        var query = context.Request.QueryString;
        var callback = new GrantInteractionCallback
        {
            InteractRef = query["interact_ref"],
            Hash = query["hash"],
        };

        var buffer = Encoding.UTF8.GetBytes(CallbackHtml);
        context.Response.ContentType = "text/html";
        context.Response.ContentLength64 = buffer.Length;
        await context.Response.OutputStream.WriteAsync(buffer);
        context.Response.OutputStream.Close();

        _listener.Stop();

        return callback;
    }

    public void Dispose()
    {
        _listener?.Close();
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj`
Expected: `Passed! - Failed: 0, Passed: 2, Skipped: 0`

- [ ] **Step 6: Commit**

```bash
git add OpenPayments.sln OpenPayments.Snippets.Tests OpenPayments.Snippets/Services/GrantInteractionListener.cs
git commit -m "feat: add GrantInteractionListener for GNAP redirect callbacks"
```

---

### Task 2: `GnapInteractionHash`

**Files:**
- Create: `OpenPayments.Snippets/Services/GnapInteractionHash.cs`
- Test: `OpenPayments.Snippets.Tests/Services/GnapInteractionHash_Tests.cs`

**Interfaces:**
- Consumes: nothing from Task 1.
- Produces: `namespace OpenPayments.Snippets.Services;` — `static class GnapInteractionHash { static string Compute(string clientNonce, string asNonce, string interactRef, Uri grantRequestUri); }`. Task 3 consumes `GnapInteractionHash.Compute(...)` exactly as declared here.

- [ ] **Step 1: Write the failing test**

Create `OpenPayments.Snippets.Tests/Services/GnapInteractionHash_Tests.cs`:

```csharp
using AwesomeAssertions;
using OpenPayments.Snippets.Services;

namespace OpenPayments.Snippets.Tests.Services;

public class GnapInteractionHash_Tests
{
    [Fact]
    public void Compute_MatchesGnapSpecExampleVector()
    {
        var hash = GnapInteractionHash.Compute(
            clientNonce: "VJLO6A4CAYLBXHTR0KRO",
            asNonce: "8UPRZ8WDW7OMX42MSB4Z",
            interactRef: "4IFWWIKYBC2PQ6U56NL1",
            grantRequestUri: new Uri("https://server.example.com/tx")
        );

        hash.Should().Be("wH1AF0isGUGcR-IqwVoISQ_39C6qvpQuPkMRtnyODN0");
    }
}
```

This is the worked example from the GNAP core protocol spec's interaction-hash section (independently verified against `base64url(SHA-256(clientNonce + "\n" + asNonce + "\n" + interactRef + "\n" + grantRequestUri))` via an out-of-band Python/`hashlib` computation before writing this test — not derived from the implementation under test).

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj --filter GnapInteractionHash_Tests`
Expected: build FAILS — `error CS0246: The type or namespace name 'GnapInteractionHash' could not be found`.

- [ ] **Step 3: Implement `GnapInteractionHash`**

Create `OpenPayments.Snippets/Services/GnapInteractionHash.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;

namespace OpenPayments.Snippets.Services;

public static class GnapInteractionHash
{
    public static string Compute(
        string clientNonce,
        string asNonce,
        string interactRef,
        Uri grantRequestUri
    )
    {
        var data = $"{clientNonce}\n{asNonce}\n{interactRef}\n{grantRequestUri}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj --filter GnapInteractionHash_Tests`
Expected: `Passed! - Failed: 0, Passed: 1, Skipped: 0`

- [ ] **Step 5: Run the full test suite to verify nothing regressed**

Run: `dotnet test OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj`
Expected: `Passed! - Failed: 0, Passed: 3, Skipped: 0`

- [ ] **Step 6: Commit**

```bash
git add OpenPayments.Snippets/Services/GnapInteractionHash.cs OpenPayments.Snippets.Tests/Services/GnapInteractionHash_Tests.cs
git commit -m "feat: add GNAP interaction-hash computation"
```

---

### Task 3: `OneTimePaymentService`

**Files:**
- Create: `OpenPayments.Snippets/Services/Authenticated/OneTimePaymentService.cs`

**Interfaces:**
- Consumes: `GrantInteractionListener` and `GnapInteractionHash` from Tasks 1–2 (exact signatures above); `IAuthenticatedClient` (existing, `OpenPayments.Sdk/Clients/IAuthenticatedClient.cs`).
- Produces: `namespace OpenPayments.Snippets.Services.Authenticated;` — `class OneTimePaymentService(IAuthenticatedClient client) { Task RunAsync(string senderWalletAddress, string receiverWalletAddress, string amount, int callbackPort); }`. Task 4 (`Program.cs`) consumes `OneTimePaymentService.RunAsync(sender, receiver, amount, callbackPort)` exactly as declared here.

No automated test for this task — per the spec, the full flow requires a live Open Payments network and is inherently manual. Verification here is `dotnet build` only (no runtime warnings, since `TreatWarningsAsErrors=true`).

- [ ] **Step 1: Implement `OneTimePaymentService`**

Create `OpenPayments.Snippets/Services/Authenticated/OneTimePaymentService.cs`:

```csharp
using System.Diagnostics;
using Interledger.OpenPayments.Clients;
using Interledger.OpenPayments.Generated.Auth;
using Interledger.OpenPayments.Generated.Resource;
using OpenPayments.Snippets.Services;
using ResourceAmount = Interledger.OpenPayments.Generated.Resource.Amount;
using AuthAmount = Interledger.OpenPayments.Generated.Auth.Amount;

namespace OpenPayments.Snippets.Services.Authenticated;

public class OneTimePaymentService(IAuthenticatedClient client)
{
    private static readonly TimeSpan CallbackTimeout = TimeSpan.FromMinutes(5);

    public async Task RunAsync(
        string senderWalletAddress,
        string receiverWalletAddress,
        string amount,
        int callbackPort
    )
    {
        // 1. Resolve wallet addresses
        var senderWaDetails = await client.GetWalletAddressAsync(senderWalletAddress);
        var receiverWaDetails = await client.GetWalletAddressAsync(receiverWalletAddress);

        // 2. Non-interactive incoming-payment grant + create incoming payment (receiver's asset)
        var incomingPaymentGrant = await client.RequestGrantAsync(
            new RequestArgs { Url = receiverWaDetails.AuthServer },
            new GrantCreateBody
            {
                AccessToken = new AccessToken
                {
                    Access = [new IncomingAccess { Actions = [Actions.Create] }],
                },
            }
        );

        if (incomingPaymentGrant.AccessToken == null)
            throw new Exception("Expected a non-interactive incoming payment grant");

        var incomingPayment = await client.CreateIncomingPaymentAsync(
            new AuthRequestArgs
            {
                Url = receiverWaDetails.ResourceServer,
                AccessToken = incomingPaymentGrant.AccessToken.Value,
            },
            new IncomingPaymentBody
            {
                WalletAddress = receiverWaDetails.Id,
                IncomingAmount = new ResourceAmount
                {
                    AssetCode = receiverWaDetails.AssetCode,
                    AssetScale = receiverWaDetails.AssetScale,
                    Value = amount,
                },
            }
        );

        Console.WriteLine("===Incoming Payment===");
        Console.WriteLine("Id: {0}", incomingPayment.Id);
        Console.WriteLine("Amount: {0}", incomingPayment.ReceivedAmount.Value);
        Console.WriteLine("ExpiresAt: {0}", incomingPayment.ExpiresAt);

        // 3. Non-interactive quote grant + create quote for that incoming payment
        var quoteGrant = await client.RequestGrantAsync(
            new RequestArgs { Url = senderWaDetails.AuthServer },
            new GrantCreateBody
            {
                AccessToken = new AccessToken
                {
                    Access = [new QuoteAccess { Actions = [Actions.Create] }],
                },
            }
        );

        if (quoteGrant.AccessToken == null)
            throw new Exception("Expected a non-interactive quote grant");

        var quote = await client.CreateQuoteAsync(
            new AuthRequestArgs
            {
                Url = senderWaDetails.ResourceServer,
                AccessToken = quoteGrant.AccessToken.Value,
            },
            new QuoteBody
            {
                WalletAddress = senderWaDetails.Id,
                Receiver = incomingPayment.Id,
                Method = PaymentMethod.Ilp,
            }
        );

        Console.WriteLine("===Quote===");
        Console.WriteLine("Id: {0}", quote.Id);
        Console.WriteLine("Receive Amount: {0}", quote.ReceiveAmount.Value);
        Console.WriteLine("Debit Amount: {0}", quote.DebitAmount.Value);

        // 4. Start the local callback listener
        using var interactionListener = new GrantInteractionListener();
        await interactionListener.StartAsync(callbackPort);

        var clientNonce = Guid.NewGuid().ToString();
        var callbackUri = new Uri($"http://localhost:{callbackPort}/callback");

        // 5. Interactive outgoing-payment grant, limited to the quote's debit amount
        var outgoingPaymentGrant = await client.RequestGrantAsync(
            new RequestArgs { Url = senderWaDetails.AuthServer },
            new GrantCreateBodyWithInteract
            {
                AccessToken = new AccessToken
                {
                    Access =
                    [
                        new OutgoingAccess
                        {
                            Identifier = senderWaDetails.Id,
                            Actions = [Actions.Create],
                            Limits = new OutgoingAccessLimits
                            {
                                DebitAmount = new AuthAmount(
                                    quote.DebitAmount.Value,
                                    quote.DebitAmount.AssetCode,
                                    quote.DebitAmount.AssetScale
                                ),
                            },
                        },
                    ],
                },
                Interact = new InteractRequest
                {
                    Start = [Start.Redirect],
                    Finish = new Finish
                    {
                        Method = FinishMethod.Redirect,
                        Uri = callbackUri,
                        Nonce = clientNonce,
                    },
                },
            }
        );

        if (outgoingPaymentGrant.Interact == null)
            throw new Exception("Expected an interactive outgoing payment grant");

        var redirectUrl = outgoingPaymentGrant.Interact.Redirect;
        var asFinishNonce = outgoingPaymentGrant.Interact.Finish;

        // 6. Print/open the interactive redirect
        Console.WriteLine("===Interaction Required===");
        Console.WriteLine("Visit the link below to authorize the payment:");
        Console.WriteLine(redirectUrl);

        try
        {
            Process.Start(new ProcessStartInfo(redirectUrl.ToString()) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                "Could not open the browser automatically ({0}); open the URL above manually.",
                ex.Message
            );
        }

        // 7. Wait for the callback
        var callback = await interactionListener.WaitForCallbackAsync(CallbackTimeout);
        var interactRef =
            callback.InteractRef ?? throw new Exception("Callback did not include an interact_ref");

        // 8. Verify the GNAP interaction hash (log-only on mismatch)
        var expectedHash = GnapInteractionHash.Compute(
            clientNonce,
            asFinishNonce,
            interactRef,
            senderWaDetails.AuthServer
        );

        if (callback.Hash != expectedHash)
        {
            Console.WriteLine(
                "WARNING: interaction hash mismatch (got '{0}', expected '{1}'); continuing anyway.",
                callback.Hash,
                expectedHash
            );
        }

        // 9. Continue the grant and create the outgoing payment
        var outgoingPaymentToken = await client.ContinueGrantAsync(
            new AuthRequestArgs
            {
                Url = outgoingPaymentGrant.Continue.Uri,
                AccessToken = outgoingPaymentGrant.Continue.AccessToken.Value,
            },
            new GrantContinueBody { InteractRef = interactRef }
        );

        if (outgoingPaymentToken.AccessToken == null)
            throw new Exception("Expected a non-interactive grant after continuation");

        var outgoingPayment = await client.CreateOutgoingPaymentAsync(
            new AuthRequestArgs
            {
                Url = senderWaDetails.ResourceServer,
                AccessToken = outgoingPaymentToken.AccessToken.Value,
            },
            new OutgoingPaymentBodyFromQuote { WalletAddress = senderWaDetails.Id, QuoteId = quote.Id }
        );

        // 10. Summary
        Console.WriteLine("===Outgoing Payment===");
        Console.WriteLine("Id: {0}", outgoingPayment.Id);
        Console.WriteLine("Quote: {0}", outgoingPayment.QuoteId);
        Console.WriteLine("IncomingPaymentUrl: {0}", outgoingPayment.Receiver);
        Console.WriteLine("Receive Amount: {0}", outgoingPayment.ReceiveAmount.Value);
        Console.WriteLine("Debit Amount: {0}", outgoingPayment.DebitAmount.Value);
    }
}
```

Note on `quote.Receiver`: this intentionally sets `Receiver = incomingPayment.Id` (the created incoming-payment resource URL), **not** `receiverWaDetails.Id` (the wallet address) — the latter is what `Guides/1_OneTimePayment.cs` does, but that's inconsistent with `QuoteService.CreateQuoteAsync`'s established, correct usage and with this spec's own wording ("create the quote for **that incoming payment**"). Do not copy the guide's `Receiver` value here.

- [ ] **Step 2: Build to verify it compiles cleanly**

Run: `dotnet build OpenPayments.Snippets/OpenPayments.Snippets.csproj`
Expected: `Build succeeded.` with `0 Warning(s)` and `0 Error(s)`.

- [ ] **Step 3: Commit**

```bash
git add OpenPayments.Snippets/Services/Authenticated/OneTimePaymentService.cs
git commit -m "feat: add OneTimePaymentService for the live one-time-payment flow"
```

---

### Task 4: Wire the `OneTimePayment` CLI command

**Files:**
- Modify: `OpenPayments.Snippets/Program.cs`

**Interfaces:**
- Consumes: `OneTimePaymentService.RunAsync(string senderWalletAddress, string receiverWalletAddress, string amount, int callbackPort)` from Task 3; existing `senderWalletAddressOption`, `receiverWalletAddressOption`, `amountOption` (`Program.cs:41-53`).
- Produces: none (leaf task).

- [ ] **Step 1: Register `OneTimePaymentService` in DI**

In `OpenPayments.Snippets/Program.cs`, modify the registration block at line 22-27:

```csharp
services.AddTransient<WalletAddressService>();
services.AddTransient<PublicIncomingPaymentService>();
services.AddTransient<IncomingPaymentService>();
services.AddTransient<QuoteService>();
services.AddTransient<OutgoingPaymentService>();
services.AddTransient<TokenService>();
services.AddTransient<OneTimePaymentService>();
```

- [ ] **Step 2: Add the `--callbackPort` option**

After the existing `tokenAction` option declaration (`Program.cs:78-81`), add:

```csharp
Option<int> callbackPortOption = new("--callbackPort")
{
    Description = "Local port for the GNAP redirect callback listener.",
    DefaultValueFactory = _ => 3300,
};
```

- [ ] **Step 3: Declare the `OneTimePayment` command**

After the existing `listOutgoingPaymentsCommand` declaration (`Program.cs:126`), add:

```csharp
var oneTimePaymentCommand = new Command("OneTimePayment")
{
    senderWalletAddressOption,
    receiverWalletAddressOption,
    amountOption,
    callbackPortOption,
};
```

- [ ] **Step 4: Wire its action**

After the existing `listOutgoingPaymentsCommand.SetAction(...)` block (`Program.cs:202-208`), add:

```csharp
oneTimePaymentCommand.SetAction(async result =>
{
    var sender = result.GetValue(senderWalletAddressOption)!;
    var receiver = result.GetValue(receiverWalletAddressOption)!;
    var amount = result.GetValue(amountOption)!;
    var callbackPort = result.GetValue(callbackPortOption);

    var service = provider.GetRequiredService<OneTimePaymentService>();
    await service.RunAsync(sender, receiver, amount, callbackPort);
});
```

- [ ] **Step 5: Register the command on the root command**

After `rootCommand.Add(listOutgoingPaymentsCommand);` (`Program.cs:244`), add:

```csharp
rootCommand.Add(oneTimePaymentCommand);
```

- [ ] **Step 6: Build and smoke-test the CLI surface**

Run:
```bash
dotnet build OpenPayments.Snippets/OpenPayments.Snippets.csproj
```
Expected: `Build succeeded.` with `0 Warning(s)` and `0 Error(s)`.

Run:
```bash
dotnet run --project OpenPayments.Snippets -- OneTimePayment --help
```
Expected: help output listing `--sender/-s`, `--receiver/-r`, `--amount/-a`, and `--callbackPort` (default `3300`), with no exception.

- [ ] **Step 7: Run the full solution build and test suite**

Run:
```bash
dotnet build OpenPayments.sln
dotnet test OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj
```
Expected: both succeed; the test run reports `Passed! - Failed: 0, Passed: 3, Skipped: 0`.

- [ ] **Step 8: Commit**

```bash
git add OpenPayments.Snippets/Program.cs
git commit -m "feat: add OneTimePayment CLI command"
```

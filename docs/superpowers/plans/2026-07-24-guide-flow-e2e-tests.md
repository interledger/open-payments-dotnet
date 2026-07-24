# Guide-Flow End-to-End Test Suite Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an in-process, stateful fake Open Payments server and run all eight `OpenPayments.Snippets/Guides/*.cs` classes against it unmodified, asserting on the resulting resource graph so guide/SDK drift fails CI instead of rotting silently.

**Architecture:** A single `FakeOpenPaymentsServer` (ASP.NET Core `TestServer` + Minimal API) implements the six Open Payments operations the guides use, keyed by path, reset fresh per test. A `TestServerRoutingHandler` `DelegatingHandler`, inserted into the SDK's `"authenticated"` named `HttpClient` pipeline *after* the existing `SigningHttpMessageHandler`, rewrites every outgoing request's scheme/host/port to the `TestServer`'s in-memory endpoint so guide code's hard-coded URLs (e.g. `https://cloudninebank.example.com/customer`) transparently resolve to the fake backend. Each guide test builds the real `Guide` class via the SDK's normal `UseOpenPayments()` DI registration and calls `Run()` unmodified, then asserts on the fake server's observed resource state.

**Tech Stack:** xunit, AwesomeAssertions (this repo's FluentAssertions-API-compatible library — see Global Constraints), Microsoft.AspNetCore.TestHost (via the `Microsoft.AspNetCore.Mvc.Testing` package, which auto-wires the `Microsoft.AspNetCore.App` framework reference into the plain `Microsoft.NET.Sdk` test project), Newtonsoft.Json, NSec.Cryptography (already flows transitively through `OpenPayments.Sdk`).

## Global Constraints

- Target framework for `OpenPayments.Snippets.Tests` is `net9.0` only (not multi-targeted).
- `Directory.Build.props` sets `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` and `<AnalysisLevel>latest</AnalysisLevel>` repo-wide. All new code must compile with zero warnings — no unused usings, no nullable-dereference warnings.
- `<ImplicitUsings>enable</ImplicitUsings>` is set on `OpenPayments.Snippets.Tests.csproj`, which brings in `System`, `System.Collections.Generic`, `System.IO`, `System.Linq`, `System.Net.Http`, `System.Threading`, `System.Threading.Tasks` implicitly — do not add explicit `using` directives for these namespaces (redundant explicit usings of implicit ones trigger CS0105 under `TreatWarningsAsErrors`).
- The repo uses **Central Package Management** (`Directory.Packages.props`, `ManagePackageVersionsCentrally=true`). Every `PackageReference` in a `.csproj` must have a matching `<PackageVersion>` entry in `Directory.Packages.props` (no `Version=` attribute on the `PackageReference` itself).
- This repo's assertion library is **AwesomeAssertions**, not FluentAssertions (already the sole assertion package referenced by both `OpenPayments.Sdk.Tests` and `OpenPayments.Snippets.Tests`). It is an API-compatible fork — `using AwesomeAssertions;` then `.Should()` exactly as FluentAssertions. Use it, not FluentAssertions, despite the design doc's wording.
- No shared xunit fixture (no `ICollectionFixture`/`IClassFixture`). Each guide test class inherits `GuideTestBase`, whose constructor builds a fresh `FakeOpenPaymentsServer` + `IAuthenticatedClient` — xunit already instantiates the test class fresh per `[Fact]`, which gives per-test isolation without a shared-state fixture.
- Deviation from the design doc's literal package list: use `Microsoft.AspNetCore.TestHost`'s `TestServer` class directly (via the `Microsoft.AspNetCore.Mvc.Testing` package, which pulls it in transitively and wires the framework reference) rather than subclassing `WebApplicationFactory<TEntryPoint>`. There is no existing ASP.NET Core `Program`/entry-point class in this repo to point a `WebApplicationFactory` at, and `WebApplicationFactory` is designed for wrapping an existing app; `TestServer` built from a `WebHostBuilder` is the simpler, equivalent primitive the design doc names as an alternative ("WebApplicationFactory/TestServer").
- Every authenticated fake-server endpoint must return `401` when the `Authorization: GNAP <token>` header's token wasn't actually issued by that server instance — this is the one behavior the design doc calls out explicitly as catching a realistic guide-rot failure mode.

---

## File Structure

- `OpenPayments.Snippets.Tests/Infrastructure/TestServerRoutingHandler.cs` — `DelegatingHandler` that rewrites outgoing request scheme/host/port to the fake server's `TestServer` address.
- `OpenPayments.Snippets.Tests/Infrastructure/FakeOpenPaymentsServer.cs` — the stateful in-memory server: wallet address lookup, grant request/continue, incoming payment/quote/outgoing payment creation, keyed resource dictionaries exposed for assertions.
- `OpenPayments.Snippets.Tests/Infrastructure/GuideTestBase.cs` — abstract base that wires a fresh `FakeOpenPaymentsServer` + DI-registered `IAuthenticatedClient` per test instance.
- `OpenPayments.Snippets.Tests/Guides/OneTimePayment_Tests.cs`
- `OpenPayments.Snippets.Tests/Guides/SendRemittanceWithFixedDebit_Tests.cs`
- `OpenPayments.Snippets.Tests/Guides/SendRemittanceWithFixedReceive_Tests.cs`
- `OpenPayments.Snippets.Tests/Guides/SetupRecurringRemittanceWithFixedIncoming_Tests.cs`
- `OpenPayments.Snippets.Tests/Guides/SendRecurringRemittanceWithFixedDebit_Tests.cs`
- `OpenPayments.Snippets.Tests/Guides/SendRecurringRemittanceWithFixedReceive_Tests.cs`
- `OpenPayments.Snippets.Tests/Guides/SplitIncomingPayment_Tests.cs`
- `OpenPayments.Snippets.Tests/Guides/GetGrantForFuturePayments_Tests.cs`
- Modified: `Directory.Packages.props` (add `Microsoft.AspNetCore.Mvc.Testing` version pin).
- Modified: `OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj` (add package references).

## Protocol facts this plan relies on (verified against the SDK source)

- `GetWalletAddressAsync` issues a bare `GET <url>` — the wallet address URL itself is the resource path, no suffix appended.
- `RequestGrantAsync` POSTs to `NormalizeBaseUrl(authServer)` (trailing slash ensured, no path appended) — i.e. exactly the auth server root.
- `ContinueGrantAsync` POSTs to the literal `Continue.Uri` from the initial grant response (no normalization/append), with header `Authorization: GNAP <continuation token>`.
- `CreateIncomingPaymentAsync` POSTs to `{resourceServer}/incoming-payments`; `CreateQuoteAsync` to `{resourceServer}/quotes`; `CreateOutgoingPaymentAsync` to `{resourceServer}/outgoing-payments`. All three carry `Authorization: GNAP <access token>` and expect **201** on success; grant creation/continuation expect **200**.
- `GrantCreateBody` (non-interactive) has no `interact` field; `GrantCreateBodyWithInteract` always does. The fake server distinguishes them by checking for the presence of the `interact` key in the raw request JSON.
- `OpenPaymentsSerialization.DefaultSettings`'s contract resolver forces `Required.Default` + `NullValueHandling.Ignore` on every property, so response payloads only need the fields guides actually read — omitted fields safely default rather than throwing.
- Confirmed working pattern (from `OpenPayments.Sdk.Tests/Extensions/ServiceCollectionExtensions_Tests.cs`, test `UseOpenPayments_AuthenticatedClient_SignsOutgoingRequests`): calling `services.AddHttpClient("authenticated")` again *after* `UseOpenPayments(...)` appends handler configuration rather than replacing the client's registration — `SigningHttpMessageHandler` (added first, inside `UseOpenPayments`) stays outermost; whatever is added afterward sits closer to the primary handler. This is exactly the ordering `TestServerRoutingHandler` needs: signing must see the pre-rewrite request.

---

### Task 1: Fake Open Payments server infrastructure + first guide test

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj`
- Create: `OpenPayments.Snippets.Tests/Infrastructure/TestServerRoutingHandler.cs`
- Create: `OpenPayments.Snippets.Tests/Infrastructure/FakeOpenPaymentsServer.cs`
- Create: `OpenPayments.Snippets.Tests/Infrastructure/GuideTestBase.cs`
- Create: `OpenPayments.Snippets.Tests/Guides/OneTimePayment_Tests.cs`

**Interfaces:**
- Produces: `FakeOpenPaymentsServer` — public surface used by every later task:
  - `Uri BaseAddress { get; }`
  - `HttpMessageHandler CreateHandler()`
  - `IReadOnlyDictionary<string, IncomingPaymentResponse> IncomingPayments { get; }`
  - `IReadOnlyDictionary<string, QuoteResponse> Quotes { get; }`
  - `IReadOnlyDictionary<string, OutgoingPaymentWithSpentAmountsResponse> OutgoingPayments { get; }`
  - `IReadOnlyCollection<string> IssuedAccessTokens { get; }`
  - Implements `IDisposable`.
- Produces: `TestServerRoutingHandler(Uri targetBaseAddress)` — a `DelegatingHandler`.
- Produces: `GuideTestBase` (abstract, implements `IDisposable`) — exposes `protected FakeOpenPaymentsServer Server { get; }` and `protected IAuthenticatedClient Client { get; }`, constructed fresh per instance.
- Consumes (from `OpenPayments.Sdk`): `IAuthenticatedClient`, `UseOpenPayments`, `OpenPaymentsOptions`, all `Interledger.OpenPayments.Generated.{Auth,Resource,Wallet}` types, `OpenPaymentsSerialization.DefaultSettings`.
- Consumes (from `OpenPayments.Snippets`): `OpenPayments.Snippets.Guides.OneTimePayment`.

- [ ] **Step 1: Add the `Microsoft.AspNetCore.Mvc.Testing` package version to central package management**

Edit `Directory.Packages.props`, adding one line to the existing `<ItemGroup>` (keep alphabetical order among the `Microsoft.*` entries):

```xml
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.6" />
```

The full `<ItemGroup>` after the edit:

```xml
  <ItemGroup>
    <PackageVersion Include="coverlet.collector" Version="6.0.4" />
    <PackageVersion Include="AwesomeAssertions" Version="9.5.0" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.6" />
    <PackageVersion Include="Microsoft.CodeAnalysis.PublicApiAnalyzers" Version="3.3.4" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="9.0.6" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.6" />
    <PackageVersion Include="Microsoft.Extensions.Http" Version="9.0.6" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageVersion Include="MinVer" Version="6.0.0" />
    <PackageVersion Include="Moq" Version="4.20.72" />
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageVersion Include="NSec.Cryptography" Version="25.4.0" />
    <PackageVersion Include="System.CommandLine" Version="2.0.0-beta5.25306.1" />
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
```

- [ ] **Step 2: Add package references to the test project**

Edit `OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj`, changing:

```xml
  <ItemGroup>
    <PackageReference Include="coverlet.collector"/>
    <PackageReference Include="AwesomeAssertions"/>
    <PackageReference Include="Microsoft.NET.Test.Sdk"/>
    <PackageReference Include="xunit"/>
    <PackageReference Include="xunit.runner.visualstudio"/>
  </ItemGroup>
```

to:

```xml
  <ItemGroup>
    <PackageReference Include="coverlet.collector"/>
    <PackageReference Include="AwesomeAssertions"/>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing"/>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection"/>
    <PackageReference Include="Microsoft.Extensions.Http"/>
    <PackageReference Include="Microsoft.NET.Test.Sdk"/>
    <PackageReference Include="xunit"/>
    <PackageReference Include="xunit.runner.visualstudio"/>
  </ItemGroup>
```

- [ ] **Step 3: Write the first guide test (it won't compile yet — the infrastructure it depends on doesn't exist)**

Create `OpenPayments.Snippets.Tests/Guides/OneTimePayment_Tests.cs`:

```csharp
using AwesomeAssertions;
using OpenPayments.Snippets.Guides;
using OpenPayments.Snippets.Tests.Infrastructure;

namespace OpenPayments.Snippets.Tests.Guides;

public class OneTimePayment_Tests : GuideTestBase
{
    [Fact]
    public async Task Run_CreatesOutgoingPaymentLinkedToQuoteAndIncomingPayment()
    {
        await new OneTimePayment(Client).Run();

        Server.IncomingPayments.Should().ContainSingle();
        var incomingPayment = Server.IncomingPayments.Values.Single();
        incomingPayment.IncomingAmount!.Value.Should().Be("140000");
        incomingPayment.IncomingAmount.AssetCode.Should().Be("MXN");

        Server.Quotes.Should().ContainSingle();
        var quote = Server.Quotes.Values.Single();
        quote.Receiver.Should().Be(incomingPayment.Id);
        quote.DebitAmount.Value.Should().Be("140000");

        Server.OutgoingPayments.Should().ContainSingle();
        var outgoingPayment = Server.OutgoingPayments.Values.Single();
        outgoingPayment.QuoteId.Should().Be(quote.Id);
        outgoingPayment.DebitAmount.Value.Should().Be(quote.DebitAmount.Value);
    }
}
```

- [ ] **Step 4: Confirm it fails to build**

Run: `dotnet build OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj`
Expected: FAIL — `GuideTestBase` (and the `Infrastructure` namespace) do not exist yet.

- [ ] **Step 5: Create the routing handler**

Create `OpenPayments.Snippets.Tests/Infrastructure/TestServerRoutingHandler.cs`:

```csharp
namespace OpenPayments.Snippets.Tests.Infrastructure;

/// <summary>
/// Rewrites every outgoing request's scheme/host/port to <paramref name="targetBaseAddress"/>,
/// leaving path and query untouched. Lets guide code hard-code URLs like
/// <c>https://cloudninebank.example.com/customer</c> while the request actually reaches
/// <see cref="FakeOpenPaymentsServer"/>'s in-memory <c>TestServer</c>. Must be registered
/// after <c>SigningHttpMessageHandler</c> in the HTTP client pipeline so signing still
/// operates on the pre-rewrite request.
/// </summary>
public sealed class TestServerRoutingHandler(Uri targetBaseAddress) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var original = request.RequestUri!;
        request.RequestUri = new UriBuilder(original)
        {
            Scheme = targetBaseAddress.Scheme,
            Host = targetBaseAddress.Host,
            Port = targetBaseAddress.Port,
        }.Uri;

        return base.SendAsync(request, cancellationToken);
    }
}
```

- [ ] **Step 6: Create the fake server**

Create `OpenPayments.Snippets.Tests/Infrastructure/FakeOpenPaymentsServer.cs`:

```csharp
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Interledger.OpenPayments.Generated.Auth;
using Interledger.OpenPayments.Generated.Resource;
using Interledger.OpenPayments.Generated.Wallet;
using Interledger.OpenPayments.Serialization;
using Amount = Interledger.OpenPayments.Generated.Resource.Amount;

namespace OpenPayments.Snippets.Tests.Infrastructure;

/// <summary>
/// A minimal, stateful, in-process Open Payments server. Implements just enough of the
/// grant, incoming-payment, quote, and outgoing-payment protocol for the Guides in
/// OpenPayments.Snippets to run against it unmodified. One backend serves every
/// guide-visible wallet address, since every resource path (<c>/customer</c> vs
/// <c>/sender</c>, <c>/incoming-payments/{id}</c>, ...) is already unique.
/// </summary>
public sealed class FakeOpenPaymentsServer : IDisposable
{
    private readonly TestServer _testServer;

    private readonly Dictionary<string, WalletAddress> _walletAddresses = new();
    private readonly HashSet<string> _pendingGrantIds = new();
    private readonly Dictionary<string, string> _continuationTokenToGrantId = new();
    private readonly HashSet<string> _accessTokens = new();

    private readonly Dictionary<string, IncomingPaymentResponse> _incomingPayments = new();
    private readonly Dictionary<string, QuoteResponse> _quotes = new();
    private readonly Dictionary<string, OutgoingPaymentWithSpentAmountsResponse> _outgoingPayments = new();

    public IReadOnlyDictionary<string, IncomingPaymentResponse> IncomingPayments => _incomingPayments;
    public IReadOnlyDictionary<string, QuoteResponse> Quotes => _quotes;
    public IReadOnlyDictionary<string, OutgoingPaymentWithSpentAmountsResponse> OutgoingPayments => _outgoingPayments;
    public IReadOnlyCollection<string> IssuedAccessTokens => _accessTokens;

    public Uri BaseAddress => _testServer.BaseAddress;

    public FakeOpenPaymentsServer()
    {
        var hostBuilder = new WebHostBuilder()
            .ConfigureServices(services => services.AddRouting())
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/{*path}", HandleGetWalletAddress);
                    endpoints.MapPost("/", HandleCreateGrant);
                    endpoints.MapPost("/continue/{id}", HandleContinueGrant);
                    endpoints.MapPost("/incoming-payments", HandleCreateIncomingPayment);
                    endpoints.MapPost("/quotes", HandleCreateQuote);
                    endpoints.MapPost("/outgoing-payments", HandleCreateOutgoingPayment);
                });
            });

        _testServer = new TestServer(hostBuilder);
    }

    public HttpMessageHandler CreateHandler() => _testServer.CreateHandler();

    public void Dispose() => _testServer.Dispose();

    private static string NewId() => Guid.NewGuid().ToString("N");

    private static async Task<T> ReadBodyAsync<T>(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body);
        var json = await reader.ReadToEndAsync();
        return JsonConvert.DeserializeObject<T>(json, OpenPaymentsSerialization.DefaultSettings)!;
    }

    private static async Task<JObject> ReadBodyAsJObjectAsync(HttpRequest request)
    {
        using var reader = new StreamReader(request.Body);
        var json = await reader.ReadToEndAsync();
        return JObject.Parse(json);
    }

    private static async Task WriteJsonAsync(HttpResponse response, int statusCode, object body)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json";
        await response.WriteAsync(JsonConvert.SerializeObject(body, OpenPaymentsSerialization.DefaultSettings));
    }

    private static string? GetBearerToken(HttpRequest request)
    {
        var header = request.Headers["Authorization"].ToString();
        return header.StartsWith("GNAP ", StringComparison.Ordinal) ? header["GNAP ".Length..] : null;
    }

    private bool TryAuthorize(HttpRequest request)
    {
        var token = GetBearerToken(request);
        return token != null && _accessTokens.Contains(token);
    }

    private Task HandleGetWalletAddress(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        if (!_walletAddresses.TryGetValue(path, out var wallet))
        {
            wallet = new WalletAddress
            {
                Id = new Uri(BaseAddress, path),
                PublicName = path.Trim('/'),
                AssetCode = "USD",
                AssetScale = 2,
                AuthServer = BaseAddress,
                ResourceServer = BaseAddress,
            };
            _walletAddresses[path] = wallet;
        }

        return WriteJsonAsync(context.Response, StatusCodes.Status200OK, wallet);
    }

    // Non-interactive grants (Incoming, Quote) issue an AccessToken immediately.
    // Interactive grants (Outgoing, with `interact`) return a Continue URI instead.
    private async Task HandleCreateGrant(HttpContext context)
    {
        var body = await ReadBodyAsJObjectAsync(context.Request);

        if (body["interact"] is not null)
        {
            var grantId = NewId();
            var continuationToken = NewId();
            _pendingGrantIds.Add(grantId);
            _continuationTokenToGrantId[continuationToken] = grantId;

            await WriteJsonAsync(context.Response, StatusCodes.Status200OK, new AuthResponse
            {
                Continue = new AuthContinue
                {
                    AccessToken = new ContinueAccessToken { Value = continuationToken },
                    Uri = new Uri(BaseAddress, $"continue/{grantId}"),
                },
            });
            return;
        }

        var token = NewId();
        _accessTokens.Add(token);

        await WriteJsonAsync(context.Response, StatusCodes.Status200OK, new AuthResponse
        {
            AccessToken = new AccessTokenResponse { Value = token, Access = new Collection<AccessItem>() },
        });
    }

    // Auto-approves any interactRef (guides only ever pass a locally generated GUID),
    // simulating completed user consent, and issues the final resource access token.
    private async Task HandleContinueGrant(HttpContext context)
    {
        var id = (string)context.Request.RouteValues["id"]!;
        var token = GetBearerToken(context.Request);

        if (token is null || !_continuationTokenToGrantId.TryGetValue(token, out var grantId) || grantId != id)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var body = await ReadBodyAsync<GrantContinueBody>(context.Request);
        if (string.IsNullOrEmpty(body.InteractRef))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        _pendingGrantIds.Remove(grantId);
        _continuationTokenToGrantId.Remove(token);

        var accessToken = NewId();
        _accessTokens.Add(accessToken);

        await WriteJsonAsync(context.Response, StatusCodes.Status200OK, new AuthResponse
        {
            AccessToken = new AccessTokenResponse { Value = accessToken, Access = new Collection<AccessItem>() },
        });
    }

    private async Task HandleCreateIncomingPayment(HttpContext context)
    {
        if (!TryAuthorize(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var body = await ReadBodyAsync<Body>(context.Request);
        var id = new Uri(BaseAddress, $"incoming-payments/{NewId()}");
        var receivedAmountCurrency = body.IncomingAmount ?? new Amount("0", "USD", 2);

        var response = new IncomingPaymentResponse
        {
            Id = id,
            WalletAddress = body.WalletAddress,
            Completed = false,
            IncomingAmount = body.IncomingAmount,
            ReceivedAmount = new Amount("0", receivedAmountCurrency.AssetCode, receivedAmountCurrency.AssetScale),
            ExpiresAt = body.ExpiresAt,
            Metadata = body.Metadata,
            CreatedAt = DateTimeOffset.UtcNow,
            Methods = new Collection<IlpPaymentMethod>
            {
                new()
                {
                    Type = IlpPaymentMethodType.Ilp,
                    IlpAddress = $"test.wallet.{NewId()}",
                    SharedSecret = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                },
            },
        };

        _incomingPayments[id.ToString()] = response;
        await WriteJsonAsync(context.Response, StatusCodes.Status201Created, response);
    }

    // A bare QuoteBody (no debitAmount/receiveAmount) quotes against the receiver
    // incoming payment's own incomingAmount — the "receiver is an Incoming Payment with
    // an incomingAmount" case every bare-QuoteBody guide relies on.
    private async Task HandleCreateQuote(HttpContext context)
    {
        if (!TryAuthorize(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var raw = await ReadBodyAsJObjectAsync(context.Request);
        var walletAddress = (Uri)raw["walletAddress"]!;
        var receiver = (Uri)raw["receiver"]!;

        Amount debitAmount;
        Amount receiveAmount;

        if (raw["debitAmount"] is JObject debitJson)
        {
            debitAmount = debitJson.ToObject<Amount>()!;
            var receiverWallet = WalletForIncomingPayment(receiver);
            receiveAmount = new Amount(debitAmount.Value, receiverWallet.AssetCode, receiverWallet.AssetScale);
        }
        else if (raw["receiveAmount"] is JObject receiveJson)
        {
            receiveAmount = receiveJson.ToObject<Amount>()!;
            var senderWallet = _walletAddresses[walletAddress.AbsolutePath];
            debitAmount = new Amount(receiveAmount.Value, senderWallet.AssetCode, senderWallet.AssetScale);
        }
        else
        {
            var incomingPayment = _incomingPayments[receiver.ToString()];
            var amount = incomingPayment.IncomingAmount
                ?? throw new InvalidOperationException(
                    $"Quote receiver {receiver} has no incomingAmount to quote against."
                );
            debitAmount = amount;
            receiveAmount = amount;
        }

        var id = new Uri(BaseAddress, $"quotes/{NewId()}");
        var response = new QuoteResponse
        {
            Id = id,
            WalletAddress = walletAddress,
            Receiver = receiver,
            DebitAmount = debitAmount,
            ReceiveAmount = receiveAmount,
            Method = PaymentMethod.Ilp,
            ExpiresAt = "",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _quotes[id.ToString()] = response;
        await WriteJsonAsync(context.Response, StatusCodes.Status201Created, response);
    }

    private WalletAddress WalletForIncomingPayment(Uri receiver)
    {
        var incomingPayment = _incomingPayments[receiver.ToString()];
        return _walletAddresses[incomingPayment.WalletAddress.AbsolutePath];
    }

    private async Task HandleCreateOutgoingPayment(HttpContext context)
    {
        if (!TryAuthorize(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var raw = await ReadBodyAsJObjectAsync(context.Request);
        var walletAddress = (Uri)raw["walletAddress"]!;

        OutgoingPaymentWithSpentAmountsResponse response;
        if (raw["quoteId"] is not null)
        {
            var quoteId = (Uri)raw["quoteId"]!;
            var quote = _quotes[quoteId.ToString()];
            response = new OutgoingPaymentWithSpentAmountsResponse
            {
                Id = new Uri(BaseAddress, $"outgoing-payments/{NewId()}"),
                WalletAddress = walletAddress,
                QuoteId = quote.Id,
                Receiver = quote.Receiver,
                DebitAmount = quote.DebitAmount,
                ReceiveAmount = quote.ReceiveAmount,
                SentAmount = new Amount("0", quote.DebitAmount.AssetCode, quote.DebitAmount.AssetScale),
                Failed = false,
                CreatedAt = DateTimeOffset.UtcNow,
                GrantSpentDebitAmount = quote.DebitAmount,
                GrantSpentReceiveAmount = quote.ReceiveAmount,
            };
        }
        else
        {
            var incomingPaymentId = (Uri)raw["incomingPayment"]!;
            var incomingPayment = _incomingPayments[incomingPaymentId.ToString()];
            var debitAmount = raw["debitAmount"]!.ToObject<Amount>()!;

            response = new OutgoingPaymentWithSpentAmountsResponse
            {
                Id = new Uri(BaseAddress, $"outgoing-payments/{NewId()}"),
                WalletAddress = walletAddress,
                QuoteId = null,
                Receiver = incomingPayment.Id,
                DebitAmount = debitAmount,
                ReceiveAmount = debitAmount,
                SentAmount = new Amount("0", debitAmount.AssetCode, debitAmount.AssetScale),
                Failed = false,
                CreatedAt = DateTimeOffset.UtcNow,
                GrantSpentDebitAmount = debitAmount,
                GrantSpentReceiveAmount = debitAmount,
            };
        }

        _outgoingPayments[response.Id.ToString()] = response;
        await WriteJsonAsync(context.Response, StatusCodes.Status201Created, response);
    }
}
```

- [ ] **Step 7: Create the shared per-test harness base class**

Create `OpenPayments.Snippets.Tests/Infrastructure/GuideTestBase.cs`:

```csharp
using Interledger.OpenPayments.Clients;
using Interledger.OpenPayments.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NSec.Cryptography;

namespace OpenPayments.Snippets.Tests.Infrastructure;

/// <summary>
/// Base for guide end-to-end tests. xunit creates a new instance of the test class per
/// <c>[Fact]</c>, so the fresh <see cref="FakeOpenPaymentsServer"/> and
/// <see cref="IAuthenticatedClient"/> built in this constructor are never shared across tests
/// — there is no <c>ICollectionFixture</c>/<c>IClassFixture</c> involved.
/// </summary>
public abstract class GuideTestBase : IDisposable
{
    private readonly ServiceProvider _provider;

    protected FakeOpenPaymentsServer Server { get; }
    protected IAuthenticatedClient Client { get; }

    protected GuideTestBase()
    {
        Server = new FakeOpenPaymentsServer();

        var services = new ServiceCollection();
        services.UseOpenPayments(options =>
        {
            options.UseAuthenticatedClient = true;
            options.KeyId = "guide-test-key";
            options.PrivateKey = Key.Create(SignatureAlgorithm.Ed25519);
            options.ClientUrl = new Uri("https://client.example.com/test");
        });

        // Appends to (does not replace) the "authenticated" client UseOpenPayments already
        // registered: SigningHttpMessageHandler stays outermost, this routing handler sits
        // just inside it, and the primary handler is swapped for the fake TestServer's.
        services
            .AddHttpClient("authenticated")
            .AddHttpMessageHandler(() => new TestServerRoutingHandler(Server.BaseAddress))
            .ConfigurePrimaryHttpMessageHandler(() => Server.CreateHandler());

        _provider = services.BuildServiceProvider();
        Client = _provider.GetRequiredService<IAuthenticatedClient>();
    }

    public void Dispose()
    {
        _provider.Dispose();
        Server.Dispose();
    }
}
```

- [ ] **Step 8: Build and run the test, confirm it passes**

Run:
```
dotnet build OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj
dotnet test OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj --filter "FullyQualifiedName~Guides.OneTimePayment_Tests"
```
Expected: build succeeds with zero warnings; the test PASSES.

If it fails, the likely culprits in order of likelihood: (a) route ordering/matching in `FakeOpenPaymentsServer` (add a temporary breakpoint or `Console.WriteLine` of `context.Request.Path`/`Method` in a handler to check what's actually being hit), (b) handler pipeline ordering (confirm `Signature`/`Signature-Input` headers exist on requests reaching the fake server by temporarily asserting `context.Request.Headers.ContainsKey("Signature")` in `HandleCreateGrant`), (c) a JSON property name mismatch between what a guide sends and what the raw-`JObject` handlers read.

- [ ] **Step 9: Commit**

```bash
git add Directory.Packages.props OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj OpenPayments.Snippets.Tests/Infrastructure OpenPayments.Snippets.Tests/Guides/OneTimePayment_Tests.cs
git commit -m "test: add fake Open Payments server infrastructure and OneTimePayment guide test"
```

---

### Task 2: SendRemittanceWithFixedDebit guide test

**Files:**
- Create: `OpenPayments.Snippets.Tests/Guides/SendRemittanceWithFixedDebit_Tests.cs`

**Interfaces:**
- Consumes: `GuideTestBase` (`Server`, `Client`) from Task 1; `OpenPayments.Snippets.Guides.SendRemittanceWithFixedDebit`.

- [ ] **Step 1: Write the test**

Create `OpenPayments.Snippets.Tests/Guides/SendRemittanceWithFixedDebit_Tests.cs`:

```csharp
using AwesomeAssertions;
using OpenPayments.Snippets.Guides;
using OpenPayments.Snippets.Tests.Infrastructure;

namespace OpenPayments.Snippets.Tests.Guides;

public class SendRemittanceWithFixedDebit_Tests : GuideTestBase
{
    [Fact]
    public async Task Run_CreatesOutgoingPaymentWithFixedDebitAmount()
    {
        await new SendRemittanceWithFixedDebit(Client).Run();

        Server.IncomingPayments.Should().ContainSingle();
        var incomingPayment = Server.IncomingPayments.Values.Single();

        Server.Quotes.Should().ContainSingle();
        var quote = Server.Quotes.Values.Single();
        quote.Receiver.Should().Be(incomingPayment.Id);
        quote.DebitAmount.Value.Should().Be("10000");
        quote.DebitAmount.AssetCode.Should().Be("USD");

        Server.OutgoingPayments.Should().ContainSingle();
        var outgoingPayment = Server.OutgoingPayments.Values.Single();
        outgoingPayment.QuoteId.Should().Be(quote.Id);
        outgoingPayment.DebitAmount.Value.Should().Be("10000");
    }
}
```

- [ ] **Step 2: Run and confirm it passes**

Run: `dotnet test OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj --filter "FullyQualifiedName~Guides.SendRemittanceWithFixedDebit_Tests"`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add OpenPayments.Snippets.Tests/Guides/SendRemittanceWithFixedDebit_Tests.cs
git commit -m "test: add SendRemittanceWithFixedDebit guide test"
```

---

### Task 3: SendRemittanceWithFixedReceive guide test

**Files:**
- Create: `OpenPayments.Snippets.Tests/Guides/SendRemittanceWithFixedReceive_Tests.cs`

**Interfaces:**
- Consumes: `GuideTestBase` from Task 1; `OpenPayments.Snippets.Guides.SendRemittanceWithFixedReceive`.

- [ ] **Step 1: Write the test**

Create `OpenPayments.Snippets.Tests/Guides/SendRemittanceWithFixedReceive_Tests.cs`:

```csharp
using AwesomeAssertions;
using OpenPayments.Snippets.Guides;
using OpenPayments.Snippets.Tests.Infrastructure;

namespace OpenPayments.Snippets.Tests.Guides;

public class SendRemittanceWithFixedReceive_Tests : GuideTestBase
{
    [Fact]
    public async Task Run_CreatesOutgoingPaymentWithFixedReceiveAmount()
    {
        await new SendRemittanceWithFixedReceive(Client).Run();

        Server.IncomingPayments.Should().ContainSingle();
        var incomingPayment = Server.IncomingPayments.Values.Single();

        Server.Quotes.Should().ContainSingle();
        var quote = Server.Quotes.Values.Single();
        quote.Receiver.Should().Be(incomingPayment.Id);
        quote.ReceiveAmount.Value.Should().Be("500000");
        quote.ReceiveAmount.AssetCode.Should().Be("MXN");

        Server.OutgoingPayments.Should().ContainSingle();
        var outgoingPayment = Server.OutgoingPayments.Values.Single();
        outgoingPayment.QuoteId.Should().Be(quote.Id);
        outgoingPayment.ReceiveAmount.Value.Should().Be("500000");
    }
}
```

- [ ] **Step 2: Run and confirm it passes**

Run: `dotnet test OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj --filter "FullyQualifiedName~Guides.SendRemittanceWithFixedReceive_Tests"`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add OpenPayments.Snippets.Tests/Guides/SendRemittanceWithFixedReceive_Tests.cs
git commit -m "test: add SendRemittanceWithFixedReceive guide test"
```

---

### Task 4: SetupRecurringRemittanceWithFixedIncoming guide test

**Files:**
- Create: `OpenPayments.Snippets.Tests/Guides/SetupRecurringRemittanceWithFixedIncoming_Tests.cs`

**Interfaces:**
- Consumes: `GuideTestBase` from Task 1; `OpenPayments.Snippets.Guides.SetupRemittanceWithFixedIncoming` (the guide class's actual name — note it differs from the file name `4_SetupRecurringRemittanceWithFixedIncoming.cs`).

- [ ] **Step 1: Write the test**

Create `OpenPayments.Snippets.Tests/Guides/SetupRecurringRemittanceWithFixedIncoming_Tests.cs`:

```csharp
using AwesomeAssertions;
using OpenPayments.Snippets.Guides;
using OpenPayments.Snippets.Tests.Infrastructure;

namespace OpenPayments.Snippets.Tests.Guides;

public class SetupRecurringRemittanceWithFixedIncoming_Tests : GuideTestBase
{
    [Fact]
    public async Task Run_CreatesOutgoingPaymentForFixedIncomingAmount()
    {
        await new SetupRemittanceWithFixedIncoming(Client).Run();

        Server.IncomingPayments.Should().ContainSingle();
        var incomingPayment = Server.IncomingPayments.Values.Single();
        incomingPayment.IncomingAmount!.Value.Should().Be("1500");
        incomingPayment.IncomingAmount.AssetCode.Should().Be("USD");

        Server.Quotes.Should().ContainSingle();
        var quote = Server.Quotes.Values.Single();
        quote.Receiver.Should().Be(incomingPayment.Id);
        quote.DebitAmount.Value.Should().Be("1500");

        Server.OutgoingPayments.Should().ContainSingle();
        var outgoingPayment = Server.OutgoingPayments.Values.Single();
        outgoingPayment.QuoteId.Should().Be(quote.Id);
    }
}
```

- [ ] **Step 2: Run and confirm it passes**

Run: `dotnet test OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj --filter "FullyQualifiedName~Guides.SetupRecurringRemittanceWithFixedIncoming_Tests"`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add OpenPayments.Snippets.Tests/Guides/SetupRecurringRemittanceWithFixedIncoming_Tests.cs
git commit -m "test: add SetupRecurringRemittanceWithFixedIncoming guide test"
```

---

### Task 5: SendRecurringRemittanceWithFixedDebit guide test

**Files:**
- Create: `OpenPayments.Snippets.Tests/Guides/SendRecurringRemittanceWithFixedDebit_Tests.cs`

**Interfaces:**
- Consumes: `GuideTestBase` from Task 1; `OpenPayments.Snippets.Guides.SendRecurringRemittanceWithFixedDebit`.

- [ ] **Step 1: Write the test**

This guide skips the quote step entirely — it builds the outgoing payment directly `OutgoingPaymentBodyFromIncomingPayment`.

Create `OpenPayments.Snippets.Tests/Guides/SendRecurringRemittanceWithFixedDebit_Tests.cs`:

```csharp
using AwesomeAssertions;
using OpenPayments.Snippets.Guides;
using OpenPayments.Snippets.Tests.Infrastructure;

namespace OpenPayments.Snippets.Tests.Guides;

public class SendRecurringRemittanceWithFixedDebit_Tests : GuideTestBase
{
    [Fact]
    public async Task Run_CreatesOutgoingPaymentDirectlyFromIncomingPayment()
    {
        await new SendRecurringRemittanceWithFixedDebit(Client).Run();

        Server.IncomingPayments.Should().ContainSingle();
        var incomingPayment = Server.IncomingPayments.Values.Single();

        Server.Quotes.Should().BeEmpty();

        Server.OutgoingPayments.Should().ContainSingle();
        var outgoingPayment = Server.OutgoingPayments.Values.Single();
        outgoingPayment.QuoteId.Should().BeNull();
        outgoingPayment.Receiver.Should().Be(incomingPayment.Id);
        outgoingPayment.DebitAmount.Value.Should().Be("20000");
        outgoingPayment.DebitAmount.AssetCode.Should().Be("USD");
    }
}
```

- [ ] **Step 2: Run and confirm it passes**

Run: `dotnet test OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj --filter "FullyQualifiedName~Guides.SendRecurringRemittanceWithFixedDebit_Tests"`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add OpenPayments.Snippets.Tests/Guides/SendRecurringRemittanceWithFixedDebit_Tests.cs
git commit -m "test: add SendRecurringRemittanceWithFixedDebit guide test"
```

---

### Task 6: SendRecurringRemittanceWithFixedReceive guide test

**Files:**
- Create: `OpenPayments.Snippets.Tests/Guides/SendRecurringRemittanceWithFixedReceive_Tests.cs`

**Interfaces:**
- Consumes: `GuideTestBase` from Task 1; `OpenPayments.Snippets.Guides.SendRecurringRemittanceWithFixedReceive`.

- [ ] **Step 1: Write the test**

This guide requests the interactive outgoing-payment grant *before* the incoming-payment grant (reversed order vs. the other guides) — the fake server's stateless-per-request handling doesn't care about call order, only about resource linkage, so no special handling is needed.

Create `OpenPayments.Snippets.Tests/Guides/SendRecurringRemittanceWithFixedReceive_Tests.cs`:

```csharp
using AwesomeAssertions;
using OpenPayments.Snippets.Guides;
using OpenPayments.Snippets.Tests.Infrastructure;

namespace OpenPayments.Snippets.Tests.Guides;

public class SendRecurringRemittanceWithFixedReceive_Tests : GuideTestBase
{
    [Fact]
    public async Task Run_CreatesOutgoingPaymentWithFixedReceiveAmount()
    {
        await new SendRecurringRemittanceWithFixedReceive(Client).Run();

        Server.IncomingPayments.Should().ContainSingle();
        var incomingPayment = Server.IncomingPayments.Values.Single();

        Server.Quotes.Should().ContainSingle();
        var quote = Server.Quotes.Values.Single();
        quote.Receiver.Should().Be(incomingPayment.Id);
        quote.ReceiveAmount.Value.Should().Be("400000");
        quote.ReceiveAmount.AssetCode.Should().Be("MXN");

        Server.OutgoingPayments.Should().ContainSingle();
        var outgoingPayment = Server.OutgoingPayments.Values.Single();
        outgoingPayment.QuoteId.Should().Be(quote.Id);
    }
}
```

- [ ] **Step 2: Run and confirm it passes**

Run: `dotnet test OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj --filter "FullyQualifiedName~Guides.SendRecurringRemittanceWithFixedReceive_Tests"`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add OpenPayments.Snippets.Tests/Guides/SendRecurringRemittanceWithFixedReceive_Tests.cs
git commit -m "test: add SendRecurringRemittanceWithFixedReceive guide test"
```

---

### Task 7: SplitIncomingPayment guide test

**Files:**
- Create: `OpenPayments.Snippets.Tests/Guides/SplitIncomingPayment_Tests.cs`

**Interfaces:**
- Consumes: `GuideTestBase` from Task 1; `OpenPayments.Snippets.Guides.SplitIncomingPayment`.

- [ ] **Step 1: Write the test**

This guide creates two incoming payments (merchant, platform), two quotes, and two outgoing payments — assertions must disambiguate the pair by amount/linkage rather than assuming dictionary order.

Create `OpenPayments.Snippets.Tests/Guides/SplitIncomingPayment_Tests.cs`:

```csharp
using AwesomeAssertions;
using OpenPayments.Snippets.Guides;
using OpenPayments.Snippets.Tests.Infrastructure;

namespace OpenPayments.Snippets.Tests.Guides;

public class SplitIncomingPayment_Tests : GuideTestBase
{
    [Fact]
    public async Task Run_CreatesTwoLinkedOutgoingPaymentsForMerchantAndPlatform()
    {
        await new SplitIncomingPayment(Client).Run();

        Server.IncomingPayments.Should().HaveCount(2);
        Server.Quotes.Should().HaveCount(2);
        Server.OutgoingPayments.Should().HaveCount(2);

        var merchantIncomingPayment = Server.IncomingPayments.Values.Single(p => p.IncomingAmount!.Value == "9900");
        var platformIncomingPayment = Server.IncomingPayments.Values.Single(p => p.IncomingAmount!.Value == "100");

        var merchantQuote = Server.Quotes.Values.Single(q => q.Receiver == merchantIncomingPayment.Id);
        var platformQuote = Server.Quotes.Values.Single(q => q.Receiver == platformIncomingPayment.Id);

        Server.OutgoingPayments.Values.Should().Contain(p => p.QuoteId == merchantQuote.Id);
        Server.OutgoingPayments.Values.Should().Contain(p => p.QuoteId == platformQuote.Id);
    }
}
```

- [ ] **Step 2: Run and confirm it passes**

Run: `dotnet test OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj --filter "FullyQualifiedName~Guides.SplitIncomingPayment_Tests"`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add OpenPayments.Snippets.Tests/Guides/SplitIncomingPayment_Tests.cs
git commit -m "test: add SplitIncomingPayment guide test"
```

---

### Task 8: GetGrantForFuturePayments guide test

**Files:**
- Create: `OpenPayments.Snippets.Tests/Guides/GetGrantForFuturePayments_Tests.cs`

**Interfaces:**
- Consumes: `GuideTestBase` from Task 1 (specifically `Server.IssuedAccessTokens`); `OpenPayments.Snippets.Guides.GetGrantForFuturePayments`.

- [ ] **Step 1: Write the test**

This guide is the only one that creates no incoming payment, quote, or outgoing payment — it just walks the interactive grant flow to completion and stops. The only observable proof the guide didn't silently no-op is that the fake server actually issued a final access token via the continuation endpoint.

Create `OpenPayments.Snippets.Tests/Guides/GetGrantForFuturePayments_Tests.cs`:

```csharp
using AwesomeAssertions;
using OpenPayments.Snippets.Guides;
using OpenPayments.Snippets.Tests.Infrastructure;

namespace OpenPayments.Snippets.Tests.Guides;

public class GetGrantForFuturePayments_Tests : GuideTestBase
{
    [Fact]
    public async Task Run_CompletesInteractiveGrantAndObtainsAccessToken()
    {
        await new GetGrantForFuturePayments(Client).Run();

        Server.IssuedAccessTokens.Should().ContainSingle();
        Server.IncomingPayments.Should().BeEmpty();
        Server.Quotes.Should().BeEmpty();
        Server.OutgoingPayments.Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Run and confirm it passes**

Run: `dotnet test OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj --filter "FullyQualifiedName~Guides.GetGrantForFuturePayments_Tests"`
Expected: PASS.

- [ ] **Step 3: Run the full suite**

Run: `dotnet test OpenPayments.Snippets.Tests/OpenPayments.Snippets.Tests.csproj`
Expected: all tests PASS, including the pre-existing `Services/GnapInteractionHash_Tests.cs` and `Services/GrantInteractionListener_Tests.cs`.

- [ ] **Step 4: Commit**

```bash
git add OpenPayments.Snippets.Tests/Guides/GetGrantForFuturePayments_Tests.cs
git commit -m "test: add GetGrantForFuturePayments guide test"
```

---

## Self-Review Notes

- **Spec coverage:** all 8 guides get a dedicated test class matching the design doc's naming; the fake server implements all 6 operations the design doc lists (`GetWalletAddress`, `RequestGrant`, `ContinueGrant`, `CreateIncomingPayment`, `CreateQuote`, `CreateOutgoingPayment`); token-based 401 rejection is implemented on every authenticated endpoint; each test builds a fresh server/client pair (no shared fixture); no new CI wiring needed since `dotnet test` at the solution level already picks up the project.
- **Deviations from the design doc, and why:** (1) `AwesomeAssertions` instead of `FluentAssertions` — this repo already standardized on the former as the actual dependency; using the doc's literal name would reference a package this repo doesn't use anywhere else. (2) Raw `TestServer` instead of subclassing `WebApplicationFactory<TEntryPoint>` — there's no existing entry-point `Program` class to point a factory at, and the doc names `TestServer` as an equivalent alternative in the same sentence. Both are called out explicitly in Global Constraints.
- **Type consistency:** `FakeOpenPaymentsServer`'s public surface (`Server`, `IncomingPayments`, `Quotes`, `OutgoingPayments`, `IssuedAccessTokens`, `BaseAddress`, `CreateHandler()`) is defined once in Task 1 and used identically (same names/types) across Tasks 2–8; `GuideTestBase`'s `Server`/`Client` protected properties are likewise consumed identically by every guide test class.

# Phase 4 — Auto-Paging & Serialization Consistency Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the two feature improvements from `IMPROVEMENTS.md`: first-class pagination — `IAsyncEnumerable`-returning `List*AllAsync` overloads that follow `pageInfo` cursors automatically (#8) — and consistent serialization via one shared lenient contract resolver plus a written System.Text.Json migration plan (#9).

**Architecture:**
- #8 adds `ListIncomingPaymentsAllAsync` / `ListOutgoingPaymentsAllAsync` at both layers (`IResourceClientBase`/`ResourceClientBase` where the loop lives, and `IAuthenticatedClient`/`AuthenticatedClient` as thin delegations), alongside — never replacing — the page-at-a-time methods. The loop starts from the caller's `query.Cursor`, requests pages with `first` preserved, and advances on `pagination.endCursor` while `hasNextPage` is true. Backward paging (`Last`) is rejected up front; a repeated cursor from the server throws instead of looping forever.
- #9 replaces the two divergent resolvers (`AuthContractResolver` relaxes `Required.Always` + ignores nulls; `ResourceContractResolver` is an empty pass-through, so resource responses hard-fail on spec-vs-server drift) with one public `OpenPaymentsContractResolver` carrying the lenient behavior, used by all three generated clients *and* `UnauthenticatedClient`'s raw deserialization. The longer-term System.Text.Json migration is deliberately **not** implemented here — it is captured as an ADR with a phased plan.

**Tech Stack:** .NET (`net8.0;net9.0` targets), C# 12 async iterators (`IAsyncEnumerable`, `[EnumeratorCancellation]`), Newtonsoft.Json 13.0.3, xUnit + FluentAssertions + Moq.

## Global Constraints

- **Prerequisite:** Phases 1–3 are fully executed. In particular: namespaces are `Interledger.OpenPayments.*`; `ResourceServerClient.List*Async` take `(Uri baseUri, string accessToken, string walletAddress, string? cursor, int? first, int? last, CancellationToken)` (Phase 1); the outgoing list methods are named `ListOutgoingPaymentsAsync` (Phase 3 Task 7); warnings are errors and the public API is tracked by PublicApiAnalyzers (Phase 3), so **every task that changes the public surface must update `OpenPayments.Sdk/PublicAPI.Unshipped.txt`** (run `dotnet format analyzers OpenPayments.sln --diagnostics RS0016 --severity warn`, or edit the file per the RS0016/RS0017 build errors).
- The generated pagination contract (verified in the committed types): `ListIncomingPaymentsResponse : Response` with `PageInfo? Pagination` and `ICollection<IncomingPayment>? Result`; `ListOutgoingPaymentsResponse : Response2` with `PageInfo? Pagination` and `ICollection<OutgoingPayment>? Result`; `PageInfo` has `string? StartCursor`, `string? EndCursor`, `bool HasNextPage`, `bool HasPreviousPage`.
- Do not modify `.g.cs` files or the page-at-a-time list methods' behavior.
- After every task: `dotnet build --configuration Release` (zero warnings) and both test suites green.

---

### Task 1: `ListIncomingPaymentsAllAsync` on `ResourceClientBase`

**Files:**
- Create: `OpenPayments.Sdk.Tests/Clients/ResourceClientBase_PagingTests.cs`
- Modify: `OpenPayments.Sdk/Clients/ResourceClientBase.cs` (class + `IResourceClientBase`)
- Modify: `OpenPayments.Sdk/PublicAPI.Unshipped.txt`

**Interfaces:**
- Consumes: `ResourceServerClient.ListIncomingPaymentsAsync(Uri baseUri, string accessToken, string walletAddress, string? cursor, int? first, int? last, CancellationToken)` (Phase 1), `AuthRequestArgs { Uri Url; string AccessToken; }`, `ListIncomingPaymentQuery { string WalletAddress; string? Cursor; int? First; int? Last; }`.
- Produces: `IResourceClientBase.ListIncomingPaymentsAllAsync(AuthRequestArgs requestArgs, ListIncomingPaymentQuery query, CancellationToken cancellationToken = default) : IAsyncEnumerable<IncomingPayment>` — Task 2 delegates to it. Semantics: starts at `query.Cursor`, keeps `query.First` as the per-page size, throws `ArgumentException` if `query.Last` is set, throws `InvalidOperationException` on a repeated server cursor.

- [ ] **Step 1: Write the failing tests**

Create `OpenPayments.Sdk.Tests/Clients/ResourceClientBase_PagingTests.cs`:

```csharp
using System.Net;
using System.Text;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Interledger.OpenPayments.Clients;
using Interledger.OpenPayments.Generated.Resource;

namespace Interledger.OpenPayments.Tests.Clients;

public class ResourceClientBase_PagingTests
{
    internal static IncomingPayment MakePayment(int i) =>
        new()
        {
            Id = new Uri($"https://host-a.example/incoming-payments/{i}"),
            WalletAddress = new Uri("https://host-a.example/alice"),
            ReceivedAmount = new Amount("0", "EUR", 2),
            Completed = false,
            CreatedAt = DateTime.UtcNow,
        };

    internal static string? GetQueryValue(Uri uri, string name) =>
        uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .Where(parts => parts[0] == name)
            .Select(parts => Uri.UnescapeDataString(parts[1]))
            .FirstOrDefault();

    internal static (HttpClient Client, List<Uri> Requests) CreateClient(
        Func<string?, object> pageForCursor
    )
    {
        var requests = new List<Uri>();
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns<HttpRequestMessage, CancellationToken>(
                (request, _) =>
                {
                    lock (requests)
                        requests.Add(request.RequestUri!);

                    var page = pageForCursor(GetQueryValue(request.RequestUri!, "cursor"));

                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                JsonConvert.SerializeObject(page),
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }
            );

        return (new HttpClient(handler.Object), requests);
    }

    private static (HttpClient Client, List<Uri> Requests) CreateTwoPageClient() =>
        CreateClient(cursor =>
            cursor switch
            {
                null => new ListIncomingPaymentsResponse
                {
                    Result = [MakePayment(1), MakePayment(2)],
                    Pagination = new PageInfo
                    {
                        EndCursor = "cursor-1",
                        HasNextPage = true,
                        HasPreviousPage = false,
                    },
                },
                "cursor-1" => new ListIncomingPaymentsResponse
                {
                    Result = [MakePayment(3)],
                    Pagination = new PageInfo
                    {
                        StartCursor = "cursor-1",
                        EndCursor = "cursor-2",
                        HasNextPage = false,
                        HasPreviousPage = true,
                    },
                },
                _ => throw new InvalidOperationException($"Unexpected cursor: {cursor}"),
            }
        );

    private static AuthRequestArgs Args() =>
        new() { Url = new Uri("https://host-a.example/"), AccessToken = "token" };

    [Fact]
    public async Task ListIncomingPaymentsAllAsync_FollowsCursorsAcrossAllPages()
    {
        var (httpClient, requests) = CreateTwoPageClient();
        var client = new ResourceClientBase(httpClient, new Uri("https://client.example"));

        var payments = new List<IncomingPayment>();
        await foreach (
            var payment in client.ListIncomingPaymentsAllAsync(
                Args(),
                new ListIncomingPaymentQuery { WalletAddress = "https://host-a.example/alice" }
            )
        )
        {
            payments.Add(payment);
        }

        payments
            .Select(p => p.Id.ToString())
            .Should()
            .Equal(
                "https://host-a.example/incoming-payments/1",
                "https://host-a.example/incoming-payments/2",
                "https://host-a.example/incoming-payments/3"
            );

        requests.Should().HaveCount(2);
        GetQueryValue(requests[0], "cursor").Should().BeNull();
        GetQueryValue(requests[1], "cursor").Should().Be("cursor-1");
    }

    [Fact]
    public async Task ListIncomingPaymentsAllAsync_StartsFromCallerCursorAndKeepsFirst()
    {
        var (httpClient, requests) = CreateTwoPageClient();
        var client = new ResourceClientBase(httpClient, new Uri("https://client.example"));

        var payments = new List<IncomingPayment>();
        await foreach (
            var payment in client.ListIncomingPaymentsAllAsync(
                Args(),
                new ListIncomingPaymentQuery
                {
                    WalletAddress = "https://host-a.example/alice",
                    Cursor = "cursor-1",
                    First = 25,
                }
            )
        )
        {
            payments.Add(payment);
        }

        payments.Should().HaveCount(1);
        requests.Should().HaveCount(1);
        GetQueryValue(requests[0], "cursor").Should().Be("cursor-1");
        GetQueryValue(requests[0], "first").Should().Be("25");
    }

    [Fact]
    public async Task ListIncomingPaymentsAllAsync_LastSet_ThrowsArgumentException()
    {
        var (httpClient, requests) = CreateTwoPageClient();
        var client = new ResourceClientBase(httpClient, new Uri("https://client.example"));

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (
                var _ in client.ListIncomingPaymentsAllAsync(
                    Args(),
                    new ListIncomingPaymentQuery
                    {
                        WalletAddress = "https://host-a.example/alice",
                        Last = 5,
                    }
                )
            ) { }
        });

        requests.Should().BeEmpty();
    }

    [Fact]
    public async Task ListIncomingPaymentsAllAsync_ServerRepeatsCursor_Throws()
    {
        var (httpClient, _) = CreateClient(_ => new ListIncomingPaymentsResponse
        {
            Result = [MakePayment(1)],
            Pagination = new PageInfo
            {
                EndCursor = "stuck",
                HasNextPage = true,
                HasPreviousPage = false,
            },
        });
        var client = new ResourceClientBase(httpClient, new Uri("https://client.example"));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (
                var _ in client.ListIncomingPaymentsAllAsync(
                    Args(),
                    new ListIncomingPaymentQuery { WalletAddress = "https://host-a.example/alice" }
                )
            ) { }
        });
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj --filter FullyQualifiedName~ResourceClientBase_PagingTests`
Expected: FAIL to compile — `ListIncomingPaymentsAllAsync` does not exist.

- [ ] **Step 3: Implement the auto-pager**

In `OpenPayments.Sdk/Clients/ResourceClientBase.cs`, add to the top of the file:

```csharp
using System.Runtime.CompilerServices;
```

Add this method to the `ResourceClientBase` class, directly after `ListIncomingPaymentsAsync`:

```csharp
    /// <inheritdoc/>
    public async IAsyncEnumerable<IncomingPayment> ListIncomingPaymentsAllAsync(
        AuthRequestArgs requestArgs,
        ListIncomingPaymentQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        if (query.Last is not null)
            throw new ArgumentException(
                "Backward paging (Last) is not supported by auto-paging; use ListIncomingPaymentsAsync for page-at-a-time access.",
                nameof(query)
            );

        var cursor = query.Cursor;
        while (true)
        {
            var page = await _client
                .ListIncomingPaymentsAsync(
                    requestArgs.Url,
                    requestArgs.AccessToken,
                    query.WalletAddress,
                    cursor,
                    query.First,
                    null,
                    cancellationToken
                )
                .ConfigureAwait(false);

            foreach (var payment in page.Result ?? [])
                yield return payment;

            if (
                page.Pagination is not { HasNextPage: true } pageInfo
                || string.IsNullOrEmpty(pageInfo.EndCursor)
            )
            {
                yield break;
            }

            if (pageInfo.EndCursor == cursor)
                throw new InvalidOperationException(
                    "The server returned the same pagination cursor twice; aborting to avoid an infinite paging loop."
                );

            cursor = pageInfo.EndCursor;
        }
    }
```

Add the member to `IResourceClientBase` (directly after its `ListIncomingPaymentsAsync` declaration):

```csharp
    /// <summary>
    /// Enumerates <b>all</b> incoming payments on a wallet address, transparently following
    /// <c>pageInfo</c> cursors across pages. <see cref="ListIncomingPaymentQuery.First"/> sets the
    /// per-page size and <see cref="ListIncomingPaymentQuery.Cursor"/> the starting position;
    /// <see cref="ListIncomingPaymentQuery.Last"/> must be unset (backward paging is not supported —
    /// use <see cref="ListIncomingPaymentsAsync"/> instead).
    /// </summary>
    /// <param name="requestArgs">Resource server URL and access token.</param>
    /// <param name="query">Wallet address filter, page size, and optional starting cursor.</param>
    /// <param name="cancellationToken">Optional cancellation token, observed between and during page requests.</param>
    public IAsyncEnumerable<IncomingPayment> ListIncomingPaymentsAllAsync(
        AuthRequestArgs requestArgs,
        ListIncomingPaymentQuery query,
        CancellationToken cancellationToken = default
    );
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj --filter FullyQualifiedName~ResourceClientBase_PagingTests`
Expected: all 4 PASS.

- [ ] **Step 5: Record the new public API and build clean**

Run: `dotnet format analyzers OpenPayments.sln --diagnostics RS0016 --severity warn` (or add the RS0016-reported lines to `OpenPayments.Sdk/PublicAPI.Unshipped.txt` by hand).

Run: `dotnet build --configuration Release && dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj`
Expected: green.

- [ ] **Step 6: Commit**

```bash
git add OpenPayments.Sdk.Tests/Clients/ResourceClientBase_PagingTests.cs OpenPayments.Sdk/Clients/ResourceClientBase.cs OpenPayments.Sdk/PublicAPI.Unshipped.txt
git commit -m "feat(paging): auto-paging ListIncomingPaymentsAllAsync on ResourceClientBase"
```

---

### Task 2: Expose incoming-payment auto-paging on `IAuthenticatedClient`

**Files:**
- Create: `OpenPayments.Sdk.Tests/Clients/AuthenticatedClient_PagingTests.cs`
- Modify: `OpenPayments.Sdk/Clients/IAuthenticatedClient.cs`, `OpenPayments.Sdk/Clients/AuthenticatedClient.cs`
- Modify: `OpenPayments.Sdk/PublicAPI.Unshipped.txt`

**Interfaces:**
- Consumes: `IResourceClientBase.ListIncomingPaymentsAllAsync` (Task 1); the test helpers `ResourceClientBase_PagingTests.MakePayment` / `.GetQueryValue` / `.CreateClient` (Task 1, `internal static`).
- Produces: `IAuthenticatedClient.ListIncomingPaymentsAllAsync(AuthRequestArgs, ListIncomingPaymentQuery, CancellationToken = default) : IAsyncEnumerable<IncomingPayment>` — the consumer-facing entry point.

- [ ] **Step 1: Write the failing test**

Create `OpenPayments.Sdk.Tests/Clients/AuthenticatedClient_PagingTests.cs`:

```csharp
using FluentAssertions;
using Interledger.OpenPayments.Clients;
using Interledger.OpenPayments.Generated.Resource;

namespace Interledger.OpenPayments.Tests.Clients;

public class AuthenticatedClient_PagingTests
{
    [Fact]
    public async Task ListIncomingPaymentsAllAsync_EnumeratesAcrossPages()
    {
        var (httpClient, requests) = ResourceClientBase_PagingTests.CreateClient(cursor =>
            cursor switch
            {
                null => new ListIncomingPaymentsResponse
                {
                    Result =
                    [
                        ResourceClientBase_PagingTests.MakePayment(1),
                        ResourceClientBase_PagingTests.MakePayment(2),
                    ],
                    Pagination = new PageInfo
                    {
                        EndCursor = "cursor-1",
                        HasNextPage = true,
                        HasPreviousPage = false,
                    },
                },
                _ => new ListIncomingPaymentsResponse
                {
                    Result = [ResourceClientBase_PagingTests.MakePayment(3)],
                    Pagination = new PageInfo
                    {
                        EndCursor = "cursor-2",
                        HasNextPage = false,
                        HasPreviousPage = true,
                    },
                },
            }
        );
        var client = new AuthenticatedClient(httpClient, new Uri("https://client.example"));

        var ids = new List<string>();
        await foreach (
            var payment in client.ListIncomingPaymentsAllAsync(
                new AuthRequestArgs
                {
                    Url = new Uri("https://host-a.example/"),
                    AccessToken = "token",
                },
                new ListIncomingPaymentQuery { WalletAddress = "https://host-a.example/alice" }
            )
        )
        {
            ids.Add(payment.Id.ToString());
        }

        ids.Should().HaveCount(3);
        requests.Should().HaveCount(2);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj --filter FullyQualifiedName~AuthenticatedClient_PagingTests`
Expected: FAIL to compile — `IAuthenticatedClient`/`AuthenticatedClient` have no `ListIncomingPaymentsAllAsync`.

- [ ] **Step 3: Add the member to the interface and the delegation to the class**

In `OpenPayments.Sdk/Clients/IAuthenticatedClient.cs`, add directly after `ListIncomingPaymentsAsync`:

```csharp
    /// <summary>
    /// Enumerates <b>all</b> incoming payments on a wallet address, transparently following
    /// <c>pageInfo</c> cursors across pages. <see cref="ListIncomingPaymentQuery.First"/> sets the
    /// per-page size and <see cref="ListIncomingPaymentQuery.Cursor"/> the starting position;
    /// <see cref="ListIncomingPaymentQuery.Last"/> must be unset (backward paging is not supported —
    /// use <see cref="ListIncomingPaymentsAsync"/> instead).
    /// </summary>
    /// <param name="requestArgs">Resource server URL and access token.</param>
    /// <param name="query">Wallet address filter, page size, and optional starting cursor.</param>
    /// <param name="cancellationToken">Optional cancellation token, observed between and during page requests.</param>
    public IAsyncEnumerable<IncomingPayment> ListIncomingPaymentsAllAsync(
        AuthRequestArgs requestArgs,
        ListIncomingPaymentQuery query,
        CancellationToken cancellationToken = default
    );
```

In `OpenPayments.Sdk/Clients/AuthenticatedClient.cs`, add directly after its `ListIncomingPaymentsAsync` method:

```csharp
    /// <inheritdoc/>
    public IAsyncEnumerable<IncomingPayment> ListIncomingPaymentsAllAsync(
        AuthRequestArgs requestArgs,
        ListIncomingPaymentQuery query,
        CancellationToken cancellationToken = default
    )
    {
        return _resClient.ListIncomingPaymentsAllAsync(requestArgs, query, cancellationToken);
    }
```

- [ ] **Step 4: Run the tests, record the public API, build clean**

Run: `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj --filter FullyQualifiedName~AuthenticatedClient_PagingTests`
Expected: PASS.

Run: `dotnet format analyzers OpenPayments.sln --diagnostics RS0016 --severity warn`, then `dotnet build --configuration Release && dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj`
Expected: green.

- [ ] **Step 5: Commit**

```bash
git add OpenPayments.Sdk.Tests/Clients/AuthenticatedClient_PagingTests.cs OpenPayments.Sdk/Clients/IAuthenticatedClient.cs OpenPayments.Sdk/Clients/AuthenticatedClient.cs OpenPayments.Sdk/PublicAPI.Unshipped.txt
git commit -m "feat(paging): expose ListIncomingPaymentsAllAsync on IAuthenticatedClient"
```

---

### Task 3: Auto-paging for outgoing payments (both layers)

**Files:**
- Modify: `OpenPayments.Sdk.Tests/Clients/ResourceClientBase_PagingTests.cs` (add outgoing tests)
- Modify: `OpenPayments.Sdk/Clients/ResourceClientBase.cs`, `OpenPayments.Sdk/Clients/IAuthenticatedClient.cs`, `OpenPayments.Sdk/Clients/AuthenticatedClient.cs`
- Modify: `OpenPayments.Sdk/PublicAPI.Unshipped.txt`

**Interfaces:**
- Consumes: `ResourceServerClient.ListOutgoingPaymentsAsync(Uri baseUri, string accessToken, string walletAddress, string? cursor, int? first, int? last, CancellationToken)` (Phase 1), `ListOutgoingPaymentQuery`.
- Produces: `IResourceClientBase.ListOutgoingPaymentsAllAsync(AuthRequestArgs, ListOutgoingPaymentQuery, CancellationToken = default) : IAsyncEnumerable<OutgoingPayment>` and the same member on `IAuthenticatedClient`, delegated by `AuthenticatedClient`.

- [ ] **Step 1: Write the failing test**

Add to `ResourceClientBase_PagingTests` (same class as Task 1):

```csharp
    private static OutgoingPayment MakeOutgoingPayment(int i) =>
        new()
        {
            Id = new Uri($"https://host-a.example/outgoing-payments/{i}"),
            WalletAddress = new Uri("https://host-a.example/alice"),
            Receiver = new Uri("https://host-b.example/incoming-payments/1"),
            ReceiveAmount = new Amount("100", "EUR", 2),
            DebitAmount = new Amount("101", "EUR", 2),
            SentAmount = new Amount("0", "EUR", 2),
            CreatedAt = DateTime.UtcNow,
        };

    [Fact]
    public async Task ListOutgoingPaymentsAllAsync_FollowsCursorsAcrossAllPages()
    {
        var (httpClient, requests) = CreateClient(cursor =>
            cursor switch
            {
                null => new ListOutgoingPaymentsResponse
                {
                    Result = [MakeOutgoingPayment(1), MakeOutgoingPayment(2)],
                    Pagination = new PageInfo
                    {
                        EndCursor = "cursor-1",
                        HasNextPage = true,
                        HasPreviousPage = false,
                    },
                },
                _ => new ListOutgoingPaymentsResponse
                {
                    Result = [MakeOutgoingPayment(3)],
                    Pagination = new PageInfo
                    {
                        EndCursor = "cursor-2",
                        HasNextPage = false,
                        HasPreviousPage = true,
                    },
                },
            }
        );
        var client = new ResourceClientBase(httpClient, new Uri("https://client.example"));

        var payments = new List<OutgoingPayment>();
        await foreach (
            var payment in client.ListOutgoingPaymentsAllAsync(
                Args(),
                new ListOutgoingPaymentQuery { WalletAddress = "https://host-a.example/alice" }
            )
        )
        {
            payments.Add(payment);
        }

        payments
            .Select(p => p.Id.ToString())
            .Should()
            .Equal(
                "https://host-a.example/outgoing-payments/1",
                "https://host-a.example/outgoing-payments/2",
                "https://host-a.example/outgoing-payments/3"
            );

        requests.Should().HaveCount(2);
        GetQueryValue(requests[1], "cursor").Should().Be("cursor-1");
    }

    [Fact]
    public async Task ListOutgoingPaymentsAllAsync_LastSet_ThrowsArgumentException()
    {
        var (httpClient, requests) = CreateTwoPageClient();
        var client = new ResourceClientBase(httpClient, new Uri("https://client.example"));

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await foreach (
                var _ in client.ListOutgoingPaymentsAllAsync(
                    Args(),
                    new ListOutgoingPaymentQuery
                    {
                        WalletAddress = "https://host-a.example/alice",
                        Last = 5,
                    }
                )
            ) { }
        });

        requests.Should().BeEmpty();
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj --filter FullyQualifiedName~ResourceClientBase_PagingTests`
Expected: FAIL to compile — `ListOutgoingPaymentsAllAsync` does not exist.

- [ ] **Step 3: Implement on `ResourceClientBase` + `IResourceClientBase`**

Add to the `ResourceClientBase` class, directly after `ListOutgoingPaymentsAsync`:

```csharp
    /// <inheritdoc/>
    public async IAsyncEnumerable<OutgoingPayment> ListOutgoingPaymentsAllAsync(
        AuthRequestArgs requestArgs,
        ListOutgoingPaymentQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        if (query.Last is not null)
            throw new ArgumentException(
                "Backward paging (Last) is not supported by auto-paging; use ListOutgoingPaymentsAsync for page-at-a-time access.",
                nameof(query)
            );

        var cursor = query.Cursor;
        while (true)
        {
            var page = await _client
                .ListOutgoingPaymentsAsync(
                    requestArgs.Url,
                    requestArgs.AccessToken,
                    query.WalletAddress,
                    cursor,
                    query.First,
                    null,
                    cancellationToken
                )
                .ConfigureAwait(false);

            foreach (var payment in page.Result ?? [])
                yield return payment;

            if (
                page.Pagination is not { HasNextPage: true } pageInfo
                || string.IsNullOrEmpty(pageInfo.EndCursor)
            )
            {
                yield break;
            }

            if (pageInfo.EndCursor == cursor)
                throw new InvalidOperationException(
                    "The server returned the same pagination cursor twice; aborting to avoid an infinite paging loop."
                );

            cursor = pageInfo.EndCursor;
        }
    }
```

Add to `IResourceClientBase`, directly after its `ListOutgoingPaymentsAsync` declaration:

```csharp
    /// <summary>
    /// Enumerates <b>all</b> outgoing payments on a wallet address, transparently following
    /// <c>pageInfo</c> cursors across pages. <see cref="ListOutgoingPaymentQuery.First"/> sets the
    /// per-page size and <see cref="ListOutgoingPaymentQuery.Cursor"/> the starting position;
    /// <see cref="ListOutgoingPaymentQuery.Last"/> must be unset (backward paging is not supported —
    /// use <see cref="ListOutgoingPaymentsAsync"/> instead).
    /// </summary>
    /// <param name="requestArgs">Resource server URL and access token.</param>
    /// <param name="query">Wallet address filter, page size, and optional starting cursor.</param>
    /// <param name="cancellationToken">Optional cancellation token, observed between and during page requests.</param>
    public IAsyncEnumerable<OutgoingPayment> ListOutgoingPaymentsAllAsync(
        AuthRequestArgs requestArgs,
        ListOutgoingPaymentQuery query,
        CancellationToken cancellationToken = default
    );
```

- [ ] **Step 4: Surface on `IAuthenticatedClient` + `AuthenticatedClient`**

In `OpenPayments.Sdk/Clients/IAuthenticatedClient.cs`, add directly after `ListOutgoingPaymentsAsync` the same documented declaration as in Step 3 (identical XML docs and signature, `IAsyncEnumerable<OutgoingPayment> ListOutgoingPaymentsAllAsync(AuthRequestArgs requestArgs, ListOutgoingPaymentQuery query, CancellationToken cancellationToken = default);`).

In `OpenPayments.Sdk/Clients/AuthenticatedClient.cs`, add directly after `ListOutgoingPaymentsAsync`:

```csharp
    /// <inheritdoc/>
    public IAsyncEnumerable<OutgoingPayment> ListOutgoingPaymentsAllAsync(
        AuthRequestArgs requestArgs,
        ListOutgoingPaymentQuery query,
        CancellationToken cancellationToken = default
    )
    {
        return _resClient.ListOutgoingPaymentsAllAsync(requestArgs, query, cancellationToken);
    }
```

- [ ] **Step 5: Run tests, record the public API, build clean**

Run: `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj --filter FullyQualifiedName~PagingTests`
Expected: all paging tests PASS.

Run: `dotnet format analyzers OpenPayments.sln --diagnostics RS0016 --severity warn`, then `dotnet build --configuration Release && dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj`
Expected: green.

- [ ] **Step 6: Commit**

```bash
git add OpenPayments.Sdk.Tests/Clients/ResourceClientBase_PagingTests.cs OpenPayments.Sdk/Clients/ResourceClientBase.cs OpenPayments.Sdk/Clients/IAuthenticatedClient.cs OpenPayments.Sdk/Clients/AuthenticatedClient.cs OpenPayments.Sdk/PublicAPI.Unshipped.txt
git commit -m "feat(paging): auto-paging ListOutgoingPaymentsAllAsync on both client layers"
```

---

### Task 4: One lenient contract resolver for every client

**Files:**
- Create: `OpenPayments.Sdk/Serialization/OpenPaymentsSerialization.cs`
- Create: `OpenPayments.Sdk.Tests/Clients/SerializationDrift_Tests.cs`
- Modify: `OpenPayments.Sdk/Generated/Auth/AuthServerClient.Core.cs`, `OpenPayments.Sdk/Generated/Resource/ResourceServerClient.Core.cs`, `OpenPayments.Sdk/Generated/Wallet/WalletAddressClient.Core.cs`, `OpenPayments.Sdk/Clients/UnauthenticatedClient.cs`
- Delete: `OpenPayments.Sdk/Generated/Auth/AuthContractResolver.cs`, `OpenPayments.Sdk/Generated/Resource/ResourceContractResolver.cs`
- Modify: `OpenPayments.Sdk/PublicAPI.Unshipped.txt`

**Interfaces:**
- Consumes: the Phase 2 `*.Core.cs` files (each currently holds a private `SerializerSettings` field wired to its own resolver) and `GeneratedClientBase(HttpClient, JsonSerializerSettings)`.
- Produces: `public sealed class OpenPaymentsContractResolver : DefaultContractResolver` and `public static class OpenPaymentsSerialization { public static JsonSerializerSettings DefaultSettings { get; } }` in namespace `Interledger.OpenPayments.Serialization` — used by all three generated clients and `UnauthenticatedClient`. `AuthContractResolver` and `ResourceContractResolver` are deleted (breaking, pre-1.0).

- [ ] **Step 1: Write the failing drift-tolerance test**

Create `OpenPayments.Sdk.Tests/Clients/SerializationDrift_Tests.cs`:

```csharp
using System.Net;
using System.Text;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Interledger.OpenPayments.Clients;

namespace Interledger.OpenPayments.Tests.Clients;

public class SerializationDrift_Tests
{
    // A resource-server response missing `receivedAmount`, which the generated contract
    // marks Required.Always. The lenient resolver must tolerate this drift instead of
    // failing the whole call.
    private const string DriftedIncomingPaymentJson = """
        {
          "id": "https://example.com/incoming-payments/1234",
          "walletAddress": "https://example.com/alice",
          "completed": false,
          "createdAt": "2026-01-01T00:00:00Z",
          "methods": []
        }
        """;

    private static HttpClient CreateClientReturning(string json)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                }
            );

        return new HttpClient(handler.Object);
    }

    [Fact]
    public async Task GetIncomingPaymentAsync_ResponseMissingRequiredField_StillDeserializes()
    {
        var client = new AuthenticatedClient(
            CreateClientReturning(DriftedIncomingPaymentJson),
            new Uri("https://client.example")
        );

        var result = await client.GetIncomingPaymentAsync(
            new AuthRequestArgs
            {
                Url = new Uri("https://example.com/incoming-payments/1234"),
                AccessToken = "token",
            }
        );

        result.Should().NotBeNull();
        result.Id.Should().Be(new Uri("https://example.com/incoming-payments/1234"));
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj --filter FullyQualifiedName~SerializationDrift_Tests`
Expected: FAIL — the pass-through `ResourceContractResolver` leaves `Required.Always` active, the missing `receivedAmount` raises a `JsonSerializationException`, and `ReadObjectResponseAsync` surfaces it as an `OpenPaymentsApiException` instead of returning the model.

- [ ] **Step 3: Create the shared resolver and settings**

Create `OpenPayments.Sdk/Serialization/OpenPaymentsSerialization.cs`:

```csharp
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Interledger.OpenPayments.Serialization;

/// <summary>
/// The single contract resolver used for all Open Payments payloads. It relaxes the generated
/// contracts' <see cref="Required.Always"/> constraints and ignores nulls, so minor
/// spec-vs-server drift degrades to default property values instead of failing the whole
/// call — the behavior the auth client always had, now applied uniformly to the resource
/// server, wallet address, and public incoming-payment responses too.
/// </summary>
public sealed class OpenPaymentsContractResolver : DefaultContractResolver
{
    /// <inheritdoc/>
    protected override JsonProperty CreateProperty(
        System.Reflection.MemberInfo member,
        MemberSerialization memberSerialization
    )
    {
        var property = base.CreateProperty(member, memberSerialization);
        property.Required = Required.Default;
        property.NullValueHandling = NullValueHandling.Ignore;

        return property;
    }
}

/// <summary>Shared serializer configuration for every Open Payments client.</summary>
public static class OpenPaymentsSerialization
{
    /// <summary>Serializer settings using <see cref="OpenPaymentsContractResolver"/>.</summary>
    public static JsonSerializerSettings DefaultSettings { get; } =
        new() { ContractResolver = new OpenPaymentsContractResolver() };
}
```

- [ ] **Step 4: Point all four consumers at the shared settings**

In each of the three `*.Core.cs` files (`Generated/Auth/AuthServerClient.Core.cs`, `Generated/Resource/ResourceServerClient.Core.cs`, `Generated/Wallet/WalletAddressClient.Core.cs`):
1. Add `using Interledger.OpenPayments.Serialization;` to the usings.
2. Delete the private settings field, i.e. remove (Auth shown; Resource has `ResourceContractResolver`, Wallet has a bare `new()`):
```csharp
    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        ContractResolver = new AuthContractResolver(),
    };
```
3. Change the constructor's base call from `: base(httpClient, SerializerSettings)` to `: base(httpClient, OpenPaymentsSerialization.DefaultSettings)`.
4. Remove the now-unused `using Newtonsoft.Json;` if nothing else in the file needs it.

In `OpenPayments.Sdk/Clients/UnauthenticatedClient.cs`: add `using Interledger.OpenPayments.Serialization;` and change:
```csharp
        var model = JsonConvert.DeserializeObject<PublicIncomingPayment>(json);
```
to:
```csharp
        var model = JsonConvert.DeserializeObject<PublicIncomingPayment>(
            json,
            OpenPaymentsSerialization.DefaultSettings
        );
```

Delete the two superseded resolvers:

```bash
git rm OpenPayments.Sdk/Generated/Auth/AuthContractResolver.cs OpenPayments.Sdk/Generated/Resource/ResourceContractResolver.cs
```

- [ ] **Step 5: Run the tests, reconcile the public API, build clean**

Run: `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj --filter FullyQualifiedName~SerializationDrift_Tests`
Expected: PASS.

Run: `dotnet build --configuration Release 2>&1 | grep -E "RS0016|RS0017"` — delete the `AuthContractResolver`/`ResourceContractResolver` lines from `OpenPayments.Sdk/PublicAPI.Unshipped.txt` (RS0017) and add the new `Interledger.OpenPayments.Serialization.*` symbols (RS0016; `dotnet format analyzers OpenPayments.sln --diagnostics RS0016 --severity warn` adds them).

Run: `dotnet build --configuration Release && dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj && dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj`
Expected: green — including every pre-existing success-path test (the lenient resolver only *loosens* deserialization; serialization of request bodies keeps honoring the `[JsonProperty]` names, and `NullValueHandling.Ignore` matches what the auth path already did).

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "fix(serialization): one lenient OpenPaymentsContractResolver for all clients"
```

---

### Task 5: Write the System.Text.Json migration ADR

**Files:**
- Create: `docs/adr/0001-system-text-json-migration.md`

**Interfaces:**
- Consumes: the end-state of Tasks 1–4 (single resolver, types-only generation).
- Produces: a decision record with a phased migration plan — the "plan the System.Text.Json migration" deliverable of improvement #9. No code changes.

- [ ] **Step 1: Write the ADR**

Create `docs/adr/0001-system-text-json-migration.md`:

```markdown
# ADR 0001: Migrate serialization from Newtonsoft.Json to System.Text.Json

- Status: Accepted (not yet scheduled)
- Date: 2026-07-23

## Context

The SDK serializes with Newtonsoft.Json 13, configured by a single lenient
`OpenPaymentsContractResolver` (see Phase 4). Modern .NET consumers increasingly expect
System.Text.Json (STJ): it removes a third-party dependency, and source-generated
contexts enable trimming/NativeAOT support, which Newtonsoft cannot offer.

What ties us to Newtonsoft today:

1. **Generated DTOs** carry `[Newtonsoft.Json.JsonProperty]` attributes (NSwag `/JsonLibrary` defaults).
2. **Hand-written type aliases** in `Generated/*/Types.cs` use `[JsonProperty]`, `[JsonExtensionData]`,
   `[JsonConverter(typeof(StringEnumConverter))]`, and `Required`/`NullValueHandling` settings.
3. **The lenient drift policy** is implemented as a `DefaultContractResolver` subclass.
4. **`GeneratedClientBase`** serializes request bodies and deserializes responses via `JsonConvert`.
5. **Consumers' `Metadata` fields** are `object?` and materialize as `JObject` (tests rely on this).

## Decision

Migrate to STJ with source-generated contexts in one coordinated breaking release
(pre-1.0 or a major bump), rather than dual-supporting both serializers.

## Phased plan

1. **Regenerate for STJ.** Add `/JsonLibrary:SystemTextJson` to the NSwag flags in the `Makefile`
   and regenerate. Generated DTOs switch to `[System.Text.Json.Serialization.JsonPropertyName]`,
   `[JsonExtensionData]` (STJ flavor, requires `IDictionary<string, JsonElement>` or `JsonObject`), and
   `JsonStringEnumConverter`-compatible enums. The codegen-check workflow keeps this honest.
2. **Port the hand-written type aliases.** Mechanical attribute swap in `Generated/*/Types.cs`
   (`JsonProperty` → `JsonPropertyName` + `JsonIgnore(Condition = WhenWritingNull)`;
   `StringEnumConverter` → `JsonStringEnumConverter` with `EnumMember`-value mapping via
   `JsonStringEnumMemberNameAttribute` (.NET 9) or a custom converter on net8.0).
3. **Replace the drift policy.** STJ has no contract resolver subclassing, but
   `DefaultJsonTypeInfoResolver` + type-info modifiers reproduce it: clear
   `IsRequired` on all properties and set null-ignoring defaults in one modifier —
   the direct equivalent of `OpenPaymentsContractResolver`.
4. **Swap the plumbing.** `GeneratedClientBase` moves from `JsonConvert`/`JsonSerializerSettings` to
   `System.Text.Json.JsonSerializer`/`JsonSerializerOptions`; `OpenPaymentsSerialization.DefaultSettings`
   becomes `JsonSerializerOptions DefaultOptions`.
5. **Source-generate.** Add a `JsonSerializerContext` partial listing every DTO
   (`[JsonSerializable(typeof(...))]` per root type), wire it into `DefaultOptions.TypeInfoResolver`,
   and enable `<IsAotCompatible>` + `<EnableTrimAnalyzer>` in the SDK csproj to prove it.
6. **Drop the dependency.** Remove `Newtonsoft.Json` from `Directory.Packages.props`; document the
   `Metadata` type change (`JObject` → `JsonElement`/`JsonObject`) in the CHANGELOG as breaking.

## Consequences

- Consumers reading `Metadata` as `JObject` must move to `JsonElement`/`JsonObject` (breaking).
- The lenient-drift tests from Phase 4 (`SerializationDrift_Tests`) carry over verbatim and gate step 3.
- Until scheduled, new serialization-touching code must go through `OpenPaymentsSerialization`
  so the future swap stays single-point.
```

- [ ] **Step 2: Commit**

```bash
git add docs/adr/0001-system-text-json-migration.md
git commit -m "docs: ADR for the System.Text.Json migration plan"
```

---

## Verification

After all 5 tasks:

```bash
dotnet build --configuration Release      # zero warnings
dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj
dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj
grep -rn "AuthContractResolver\|ResourceContractResolver" --include='*.cs' .   # expected: nothing
```

End-to-end (optional but recommended, per `IMPROVEMENTS.md`): run the `OpenPayments.Snippets` guides against the Interledger test wallet with a configured `example.env`, exercising a list endpoint through the new `ListIncomingPaymentsAllAsync` path.

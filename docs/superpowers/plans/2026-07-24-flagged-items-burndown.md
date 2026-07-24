# Flagged-Items Burndown Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Resolve every item flagged-and-deferred by the Phase 1–4 reviews: the two Important findings (untested signature-verification path + per-assembly coverage floor; PublicAPI tracking of the generated surface) and all outstanding Minor findings (outgoing-paging test parity, Slow test category, net8.0 runtime testing, coverage-script parse hardening, RS0026 scoping, codegen-check hand-edit gap, CHANGELOG/README gaps).

**Architecture:** Test-only tasks first (they surface one real production bug in `SignatureInputBuilder`, fixed via TDD in Task 2), then CI/packaging hardening, then docs. Each task is independently committable and leaves the build green.

**Tech Stack:** .NET 8/9, xUnit + FluentAssertions + Moq, NSec (Ed25519), PublicApiAnalyzers, ReportGenerator, GitHub Actions, NSwag 14.7.1 (pinned local tool).

## Explicitly OUT of scope (separate plans)

These appeared in the docs as open work but are whole projects, each with its own design doc — do **not** fold them in here:

- System.Text.Json migration (`docs/adr/0001-system-text-json-migration.md`)
- Spec catch-up v1.0.3 → v1.3.3 (`docs/superpowers/specs/2026-07-23-spec-update-strategy-design.md`)
- E2E guide-flow tests (`docs/superpowers/specs/2026-07-23-guide-flow-e2e-tests-design.md`)

## Global Constraints

- Branch: `cozminu/pelican`. Commit per task, conventional-commit messages. Do not push anywhere without explicit user instruction (final destination is local branch `cozmin/overhaul`, not main, not a PR).
- Never hand-edit `OpenPayments.Sdk/Generated/**/*.g.cs`.
- `dotnet build --configuration Release` must stay at 0 warnings / 0 errors after every task (`TreatWarningsAsErrors` is on).
- No task in this plan changes the public API surface; therefore no `PublicAPI.Unshipped.txt` edits are expected. If the analyzer demands one, stop and re-check the diff — something went wrong.
- Baseline test counts before this plan: 58/58 in `OpenPayments.Sdk.Tests`, 35/35 in `OpenPayments.Sdk.HttpSignatureUtils.Tests`.
- This dev machine has only the .NET 9 runtime (`dotnet --list-runtimes` shows 9.0.x only). From Task 6 onward, run tests locally with `-f net9.0`; CI is the net8.0 runtime gate.
- The two concurrency tests take ~4.5 min each. When a task's verification doesn't need them, use the `--filter` shown in that task to skip them.
- `Interledger.OpenPayments.HttpSignatureUtils.Tests` does NOT have implicit usings — every test file needs explicit `using` directives (match the existing files there).

---

### Task 1: Characterization tests for SignatureInputParser and SignatureInputValidator

Two of the four 0%-coverage classes flagged by the Phase 3 review. These tests document existing behavior — they should pass as written, no production change.

**Files:**
- Create: `OpenPayments.Sdk.HttpSignatureUtils.Tests/SignatureInputParser_Tests.cs`
- Create: `OpenPayments.Sdk.HttpSignatureUtils.Tests/SignatureInputValidator_Tests.cs`

**Interfaces:**
- Consumes: `SignatureInputParser.GetComponents(string sigInput) : List<string>?`, `SignatureInputValidator.Validate(List<string> components, HttpRequestMessage request) : bool` (both in `Interledger.OpenPayments.HttpSignatureUtils`, both with parameterless ctors).
- Produces: nothing used by later tasks.

- [ ] **Step 1: Write the parser tests**

```csharp
using System.Collections.Generic;
using Xunit;

namespace Interledger.OpenPayments.HttpSignatureUtils.Tests;

public class SignatureInputParser_Tests
{
    private readonly SignatureInputParser _parser = new();

    [Fact]
    public void GetComponents_MinimalGetInput_ReturnsMethodAndTargetUri()
    {
        var sigInput =
            "sig1=(\"@method\" \"@target-uri\");created=1700000000;keyid=\"test-key\";alg=\"ed25519\"";

        var components = _parser.GetComponents(sigInput);

        Assert.NotNull(components);
        Assert.Equal(new List<string> { "@method", "@target-uri" }, components);
    }

    [Fact]
    public void GetComponents_FullPostInput_ReturnsAllComponentsInOrder()
    {
        var sigInput =
            "sig1=(\"@method\" \"@target-uri\" \"authorization\" \"content-digest\" \"content-length\" \"content-type\");created=1700000000;keyid=\"test-key\";alg=\"ed25519\"";

        var components = _parser.GetComponents(sigInput);

        Assert.Equal(
            new List<string>
            {
                "@method",
                "@target-uri",
                "authorization",
                "content-digest",
                "content-length",
                "content-type",
            },
            components
        );
    }
}
```

- [ ] **Step 2: Write the input-validator tests**

```csharp
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Xunit;

namespace Interledger.OpenPayments.HttpSignatureUtils.Tests;

public class SignatureInputValidator_Tests
{
    private readonly SignatureInputValidator _validator = new();

    private static HttpRequestMessage Get() =>
        new(HttpMethod.Get, "https://example.com/incoming-payments");

    [Fact]
    public void Validate_MethodAndTargetUri_NoAuthNoBody_ReturnsTrue()
    {
        Assert.True(_validator.Validate(["@method", "@target-uri"], Get()));
    }

    [Fact]
    public void Validate_MissingMethod_ReturnsFalse()
    {
        Assert.False(_validator.Validate(["@target-uri"], Get()));
    }

    [Fact]
    public void Validate_MissingTargetUri_ReturnsFalse()
    {
        Assert.False(_validator.Validate(["@method"], Get()));
    }

    [Fact]
    public void Validate_AuthorizationHeaderPresentButNotCovered_ReturnsFalse()
    {
        var request = Get();
        request.Headers.TryAddWithoutValidation("Authorization", "GNAP token-123");

        Assert.False(_validator.Validate(["@method", "@target-uri"], request));
    }

    [Fact]
    public void Validate_AuthorizationHeaderCovered_ReturnsTrue()
    {
        var request = Get();
        request.Headers.TryAddWithoutValidation("Authorization", "GNAP token-123");

        Assert.True(_validator.Validate(["@method", "@target-uri", "authorization"], request));
    }

    [Fact]
    public void Validate_ContentDigestCoveredButNoContent_ReturnsFalse()
    {
        Assert.False(_validator.Validate(["@method", "@target-uri", "content-digest"], Get()));
    }

    [Fact]
    public void Validate_ContentDigestCoveredWithAllContentHeaders_ReturnsTrue()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/x")
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        };
        request.Content.Headers.TryAddWithoutValidation("Content-Digest", "sha-512=:abc:");
        request.Content.Headers.ContentLength = 2;

        Assert.True(_validator.Validate(["@method", "@target-uri", "content-digest"], request));
    }
}
```

- [ ] **Step 3: Run the new tests**

Run: `dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests -c Release --filter "FullyQualifiedName~SignatureInput"`
Expected: 9 passed, 0 failed. If any fails, that is a genuine behavior discovery — stop and report it rather than bending the assertion (exception: do NOT "fix" `SignatureInputValidator`'s dead first check; these tests intentionally don't cover it).

- [ ] **Step 4: Full suite + commit**

Run: `dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests -c Release`
Expected: 44/44 passed.

```bash
git add OpenPayments.Sdk.HttpSignatureUtils.Tests/SignatureInputParser_Tests.cs OpenPayments.Sdk.HttpSignatureUtils.Tests/SignatureInputValidator_Tests.cs
git commit -m "test(signatures): cover SignatureInputParser and SignatureInputValidator"
```

---

### Task 2: Align SignatureInputBuilder with the signer (TDD — fixes a real bug)

`SignatureInputBuilder` (verification side) builds a different signature base than `HttpRequestSigner` (signing side): lowercase `@method` vs uppercase (RFC 9421 §2.2.1 mandates uppercase), `AppendLine` (`Environment.NewLine`, i.e. CRLF on Windows) vs `"\n"`, and a SHA-256 content-digest fallback vs the signer's SHA-512. Result: a request signed by this SDK's own signer fails validation. Fix the builder to match the signer — the signer is authoritative (it is what real Rafiki servers accept).

**Files:**
- Create: `OpenPayments.Sdk.HttpSignatureUtils.Tests/SignatureInputBuilder_Tests.cs`
- Modify: `OpenPayments.Sdk.HttpSignatureUtils/SignatureInputBuilder.cs` (whole file below)
- Modify: `CHANGELOG.md` (one `### Fixed` bullet)

**Interfaces:**
- Consumes: `SignatureInputBuilder.BuildBaseAsync(List<string> components, HttpRequestMessage request, string sigInput) : Task<string?>`.
- Produces: a builder whose output is byte-identical to `HttpRequestSigner`'s signature base — Task 3's round-trip tests depend on this.

- [ ] **Step 1: Write the failing tests**

```csharp
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Interledger.OpenPayments.HttpSignatureUtils.Tests;

public class SignatureInputBuilder_Tests
{
    private readonly SignatureInputBuilder _builder = new();

    [Fact]
    public async Task BuildBaseAsync_Get_UsesUppercaseMethodAndLineFeedSeparators()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");
        var sigInput =
            "sig1=(\"@method\" \"@target-uri\");created=1700000000;keyid=\"test-key\";alg=\"ed25519\"";

        var result = await _builder.BuildBaseAsync(["@method", "@target-uri"], request, sigInput);

        var expected =
            "\"@method\": GET\n"
            + "\"@target-uri\": https://example.com/resource\n"
            + "\"@signature-params\": (\"@method\" \"@target-uri\");created=1700000000;keyid=\"test-key\";alg=\"ed25519\"";
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task BuildBaseAsync_HeaderComponent_UsesHeaderValue()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");
        request.Headers.TryAddWithoutValidation("authorization", "GNAP token-123");
        var sigInput =
            "sig1=(\"@method\" \"@target-uri\" \"authorization\");created=1700000000;keyid=\"test-key\";alg=\"ed25519\"";

        var result = await _builder.BuildBaseAsync(
            ["@method", "@target-uri", "authorization"],
            request,
            sigInput
        );

        Assert.Contains("\"authorization\": GNAP token-123\n", result);
    }

    [Fact]
    public async Task BuildBaseAsync_ContentDigestFallback_ComputesSha512LikeTheSigner()
    {
        var body = "{\"access_token\":{}}";
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/resource")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        var sigInput =
            "sig1=(\"content-digest\");created=1700000000;keyid=\"test-key\";alg=\"ed25519\"";

        var result = await _builder.BuildBaseAsync(["content-digest"], request, sigInput);

        var expectedDigest = Convert.ToBase64String(SHA512.HashData(Encoding.UTF8.GetBytes(body)));
        Assert.Contains($"\"content-digest\": sha-512=:{expectedDigest}:\n", result);
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests -c Release --filter "FullyQualifiedName~SignatureInputBuilder"`
Expected: FAIL — test 1 gets `"@method": get` (lowercase), test 3 gets `sha-256=...`. Test 2 may already pass.

- [ ] **Step 3: Replace `SignatureInputBuilder.cs` with the aligned implementation**

```csharp
using System.Security.Cryptography;
using System.Text;

namespace Interledger.OpenPayments.HttpSignatureUtils;

/// <inheritdoc cref="ISignatureInputBuilder"/>
public class SignatureInputBuilder : ISignatureInputBuilder
{
    /// <inheritdoc cref="ISignatureInputBuilder"/>
    public async Task<string?> BuildBaseAsync(
        List<string> components,
        HttpRequestMessage request,
        string sigInput
    )
    {
        var sb = new StringBuilder();

        foreach (var component in components)
        {
            switch (component)
            {
                case "@method":
                    // RFC 9421 §2.2.1: uppercase, and HttpRequestSigner signs it uppercase —
                    // the base built here must be byte-identical to the one that was signed.
                    sb.Append($"\"@method\": {request.Method.Method.ToUpperInvariant()}\n");
                    break;
                case "@target-uri":
                    sb.Append($"\"@target-uri\": {request.RequestUri}\n");
                    break;
                default:
                    var value = await GetHeaderValueAsync(request, component);
                    sb.Append($"\"{component}\": {value}\n");
                    break;
            }
        }

        sb.Append($"\"@signature-params\": {sigInput.Replace("sig1=", "")}");
        return sb.ToString();
    }

    private static async Task<string> GetHeaderValueAsync(HttpRequestMessage request, string name)
    {
        if (request.Headers.TryGetValues(name, out var values))
            return string.Join(", ", values);
        if (request.Content?.Headers.TryGetValues(name, out var cvalues) == true)
            return string.Join(", ", cvalues);

        if (name == "content-digest" && request.Content != null)
        {
            // sha-512, matching HttpRequestSigner's ComputeContentDigest.
            var body = await request.Content.ReadAsStringAsync();
            var hash = SHA512.HashData(Encoding.UTF8.GetBytes(body));
            return $"sha-512=:{Convert.ToBase64String(hash)}:";
        }

        return "";
    }
}
```

- [ ] **Step 4: Run to verify green, then the whole suite**

Run: `dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests -c Release`
Expected: 47/47 passed (44 + 3 new).

- [ ] **Step 5: Add the CHANGELOG entry**

In `CHANGELOG.md` under `## [Unreleased]` → `### Fixed`, add this bullet:

```markdown
- `HttpSignatureValidator` now builds the same signature base as the signer (uppercase `@method`, LF separators, SHA-512 content-digest fallback), so requests signed by `HttpRequestSigner` validate successfully.
```

- [ ] **Step 6: Commit**

```bash
git add OpenPayments.Sdk.HttpSignatureUtils/SignatureInputBuilder.cs OpenPayments.Sdk.HttpSignatureUtils.Tests/SignatureInputBuilder_Tests.cs CHANGELOG.md
git commit -m "fix(signatures): build validation base identical to the signer's (RFC 9421 uppercase @method, LF, sha-512)"
```

---

### Task 3: Round-trip tests for HttpSignatureValidator

The last 0%-coverage class. Sign with the SDK's own `HttpRequestSigner`, then prove `HttpSignatureValidator` accepts the genuine request and rejects tampering / wrong keys. **Depends on Task 2** — round-trips fail before the builder fix.

**Files:**
- Create: `OpenPayments.Sdk.HttpSignatureUtils.Tests/HttpSignatureValidator_Tests.cs`

**Interfaces:**
- Consumes: `HttpSignatureValidator(ISignatureInputParser, ISignatureInputValidator, ISignatureInputBuilder)`, `.ValidateSignatureAsync(HttpRequestMessage, Jwk) : Task<bool>`, `.AreSignatureHeadersPresent(HttpRequestMessage) : bool`; `KeyUtils.GenerateKey()`, `KeyUtils.GenerateJwk(string keyId, Key? privateKey = null)`; `HttpRequestSigner.SignHttpRequestAsync(HttpRequestMessage, Key, string) : Task<SignatureHeaders>` (`.Signature`, `.SignatureInput`).
- Produces: nothing used by later tasks.

- [ ] **Step 1: Write the tests**

```csharp
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NSec.Cryptography;
using Xunit;

namespace Interledger.OpenPayments.HttpSignatureUtils.Tests;

public class HttpSignatureValidator_Tests
{
    private static HttpSignatureValidator CreateValidator() =>
        new(new SignatureInputParser(), new SignatureInputValidator(), new SignatureInputBuilder());

    private static async Task<HttpRequestMessage> SignAsync(
        HttpRequestMessage request,
        Key key,
        string keyId
    )
    {
        var headers = await HttpRequestSigner.SignHttpRequestAsync(request, key, keyId);
        request.Headers.TryAddWithoutValidation("Signature", headers.Signature);
        request.Headers.TryAddWithoutValidation("Signature-Input", headers.SignatureInput);
        return request;
    }

    [Fact]
    public void AreSignatureHeadersPresent_MissingHeaders_ReturnsFalse()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");
        Assert.False(CreateValidator().AreSignatureHeadersPresent(request));
    }

    [Fact]
    public async Task AreSignatureHeadersPresent_AfterSigning_ReturnsTrue()
    {
        var key = KeyUtils.GenerateKey();
        var request = await SignAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource"),
            key,
            "test-key"
        );

        Assert.True(CreateValidator().AreSignatureHeadersPresent(request));
    }

    [Fact]
    public async Task ValidateSignatureAsync_SignedGetRequest_ReturnsTrue()
    {
        var key = KeyUtils.GenerateKey();
        var jwk = KeyUtils.GenerateJwk("test-key", key);
        var request = await SignAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource"),
            key,
            "test-key"
        );

        Assert.True(await CreateValidator().ValidateSignatureAsync(request, jwk));
    }

    [Fact]
    public async Task ValidateSignatureAsync_SignedRequestWithAuthorizationAndBody_ReturnsTrue()
    {
        var key = KeyUtils.GenerateKey();
        var jwk = KeyUtils.GenerateJwk("test-key", key);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://example.com/grant")
        {
            Content = new StringContent(
                "{\"access_token\":{}}",
                Encoding.UTF8,
                "application/json"
            ),
        };
        request.Headers.TryAddWithoutValidation("Authorization", "GNAP token-123");
        await SignAsync(request, key, "test-key");

        Assert.True(await CreateValidator().ValidateSignatureAsync(request, jwk));
    }

    [Fact]
    public async Task ValidateSignatureAsync_HeaderTamperedAfterSigning_ReturnsFalse()
    {
        var key = KeyUtils.GenerateKey();
        var jwk = KeyUtils.GenerateJwk("test-key", key);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource");
        request.Headers.TryAddWithoutValidation("Authorization", "GNAP token-123");
        await SignAsync(request, key, "test-key");

        request.Headers.Remove("Authorization");
        request.Headers.TryAddWithoutValidation("Authorization", "GNAP tampered");

        Assert.False(await CreateValidator().ValidateSignatureAsync(request, jwk));
    }

    [Fact]
    public async Task ValidateSignatureAsync_WrongPublicKey_ReturnsFalse()
    {
        var signingKey = KeyUtils.GenerateKey();
        var otherJwk = KeyUtils.GenerateJwk("other-key");
        var request = await SignAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://example.com/resource"),
            signingKey,
            "test-key"
        );

        Assert.False(await CreateValidator().ValidateSignatureAsync(request, otherJwk));
    }
}
```

- [ ] **Step 2: Run the new tests**

Run: `dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests -c Release --filter "FullyQualifiedName~HttpSignatureValidator"`
Expected: 6 passed. A round-trip failure here means Task 2's fix is incomplete — debug there, do not weaken these assertions.

- [ ] **Step 3: Full suite + commit**

Run: `dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests -c Release`
Expected: 53/53 passed.

```bash
git add OpenPayments.Sdk.HttpSignatureUtils.Tests/HttpSignatureValidator_Tests.cs
git commit -m "test(signatures): round-trip HttpSignatureValidator against HttpRequestSigner"
```

---

### Task 4: Outgoing-paging test parity

Phase 4 Minor #3: outgoing paging lacks the caller-cursor and repeated-cursor-guard tests incoming has, and has no `AuthenticatedClient`-layer pass-through test. Also resolves the Phase 4 Task-3 nit that the outgoing `LastSet` test reuses the incoming-shaped mock.

**Files:**
- Modify: `OpenPayments.Sdk.Tests/Clients/ResourceClientBase_PagingTests.cs`
- Modify: `OpenPayments.Sdk.Tests/Clients/AuthenticatedClient_PagingTests.cs`

**Interfaces:**
- Consumes: `ResourceClientBase.ListOutgoingPaymentsAllAsync(AuthRequestArgs, ListOutgoingPaymentQuery, CancellationToken = default) : IAsyncEnumerable<OutgoingPayment>`; same signature on `AuthenticatedClient`; `ListOutgoingPaymentQuery { required string WalletAddress; string? Cursor; int? First; int? Last }`; the file's existing helpers `CreateClient`, `GetQueryValue`, `Args`, `MakeOutgoingPayment`.
- Produces: `MakeOutgoingPayment` becomes `internal static` (was `private static`) so `AuthenticatedClient_PagingTests` can reuse it.

- [ ] **Step 1: In `ResourceClientBase_PagingTests.cs`, make `MakeOutgoingPayment` internal and add an outgoing two-page helper**

Change `private static OutgoingPayment MakeOutgoingPayment(int i)` to `internal static OutgoingPayment MakeOutgoingPayment(int i)`, then add next to `CreateTwoPageClient()`:

```csharp
    private static (HttpClient Client, List<Uri> Requests) CreateTwoPageOutgoingClient() =>
        CreateClient(cursor =>
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
                "cursor-1" => new ListOutgoingPaymentsResponse
                {
                    Result = [MakeOutgoingPayment(3)],
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
```

- [ ] **Step 2: Add the two missing outgoing tests and repoint the LastSet test**

Add after `ListOutgoingPaymentsAllAsync_FollowsCursorsAcrossAllPages`:

```csharp
    [Fact]
    public async Task ListOutgoingPaymentsAllAsync_StartsFromCallerCursorAndKeepsFirst()
    {
        var (httpClient, requests) = CreateTwoPageOutgoingClient();
        var client = new ResourceClientBase(httpClient, new Uri("https://client.example"));

        var payments = new List<OutgoingPayment>();
        await foreach (
            var payment in client.ListOutgoingPaymentsAllAsync(
                Args(),
                new ListOutgoingPaymentQuery
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
    public async Task ListOutgoingPaymentsAllAsync_ServerRepeatsCursor_Throws()
    {
        var (httpClient, _) = CreateClient(_ => new ListOutgoingPaymentsResponse
        {
            Result = [MakeOutgoingPayment(1)],
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
                var _ in client.ListOutgoingPaymentsAllAsync(
                    Args(),
                    new ListOutgoingPaymentQuery { WalletAddress = "https://host-a.example/alice" }
                )
            ) { }
        });
    }
```

In the existing `ListOutgoingPaymentsAllAsync_LastSet_ThrowsArgumentException`, change `CreateTwoPageClient()` to `CreateTwoPageOutgoingClient()` (the request never fires, but the mock should be outgoing-shaped).

- [ ] **Step 3: Add the AuthenticatedClient-layer outgoing pass-through test**

Append to `AuthenticatedClient_PagingTests`:

```csharp
    [Fact]
    public async Task ListOutgoingPaymentsAllAsync_EnumeratesAcrossPages()
    {
        var (httpClient, requests) = ResourceClientBase_PagingTests.CreateClient(cursor =>
            cursor switch
            {
                null => new ListOutgoingPaymentsResponse
                {
                    Result =
                    [
                        ResourceClientBase_PagingTests.MakeOutgoingPayment(1),
                        ResourceClientBase_PagingTests.MakeOutgoingPayment(2),
                    ],
                    Pagination = new PageInfo
                    {
                        EndCursor = "cursor-1",
                        HasNextPage = true,
                        HasPreviousPage = false,
                    },
                },
                _ => new ListOutgoingPaymentsResponse
                {
                    Result = [ResourceClientBase_PagingTests.MakeOutgoingPayment(3)],
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
            var payment in client.ListOutgoingPaymentsAllAsync(
                new AuthRequestArgs
                {
                    Url = new Uri("https://host-a.example/"),
                    AccessToken = "token",
                },
                new ListOutgoingPaymentQuery { WalletAddress = "https://host-a.example/alice" }
            )
        )
        {
            ids.Add(payment.Id.ToString());
        }

        ids.Should().HaveCount(3);
        requests.Should().HaveCount(2);
    }
```

- [ ] **Step 4: Run the paging tests**

Run: `dotnet test OpenPayments.Sdk.Tests -c Release --filter "FullyQualifiedName~PagingTests"`
Expected: 11 passed (8 existing + 3 new), 0 failed.

- [ ] **Step 5: Commit**

```bash
git add OpenPayments.Sdk.Tests/Clients/ResourceClientBase_PagingTests.cs OpenPayments.Sdk.Tests/Clients/AuthenticatedClient_PagingTests.cs
git commit -m "test(paging): outgoing parity — caller cursor, repeated-cursor guard, authenticated pass-through"
```

---

### Task 5: Slow test category for the concurrency suites

Phase 1 Minor: the two concurrency tests take ~4.5 min each (~9 min combined). Tag them so day-to-day runs can skip them; CI keeps running everything.

**Files:**
- Modify: `OpenPayments.Sdk.Tests/Clients/ResourceClientBase_ConcurrencyTests.cs:17`
- Modify: `OpenPayments.Sdk.Tests/Clients/AuthClientBase_ConcurrencyTests.cs:17`
- Modify: `README.md` (the `## 🔧 Running the tests` section)

**Interfaces:**
- Consumes: nothing from other tasks.
- Produces: the trait name `Category=Slow` — Task 6's README note and any future CI filter must use exactly this spelling.

- [ ] **Step 1: Tag both tests**

In each file, directly under the existing `[Fact]` attribute (line 17 in both), add:

```csharp
    [Trait("Category", "Slow")]
```

- [ ] **Step 2: Document the filter in README**

Replace the `## 🔧 Running the tests` section body:

````markdown
## 🔧 Running the tests

```bash
dotnet test
```

The two thread-safety concurrency suites take several minutes; skip them during day-to-day
development with:

```bash
dotnet test --filter "Category!=Slow"
```
````

- [ ] **Step 3: Verify the filter actually excludes them**

Run: `dotnet test OpenPayments.Sdk.Tests -c Release --filter "Category!=Slow"`
Expected: 59 passed (61 after Task 4, minus the 2 Slow-tagged tests). Completes in well under a minute — if it takes ~10 min the trait didn't apply.

- [ ] **Step 4: Commit**

```bash
git add OpenPayments.Sdk.Tests/Clients/ResourceClientBase_ConcurrencyTests.cs OpenPayments.Sdk.Tests/Clients/AuthClientBase_ConcurrencyTests.cs README.md
git commit -m "test: tag concurrency suites as Category=Slow and document the local filter"
```

---

### Task 6: Run tests on net8.0 in CI (runtime coverage for the lowest TFM)

Phase 3 Minor: `net8.0` is compile-checked but never runtime-tested. Multi-target both test projects and give CI the .NET 8 runtime.

**Files:**
- Modify: `OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj:4`
- Modify: `OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj:3`
- Modify: `.github/workflows/build.yaml` (Setup .NET SDK step)
- Modify: `.github/workflows/release.yaml` (Setup .NET SDK step — it also runs `dotnet test`)
- Modify: `README.md` (Running the tests section — one added note)

**Interfaces:**
- Consumes: nothing from other tasks.
- Produces: test projects with `<TargetFrameworks>net8.0;net9.0</TargetFrameworks>` — every later local `dotnet test` on this net9-only machine needs `-f net9.0`.

- [ ] **Step 1: Multi-target both test csprojs**

In both files replace:

```xml
    <TargetFramework>net9.0</TargetFramework>
```

with:

```xml
    <TargetFrameworks>net8.0;net9.0</TargetFrameworks>
```

- [ ] **Step 2: Add the .NET 8 runtime to both workflows**

In `.github/workflows/build.yaml` AND `.github/workflows/release.yaml`, the `Setup .NET SDK` step becomes:

```yaml
      - name: Setup .NET SDK
        uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json
          dotnet-version: 8.0.x
```

(`setup-dotnet` installs the union: 8.0.x for the test runtime plus the SDK pinned by `global.json`. The build itself still uses the `global.json` SDK.)

- [ ] **Step 3: Note the local single-framework run in README**

Append to the `## 🔧 Running the tests` section (after the Slow-filter block from Task 5):

````markdown
Test projects target both `net8.0` and `net9.0`. If you only have one runtime installed
locally, pick it explicitly, e.g.:

```bash
dotnet test -f net9.0
```
````

- [ ] **Step 4: Verify build (both TFMs compile) and net9 tests**

Run: `dotnet build -c Release`
Expected: 0 warnings, 0 errors — both test projects build `net8.0` and `net9.0` (targeting packs restore via NuGet; the 8.0 *runtime* is only needed to execute).

Run: `dotnet test -c Release -f net9.0 --filter "Category!=Slow"`
Expected: all pass. (net8.0 execution is validated by CI; this machine has no 8.0 runtime — record that in the task report, do not claim net8 was run locally.)

- [ ] **Step 5: Commit**

```bash
git add OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj .github/workflows/build.yaml .github/workflows/release.yaml README.md
git commit -m "ci: run test suites on net8.0 and net9.0 runtimes"
```

---

### Task 7: Per-assembly coverage floors with hard parse failure

Phase 3 Important #1 (second half) + Minor #4: replace the aggregate 60% floor with per-assembly floors so `Interledger.OpenPayments.HttpSignatureUtils` can't hide behind SDK coverage, and fail loudly when the summary can't be parsed instead of reporting "below floor".

**Files:**
- Modify: `.github/workflows/build.yaml` (the `Enforce coverage threshold` step)

**Interfaces:**
- Consumes: ReportGenerator `TextSummary` format — assembly rows are flush-left two-field lines, e.g. `Interledger.OpenPayments.HttpSignatureUtils    60.2%` (class rows are indented; verified against real output on 2026-07-24).
- Produces: nothing used by later tasks.

- [ ] **Step 1: Replace the threshold step**

Delete the existing `Enforce coverage threshold` step and put in its place:

```yaml
      - name: Enforce per-assembly coverage floors
        run: |
          check_assembly() {
            NAME="$1"; FLOOR="$2"
            COV=$(awk -v name="$NAME" '$1 == name && NF == 2 { sub(/%$/, "", $2); print $2 }' coverage-report/Summary.txt)
            if [ -z "$COV" ]; then
              echo "::error::Could not parse line coverage for ${NAME} from coverage-report/Summary.txt"
              exit 1
            fi
            echo "${NAME} line coverage: ${COV}%"
            if ! awk -v c="$COV" -v f="$FLOOR" 'BEGIN { exit !(c + 0 >= f + 0) }'; then
              echo "::error::${NAME} line coverage ${COV}% is below the ${FLOOR}% floor"
              exit 1
            fi
          }
          check_assembly "Interledger.OpenPayments" 60
          check_assembly "Interledger.OpenPayments.HttpSignatureUtils" 60
```

(Floors stay at 60 — ratcheting them up is a deliberate later decision, not part of this fix. With Tasks 1–3 landed, HttpSignatureUtils will clear 60 comfortably on its own.)

- [ ] **Step 2: Verify locally against a real Summary.txt**

```bash
dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests -c Release -f net9.0 --collect:"XPlat Code Coverage"
~/.dotnet/tools/reportgenerator -reports:"OpenPayments.Sdk.HttpSignatureUtils.Tests/TestResults/*/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:"TextSummary" -classfilters:"-Interledger.OpenPayments.Generated.*"
```

Then paste the `check_assembly` function into the shell and run:
- `check_assembly "Interledger.OpenPayments.HttpSignatureUtils" 60` → Expected: prints coverage (should now be well above 60 after Tasks 1–3), exits 0.
- `check_assembly "Interledger.OpenPayments.HttpSignatureUtils" 99` → Expected: `below the 99% floor` error, exit 1.
- `check_assembly "No.Such.Assembly" 60` → Expected: `Could not parse` error, exit 1.

Clean up: `rm -rf coverage-report` (do not commit it).

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/build.yaml
git commit -m "ci: per-assembly coverage floors; fail hard on unparseable summary"
```

---

### Task 8: PublicAPI stance for generated types — ADR, `make api`, hardened codegen-check

Phase 3 Important #2 (needs deciding **before the first `v*` tag**) plus the Phase 2 Minor (hand-edits to committed `.g.cs` escape the drift check). Decision being implemented: **keep tracking the generated DTO surface** (it *is* the API consumers compile against), and make the regen-then-refresh workflow mechanical and CI-enforced instead of exempting `Generated.*`.

**Files:**
- Create: `docs/adr/0002-public-api-tracking-of-generated-types.md`
- Modify: `Makefile` (new `api` target + `.PHONY`)
- Modify: `.github/workflows/codegen-check.yaml`
- Modify: `.github/contributing.md` (new section at end of file)

**Interfaces:**
- Consumes: the per-project refresh command discovered in Phase 3 Task 6: `dotnet format analyzers <csproj> --diagnostics RS0016 RS0017 --severity warn --include-generated` (per-project and `--include-generated` are both mandatory — the whole-solution form silently skips `.g.cs` symbols).
- Produces: `make api` — referenced by codegen-check.yaml and contributing.md.

- [ ] **Step 1: Add the Makefile target**

Change the `.PHONY` line to end with ` models api` and append at the bottom:

```make
api:
	dotnet format analyzers OpenPayments.Sdk/OpenPayments.Sdk.csproj --diagnostics RS0016 RS0017 --severity warn --include-generated
	dotnet format analyzers OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj --diagnostics RS0016 RS0017 --severity warn --include-generated
```

- [ ] **Step 2: Verify `make api` is a no-op on a clean tree**

Run: `make api` then `git status --porcelain`
Expected: empty output (takes a few minutes; it builds both projects). If it normalizes the committed `PublicAPI.*.txt` files (reorders/removes lines), inspect the diff — if it's pure normalization, include those file changes in this task's commit and say so in the report; if it adds/removes actual symbols, stop and investigate.

- [ ] **Step 3: Harden codegen-check.yaml**

Three edits:

1. Extend the `pull_request.paths` list with the generated output and API baselines so hand-edits trigger the check:

```yaml
      - 'OpenPayments.Sdk/Generated/**'
      - 'OpenPayments.Sdk/PublicAPI.Shipped.txt'
      - 'OpenPayments.Sdk/PublicAPI.Unshipped.txt'
```

2. After the `Regenerate models` step, add:

```yaml
      - name: Refresh public API baselines
        run: make api
```

3. Replace the final step with:

```yaml
      - name: Fail if committed generated code or API baselines drifted
        run: git diff --exit-code -- 'OpenPayments.Sdk/Generated/**/*.g.cs' 'OpenPayments.Sdk/PublicAPI.Shipped.txt' 'OpenPayments.Sdk/PublicAPI.Unshipped.txt'
```

(A hand-edit to a committed `.g.cs` now triggers the workflow, `make models` regenerates over it, and the diff fails the build. Same for stale/hand-edited API baselines.)

- [ ] **Step 4: Write the ADR**

Create `docs/adr/0002-public-api-tracking-of-generated-types.md`:

```markdown
# ADR 0002: Keep NSwag-generated types in the tracked public API surface

- Status: Accepted
- Date: 2026-07-24
- Context: Phase 3 final review, Important finding #2

## Context

`Microsoft.CodeAnalysis.PublicApiAnalyzers` tracks the SDK's public surface in
`PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`. ~790 of the ~830 tracked lines in
`OpenPayments.Sdk/PublicAPI.Unshipped.txt` are NSwag-generated DTOs
(`Interledger.OpenPayments.Generated.*`). Every model regeneration therefore requires a
baseline refresh, and once a `v*` tag ships, the surface freezes into `Shipped.txt`. The
review demanded a deliberate decision before that first tag: exempt `Generated.*` from
tracking, or own the coupling.

## Decision

Keep the generated types tracked, and make the refresh mechanical:

- `make api` refreshes both projects' baselines. It must run per-project with
  `--include-generated` (`dotnet format analyzers <csproj> --diagnostics RS0016 RS0017
  --severity warn --include-generated`); the whole-solution form silently skips `.g.cs`
  symbols.
- `codegen-check.yaml` runs `make models` + `make api` on any PR touching the spec
  submodule, Makefile, tool pin, generated output, or the baselines themselves, and fails
  on drift. Regeneration and baseline refresh land in the same commit or CI rejects it.

## Rationale

The generated DTOs are the API consumers compile against — a spec bump that renames a DTO
property is exactly as breaking as renaming a hand-written method, and the analyzer diff is
where that breakage becomes visible in review. Exempting `Generated.*` would hide the
majority of real surface changes to save one scripted step that CI now enforces anyway.

## Consequences

- Spec updates produce a reviewable `PublicAPI.*.txt` diff summarizing the surface change.
- After a release tags the surface into `Shipped.txt`, regen-driven removals surface as
  RS0017 diffs against shipped API — i.e., flagged as the breaking changes they are.
- Contributors must run `make models && make api` together (documented in
  `.github/contributing.md`); forgetting is a CI failure, not silent drift.
```

- [ ] **Step 5: Document the workflow in contributing.md**

Append to `.github/contributing.md`:

````markdown
## Regenerating models and public API baselines

`OpenPayments.Sdk/Generated/**/*.g.cs` is committed, and the public API surface is tracked by
PublicApiAnalyzers (`PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`). If you regenerate the
models — after updating the `open-payments-specifications` submodule, the `Makefile` NSwag
flags, or the pinned NSwag version — refresh the baselines in the same PR:

```bash
make models   # regenerate *.g.cs from the OpenAPI specs
make api      # sync PublicAPI.*.txt with the regenerated surface
```

CI (`codegen-check.yaml`) reruns both on any PR touching codegen inputs, generated output, or
the baselines, and fails if the committed files drift. See
`docs/adr/0002-public-api-tracking-of-generated-types.md` for why generated types stay tracked.
````

- [ ] **Step 6: Verify and commit**

Run: `dotnet build -c Release`
Expected: 0 warnings / 0 errors (nothing code-affecting changed).

```bash
git add Makefile .github/workflows/codegen-check.yaml docs/adr/0002-public-api-tracking-of-generated-types.md .github/contributing.md
git commit -m "build(api): make api target, ADR 0002, codegen-check enforces baselines and .g.cs hand-edit drift"
```

---

### Task 9: Scope the RS0026 suppression to the files that need it

Phase 3 Minor: `dotnet_diagnostic.RS0026.severity = none` currently applies to every `.cs` file. The overload sets that trigger it live in exactly three files.

**Files:**
- Modify: `.editorconfig` (RS0026 block at the end of the file)

**Interfaces:**
- Consumes: nothing from other tasks.
- Produces: nothing used by later tasks.

- [ ] **Step 1: Move the suppression into a scoped section**

Replace this block at the end of `.editorconfig`:

```ini
# RS0026 (PublicApiAnalyzers): "do not add multiple overloads with optional parameters" fires on
# CreateQuoteAsync/CreateOutgoingPaymentAsync, whose overloads intentionally differ by required
# body-parameter type (e.g. QuoteBody vs QuoteBodyWithDebitAmount) and share a trailing optional
# CancellationToken; callers cannot hit real overload-resolution ambiguity here.
dotnet_diagnostic.RS0026.severity = none
```

with (note: this moves the setting OUT of the `[*.cs]` section into its own section at the very end of the file):

```ini
# RS0026 (PublicApiAnalyzers): "do not add multiple overloads with optional parameters" fires on
# CreateQuoteAsync/CreateOutgoingPaymentAsync, whose overloads intentionally differ by required
# body-parameter type (e.g. QuoteBody vs QuoteBodyWithDebitAmount) and share a trailing optional
# CancellationToken; callers cannot hit real overload-resolution ambiguity here. Scoped to the
# three files declaring those overload sets so a new ambiguous overload elsewhere still fails.
[OpenPayments.Sdk/Clients/{IAuthenticatedClient,AuthenticatedClient,ResourceClientBase}.cs]
dotnet_diagnostic.RS0026.severity = none
```

- [ ] **Step 2: Verify the scoped suppression still covers the firing sites**

Run: `dotnet build -c Release`
Expected: 0 warnings / 0 errors.

- [ ] **Step 3: Verify the suppression is still load-bearing (negative check)**

Temporarily comment out the new section's `dotnet_diagnostic.RS0026.severity = none` line, run `dotnet build OpenPayments.Sdk -c Release`.
Expected: build FAILS with RS0026 on the CreateQuoteAsync/CreateOutgoingPaymentAsync overloads. Restore the line, rebuild clean. (If it does NOT fail, the suppression is dead — remove it entirely instead and say so in the report.)

- [ ] **Step 4: Commit**

```bash
git add .editorconfig
git commit -m "chore(analyzers): scope RS0026 suppression to the three files with the flagged overload sets"
```

---

### Task 10: CHANGELOG completeness and README auto-pager showcase

Phase 3 Minor #1 (missing HttpSignatureUtils metadata line) and Phase 4 Minor #6 (README doesn't showcase the auto-pager). Also adds the missing `### Added` entry for Phase 4's auto-paging, which the changelog never recorded.

**Files:**
- Modify: `CHANGELOG.md` (`[Unreleased]` section)
- Modify: `README.md` (`## 🎈 Usage` section)

**Interfaces:**
- Consumes: `IAuthenticatedClient.ListIncomingPaymentsAllAsync(AuthRequestArgs, ListIncomingPaymentQuery, CancellationToken = default) : IAsyncEnumerable<IncomingPayment>`; `AuthRequestArgs { Url, AccessToken }` (namespace `Interledger.OpenPayments.Clients`); `ListIncomingPaymentQuery { required string WalletAddress }` (namespace `Interledger.OpenPayments.Generated.Resource`).
- Produces: nothing used by later tasks.

- [ ] **Step 1: CHANGELOG — add the Added section and the metadata line**

Directly under `## [Unreleased]` (before `### Changed`) insert:

```markdown
### Added
- `IAsyncEnumerable` auto-paging: `ListIncomingPaymentsAllAsync` / `ListOutgoingPaymentsAllAsync` on both the authenticated client and the resource client follow `pageInfo` cursors automatically (rejecting backward-paging queries and repeated server cursors).
```

And append to the existing `### Changed` list:

```markdown
- `Interledger.OpenPayments.HttpSignatureUtils` graduated from placeholder package metadata to full NuGet metadata (description, license, README, repository URL, icon, authors).
```

- [ ] **Step 2: README — showcase the auto-pager**

In the Usage snippet's `using` block, add:

```csharp
using Interledger.OpenPayments.Generated.Resource;
```

Then between the client-initialization code block and the "Please visit [OpenPayments Docs]…" line, insert:

````markdown
List endpoints also come as auto-paging streams — `ListIncomingPaymentsAllAsync` /
`ListOutgoingPaymentsAllAsync` follow the server's pagination cursors for you:

```csharp
await foreach (
    var payment in client.ListIncomingPaymentsAllAsync(
        new AuthRequestArgs
        {
            Url = new Uri(RESOURCE_SERVER_URL),
            AccessToken = INCOMING_PAYMENT_ACCESS_TOKEN,
        },
        new ListIncomingPaymentQuery { WalletAddress = CLIENT_WALLET_ADDRESS }
    )
)
{
    Console.WriteLine(payment.Id);
}
```
````

- [ ] **Step 3: Verify docs-only diff and commit**

Run: `git diff --stat` — Expected: only `CHANGELOG.md` and `README.md`.

```bash
git add CHANGELOG.md README.md
git commit -m "docs: changelog Added/metadata entries; README auto-pager example"
```

---

## Final Verification (after all 10 tasks)

- [ ] `dotnet build -c Release` → 0 warnings / 0 errors.
- [ ] `dotnet test -c Release -f net9.0` (full, including Slow) → all pass: `OpenPayments.Sdk.Tests` = 61 (58 + 3 from Task 4), `OpenPayments.Sdk.HttpSignatureUtils.Tests` = 53 (35 + 18 from Tasks 1–3).
- [ ] Full coverage spot-check: run the Task 7 Step 2 commands but with `-reports:"**/coverage.cobertura.xml"` after a full `dotnet test -f net9.0 --collect:"XPlat Code Coverage"`; both `check_assembly` calls pass. Remove `coverage-report/` and `TestResults/` artifacts afterward.
- [ ] `make api && git status --porcelain` → empty.
- [ ] `git log --oneline 0889f62..HEAD` → 10 commits matching the task list.
- [ ] Update `.superpowers/sdd/progress.md` with a new "Flagged-items burndown" ledger section listing task → commit mappings.
- [ ] Do NOT push anywhere — pushing to `cozmin/overhaul` happens only via superpowers:finishing-a-development-branch with explicit user confirmation.

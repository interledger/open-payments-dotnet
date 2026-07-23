# Phase 2 — Namespace Alignment & Types-Only Code Generation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Execute the pre-adoption breaking-change window from `IMPROVEMENTS.md`: rename every namespace from `OpenPayments.Sdk.*` to `Interledger.OpenPayments.*` so namespaces match the NuGet package identity (#5), and switch NSwag to generating DTOs only — deleting thousands of lines of dead generated client code and moving the small shared HTTP plumbing into one hand-owned base class (#4), with a single decided stance on committed generated code (committed + CI drift check, no regeneration during CI builds).

**Architecture:**
- #5 is a mechanical, atomic rename: one scripted pass over all `.cs` files + the Makefile `/namespace:` flags + the README usage snippet, plus `AssemblyName`/`InternalsVisibleTo` updates so `[InternalsVisibleTo]` still names real assemblies. A second task gives the currently *global-namespace* `OpenPayments.Sdk.HttpSignatureUtils` types a proper `Interledger.OpenPayments.HttpSignatureUtils` namespace.
- #4 keeps the hand-written `*.Methods.*.cs` partials byte-for-byte untouched. The trick: those partials only depend on a small infrastructure surface (`_httpClient`, `JsonSerializerSettings`, `ReadObjectResponseAsync`, `ReadAsStringAsync`, `ConvertToString`, `NormalizeBaseUrl`, `ObjectResponseResult<T>`, `Helpers.ExtractHeaders`, and no-op `PrepareRequest`/`ProcessResponse` partial hooks). That surface moves into a new hand-owned `GeneratedClientBase` plus one tiny `*.Core.cs` partial per client (ctor + resolver + partial-method declarations). NSwag then regenerates the `.g.cs` files with `/GenerateClientClasses:false /GenerateExceptionClasses:false`, producing DTOs/enums only. **This was verified empirically against NSwag.ConsoleCore 14.7.1 and the current spec submodule**: types-only output still contains every inline operation type the hand-written code subclasses (`Body`, `Body2`, `Body3`, `Response`, `Response2`, `Response3`, `PageInfo`, `ErrorResponse`, `Error`, …), and `/GenerateExceptionClasses:false` removes `ApiException` entirely. Deserialization failures now raise `OpenPaymentsApiException` (finishing improvement #3's unification), and the hard-coded test-wallet `BaseUrl` constructors disappear with the generated client code.

**Tech Stack:** .NET 9 (`net9.0`), NSwag.ConsoleCore 14.7.1 (pinned via `.config/dotnet-tools.json`), swagger-cli (npx), Newtonsoft.Json 13.0.3, xUnit + FluentAssertions + Moq.

## Global Constraints

- **Prerequisite:** the Phase 1 plan (`2026-07-23-phase1-correctness-architecture.md`) is fully executed. All code snippets below show the *post-Phase-1* shape of files (e.g. `ResourceServerClient` methods take a `Uri baseUri` first parameter, `OpenPaymentsApiException` exists, `.Auth.cs` partials no longer sign requests).
- Directory names, `.csproj` file names, the `.sln`, and NuGet `PackageId`s (`Interledger.OpenPayments`, `Interledger.OpenPayments.HttpSignatureUtils`) do **not** change — only namespaces, assembly names of test projects, and generated code.
- Never hand-edit `.g.cs` files **except** in Task 1's scripted rename, where the namespace inside `.g.cs` is changed by the same one-shot script that changes the Makefile `/namespace:` flag in the same commit (they must stay in lockstep; `make models` in Task 3 regenerates them anyway).
- After every task: `dotnet build --configuration Release` succeeds and both test suites pass (`dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj` and `dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj`).
- Codegen toolchain for Task 3: Node (for `npx swagger-cli`) and the pinned `dotnet nswag` tool. The spec submodule `open-payments-specifications/` must be initialized (`git submodule update --init`).
- Public API renames in this phase are intentional breaking changes; the package is pre-1.0 in practice (per `IMPROVEMENTS.md` #5).

---

### Task 1: Rename `OpenPayments.Sdk.*` → `Interledger.OpenPayments.*` everywhere

**Files:**
- Modify: every tracked `*.cs` file (scripted; includes `.g.cs`, tests, snippets)
- Modify: `Makefile` (three `/namespace:` flags)
- Modify: `README.md` (usage-snippet `using` lines)
- Modify: `.github/workflows/build.yaml`, `.github/workflows/release.yaml` (coverage `classfilters` only)
- Modify: `OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj`, `OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj` (AssemblyName), `OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj` (InternalsVisibleTo)

**Interfaces:**
- Consumes: nothing new.
- Produces: all namespaces become `Interledger.OpenPayments`, `Interledger.OpenPayments.Clients`, `Interledger.OpenPayments.Configuration`, `Interledger.OpenPayments.Extensions`, `Interledger.OpenPayments.Generated{,.Auth,.Resource,.Wallet}`, `Interledger.OpenPayments.HttpSignatureUtils` (for the files that already had a namespace), `Interledger.OpenPayments.Tests.*`, `Interledger.OpenPayments.HttpSignatureUtils.Tests`. Test assemblies are renamed to match (`Interledger.OpenPayments.Tests`, `Interledger.OpenPayments.HttpSignatureUtils.Tests`) so `InternalsVisibleTo` keeps working. Later tasks and phases use these names exclusively.

There is no failing-test step for a rename; the "test" is: the old prefix is gone, the build compiles, and every existing test still passes.

- [ ] **Step 1: Run the scripted rename over all C# sources, the Makefile, and the README**

From the repo root:

```bash
perl -pi -e 's/OpenPayments\.Sdk/Interledger.OpenPayments/g' $(git ls-files '*.cs') Makefile README.md
```

This rewrites, in one consistent pass: all `namespace` declarations (including inside `.g.cs`), all `using` directives, the `[assembly: InternalsVisibleTo("OpenPayments.Sdk.Tests")]` attribute in `OpenPayments.Sdk/Clients/UnauthenticatedClient.cs` (becomes `"Interledger.OpenPayments.Tests"` — Step 3 makes the test assembly actually have that name), the three `/namespace:OpenPayments.Sdk.Generated.*` flags in the Makefile, and the README usage snippet's `using OpenPayments.Sdk.Clients;` / `using OpenPayments.Sdk.Extensions;` / `using OpenPayments.Sdk.HttpSignatureUtils;` lines. It does **not** touch directory names, csproj paths, or `OpenPayments.Snippets.*` namespaces (different string).

- [ ] **Step 2: Fix the coverage class filter in both workflows**

In `.github/workflows/build.yaml` and `.github/workflows/release.yaml`, replace (one occurrence each):

```yaml
          reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html -classfilters:"-OpenPayments.Sdk.Generated.*"
```
with:
```yaml
          reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html -classfilters:"-Interledger.OpenPayments.Generated.*"
```

(Do not touch the workflow lines that reference csproj *paths* like `OpenPayments.Sdk/OpenPayments.Sdk.csproj` — file paths are unchanged.)

- [ ] **Step 3: Align test assembly names and the InternalsVisibleTo declaration**

Modify `OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj` — replace:
```xml
    <RootNamespace>Interledger.OpenPayments.Tests</RootNamespace>
    <PackageId>OpenPayments.Sdk.Tests</PackageId>
```
with:
```xml
    <RootNamespace>Interledger.OpenPayments.Tests</RootNamespace>
    <AssemblyName>Interledger.OpenPayments.Tests</AssemblyName>
```

Modify `OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj` — replace:
```xml
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
```
with:
```xml
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <RootNamespace>Interledger.OpenPayments.HttpSignatureUtils.Tests</RootNamespace>
    <AssemblyName>Interledger.OpenPayments.HttpSignatureUtils.Tests</AssemblyName>
  </PropertyGroup>
```

Modify `OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj` — replace the broken file-path form:
```xml
    <InternalsVisibleTo Include="../OpenPayments.Sdk.HttpSignatureUtils.Tests"/>
```
with the assembly name that Step 3 just gave the test project:
```xml
    <InternalsVisibleTo Include="Interledger.OpenPayments.HttpSignatureUtils.Tests"/>
```

- [ ] **Step 4: Verify the old prefix is gone**

Run: `grep -rn "OpenPayments\.Sdk" --include='*.cs' . ; grep -n "OpenPayments.Sdk" Makefile README.md`
Expected: **no output** from either command (exit code 1).

Run: `grep -rn "OpenPayments.Sdk.Generated" .github/workflows/`
Expected: no output.

- [ ] **Step 5: Build and run all tests**

Run: `dotnet build --configuration Release`
Expected: succeeds. If any `CS0246` appears it means a `using` was missed by the script — fix the reported file by hand with the same substitution.

Run: `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj`
Run: `dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj`
Expected: all tests PASS (the `InternalsVisibleTo` pairs in both directions still line up because assembly names moved together with the attribute strings).

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor!: rename namespaces OpenPayments.Sdk.* to Interledger.OpenPayments.*"
```

---

### Task 2: Give the global-namespace HttpSignatureUtils types a real namespace

**Files:**
- Modify: `OpenPayments.Sdk.HttpSignatureUtils/HttpRequestSigner.cs` (contains `SignatureHeaders` + `HttpRequestSigner`)
- Modify: `OpenPayments.Sdk.HttpSignatureUtils/HttpSignatureValidator.cs`
- Modify: `OpenPayments.Sdk.HttpSignatureUtils/SignatureInputBuilder.cs`
- Modify: `OpenPayments.Sdk.HttpSignatureUtils/SignatureInputParser.cs`
- Modify: `OpenPayments.Sdk.HttpSignatureUtils/SignatureInputValidator.cs`
- Modify: `OpenPayments.Sdk.HttpSignatureUtils/SigningHttpMessageHandler.cs` (created in Phase 1)
- Modify: `OpenPayments.Sdk.HttpSignatureUtils/Extensions/HttpClientSignatureExtensions.cs`
- Modify: `OpenPayments.Sdk.HttpSignatureUtils/Extensions/KeyExtensions.cs`
- Modify: `OpenPayments.Sdk.HttpSignatureUtils/Interfaces/IHttpSignatureValidator.cs`
- Modify: `OpenPayments.Sdk.HttpSignatureUtils/Interfaces/ISignatureInputBuilder.cs`
- Modify: `OpenPayments.Sdk.HttpSignatureUtils/Interfaces/ISignatureInputParser.cs`
- Modify: `OpenPayments.Sdk.HttpSignatureUtils/Interfaces/ISignatureInputValidator.cs`
- Modify: `OpenPayments.Sdk/Extensions/ServiceCollectionExtensions.cs` (add one `using`)

**Interfaces:**
- Consumes: the renamed namespaces from Task 1.
- Produces: every public type in the HttpSignatureUtils project now lives in `Interledger.OpenPayments.HttpSignatureUtils` (`HttpRequestSigner`, `SignatureHeaders`, `SigningHttpMessageHandler`, `HttpSignatureValidator`, `SignatureInputBuilder`, `SignatureInputParser`, `SignatureInputValidator`, the four `ISignatureInput*`/`IHttpSignatureValidator` interfaces, and internal `KeyExtensions`) — joining `KeyUtils`, `GenerateKeyArgs`, and `Jwk` which already declare that namespace. Phase 3 and 4 code `using Interledger.OpenPayments.HttpSignatureUtils;` relies on this.

- [ ] **Step 1: Add the namespace declaration to each of the 12 files**

In each file listed above **under `OpenPayments.Sdk.HttpSignatureUtils/`**, insert a file-scoped namespace declaration immediately after the last `using` directive (or as the first line when the file has no usings):

```csharp
namespace Interledger.OpenPayments.HttpSignatureUtils;
```

Example — `OpenPayments.Sdk.HttpSignatureUtils/SigningHttpMessageHandler.cs` begins:
```csharp
using NSec.Cryptography;

namespace Interledger.OpenPayments.HttpSignatureUtils;

/// <summary>
/// A <see cref="DelegatingHandler"/> that signs every outgoing request using HTTP Message
```

(Types in the same project reference each other without `using`s since they now share one namespace. The two test projects resolve these types through their enclosing namespaces — `Interledger.OpenPayments.HttpSignatureUtils.Tests` encloses `Interledger.OpenPayments.HttpSignatureUtils` — so they need no edits.)

- [ ] **Step 2: Add the `using` where the SDK project consumes these types**

Modify `OpenPayments.Sdk/Extensions/ServiceCollectionExtensions.cs` — the file references `SigningHttpMessageHandler` (wired in Phase 1 Task 4). Replace its using block:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Interledger.OpenPayments.Clients;
using Interledger.OpenPayments.Configuration;
```
with:
```csharp
using Microsoft.Extensions.DependencyInjection;
using Interledger.OpenPayments.Clients;
using Interledger.OpenPayments.Configuration;
using Interledger.OpenPayments.HttpSignatureUtils;
```

- [ ] **Step 3: Build and run all tests**

Run: `dotnet build --configuration Release`
Expected: succeeds. Any `CS0246` points at a consumer of a moved type that needs the same `using Interledger.OpenPayments.HttpSignatureUtils;` — add it there and re-build (the only known consumer outside the project itself is `ServiceCollectionExtensions.cs`, handled in Step 2).

Run: `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj && dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj`
Expected: all PASS.

- [ ] **Step 4: Commit**

```bash
git add OpenPayments.Sdk.HttpSignatureUtils OpenPayments.Sdk/Extensions/ServiceCollectionExtensions.cs
git commit -m "refactor!: move global-namespace signature utils into Interledger.OpenPayments.HttpSignatureUtils"
```

---

### Task 3: Switch NSwag to types-only generation; hand-own the HTTP plumbing

**Files:**
- Create: `.config/dotnet-tools.json`
- Create: `OpenPayments.Sdk/Generated/GeneratedClientBase.cs`
- Create: `OpenPayments.Sdk/Generated/Auth/AuthServerClient.Core.cs`
- Create: `OpenPayments.Sdk/Generated/Resource/ResourceServerClient.Core.cs`
- Create: `OpenPayments.Sdk/Generated/Wallet/WalletAddressClient.Core.cs`
- Delete: `OpenPayments.Sdk/Generated/Auth/AuthServerClient.Auth.cs`
- Delete: `OpenPayments.Sdk/Generated/Resource/ResourceServerClient.Auth.cs`
- Modify: `Makefile` (full rewrite)
- Modify: `OpenPayments.Sdk/OpenPaymentsApiException.cs` (add `innerException` support)
- Modify: `OpenPayments.Sdk/Generated/{Auth,Resource,Wallet}/*.g.cs` (regenerated by `make models` — never by hand)
- Modify: `OpenPayments.Sdk/Generated/Auth/AuthServerClient.Methods.Grant.cs`, `…Methods.Token.cs`, `OpenPayments.Sdk/Generated/Resource/ResourceServerClient.Methods.{IncomingPayment,OutgoingPayment,Quote}.cs`, `OpenPayments.Sdk/Generated/Wallet/WalletAddressClient.Methods.cs` (doc-comment `cref` sweep only)
- Test: `OpenPayments.Sdk.Tests/Clients/UnauthenticatedClient_Tests.cs`

**Interfaces:**
- Consumes: `OpenPaymentsExceptionFactory.Create(...)` (Phase 1 Task 5), `AuthContractResolver`/`ResourceContractResolver` (existing), `Helpers.ExtractHeaders` (existing, `Interledger.OpenPayments.Generated`), the post-Phase-1 hand-written `*.Methods.*.cs` partials (unchanged except doc comments).
- Produces:
  - `public abstract class GeneratedClientBase` (namespace `Interledger.OpenPayments.Generated`) with `protected readonly HttpClient _httpClient`, ctor `(HttpClient httpClient, JsonSerializerSettings serializerSettings)`, `protected JsonSerializerSettings JsonSerializerSettings { get; }`, `public bool ReadResponseAsString { get; set; }`, `protected readonly struct ObjectResponseResult<T>` (`T Object`, `string Text`), `protected static Task<string> ReadAsStringAsync(HttpContent?, CancellationToken)`, `protected virtual Task<ObjectResponseResult<T>> ReadObjectResponseAsync<T>(HttpResponseMessage?, IReadOnlyDictionary<string, IEnumerable<string>>, CancellationToken)` (throws `OpenPaymentsApiException` on JSON failure), `protected static string ConvertToString(object?, CultureInfo)`, `protected static string NormalizeBaseUrl(Uri)`.
  - `AuthServerClient`, `ResourceServerClient`, `WalletAddressClient` each remain `public partial class`, now inheriting `GeneratedClientBase`, each with ctor `(HttpClient httpClient)` (signature unchanged from callers' perspective — `AuthClientBase`, `ResourceClientBase`, `WalletAddressClientBase` construct them exactly as before). `AuthServerClient.ClientUrl` and `ResourceServerClient.ClientUrl` (`Uri`, `= default!`) are preserved.
  - `OpenPaymentsApiException` gains an optional `Exception? innerException` constructor parameter; `OpenPaymentsExceptionFactory.Create` gains the matching optional parameter.
  - `.g.cs` files contain only DTOs and enums (no client classes, no `ApiException`, no hard-coded `BaseUrl` values).

- [ ] **Step 1: Write the failing test — deserialization failure raises `OpenPaymentsApiException`**

Add to `OpenPayments.Sdk.Tests/Clients/UnauthenticatedClient_Tests.cs`, inside the `UnauthenticatedClient_WalletAddress_Tests` class:

```csharp
        [Fact]
        public async Task GetWalletAddressAsync_MalformedJson_ThrowsOpenPaymentsApiException()
        {
            var httpClient = _fixture.CreateHttpClientMock(HttpStatusCode.OK, "{ this is not json");
            var client = new UnauthenticatedClient(httpClient);

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(
                () => client.GetWalletAddressAsync("https://example.com/alice")
            );

            exception.StatusCode.Should().Be(200);
            exception.InnerException.Should().BeAssignableTo<Newtonsoft.Json.JsonException>();
        }
```

Add `using System.Net;` to the file's usings if not already present (the `CreateHttpClientMock(HttpStatusCode, string)` overload was added to `UnauthenticatedClientFixture` in Phase 1 Task 8; `OpenPaymentsApiException` resolves through the enclosing `Interledger.OpenPayments` namespace).

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj --filter FullyQualifiedName~GetWalletAddressAsync_MalformedJson_ThrowsOpenPaymentsApiException`
Expected: FAIL — today the generated `WalletAddressClient.ReadObjectResponseAsync` in `WalletAddressClient.g.cs` throws the generated `Interledger.OpenPayments.Generated.Wallet.ApiException` on the `JsonReaderException`, not `OpenPaymentsApiException`.

- [ ] **Step 3: Add `innerException` support to `OpenPaymentsApiException` and the factory**

Modify `OpenPayments.Sdk/OpenPaymentsApiException.cs` — replace:
```csharp
    public OpenPaymentsApiException(
        string message,
        int statusCode,
        string? errorCode,
        string? rawResponse,
        IReadOnlyDictionary<string, IEnumerable<string>> headers
    )
        : base(message)
    {
```
with:
```csharp
    public OpenPaymentsApiException(
        string message,
        int statusCode,
        string? errorCode,
        string? rawResponse,
        IReadOnlyDictionary<string, IEnumerable<string>> headers,
        Exception? innerException = null
    )
        : base(message, innerException)
    {
```
and replace the factory:
```csharp
    public static OpenPaymentsApiException Create(
        string message,
        int statusCode,
        string? errorCode,
        string? rawResponse,
        IReadOnlyDictionary<string, IEnumerable<string>> headers
    ) => new(message, statusCode, errorCode, rawResponse, headers);
```
with:
```csharp
    public static OpenPaymentsApiException Create(
        string message,
        int statusCode,
        string? errorCode,
        string? rawResponse,
        IReadOnlyDictionary<string, IEnumerable<string>> headers,
        Exception? innerException = null
    ) => new(message, statusCode, errorCode, rawResponse, headers, innerException);
```

- [ ] **Step 4: Create `GeneratedClientBase`**

Create `OpenPayments.Sdk/Generated/GeneratedClientBase.cs`:

```csharp
using System.Globalization;
using Newtonsoft.Json;

namespace Interledger.OpenPayments.Generated;

/// <summary>
/// Shared HTTP and serialization plumbing for the hand-written API client partial classes
/// (<c>AuthServerClient</c>, <c>ResourceServerClient</c>, <c>WalletAddressClient</c>).
/// Replaces the infrastructure NSwag used to emit into each <c>*.g.cs</c> file before the
/// switch to types-only generation (<c>/GenerateClientClasses:false</c>).
/// </summary>
public abstract class GeneratedClientBase
{
    protected readonly HttpClient _httpClient;

    protected GeneratedClientBase(HttpClient httpClient, JsonSerializerSettings serializerSettings)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        JsonSerializerSettings = serializerSettings;
    }

    /// <summary>Serializer settings used for request bodies and response parsing.</summary>
    protected JsonSerializerSettings JsonSerializerSettings { get; }

    /// <summary>
    /// When <c>true</c>, response bodies are buffered as strings before deserialization
    /// (and included in exception details); otherwise they are streamed.
    /// </summary>
    public bool ReadResponseAsString { get; set; }

    protected readonly struct ObjectResponseResult<T>(T responseObject, string responseText)
    {
        public T Object { get; } = responseObject;

        public string Text { get; } = responseText;
    }

    protected static Task<string> ReadAsStringAsync(
        HttpContent? content,
        CancellationToken cancellationToken
    ) => content == null ? Task.FromResult(string.Empty) : content.ReadAsStringAsync(cancellationToken);

    protected virtual async Task<ObjectResponseResult<T>> ReadObjectResponseAsync<T>(
        HttpResponseMessage? response,
        IReadOnlyDictionary<string, IEnumerable<string>> headers,
        CancellationToken cancellationToken
    )
    {
        if (response == null || response.Content == null)
        {
            return new ObjectResponseResult<T>(default!, string.Empty);
        }

        if (ReadResponseAsString)
        {
            var responseText = await ReadAsStringAsync(response.Content, cancellationToken)
                .ConfigureAwait(false);
            try
            {
                var typedBody = JsonConvert.DeserializeObject<T>(responseText, JsonSerializerSettings);
                return new ObjectResponseResult<T>(typedBody!, responseText);
            }
            catch (JsonException exception)
            {
                throw OpenPaymentsExceptionFactory.Create(
                    "Could not deserialize the response body string as " + typeof(T).FullName + ".",
                    (int)response.StatusCode,
                    null,
                    responseText,
                    headers,
                    exception
                );
            }
        }

        try
        {
            using var responseStream = await response
                .Content.ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            using var streamReader = new StreamReader(responseStream);
            using var jsonTextReader = new JsonTextReader(streamReader);
            var serializer = JsonSerializer.Create(JsonSerializerSettings);
            var typedBody = serializer.Deserialize<T>(jsonTextReader);
            return new ObjectResponseResult<T>(typedBody!, string.Empty);
        }
        catch (JsonException exception)
        {
            throw OpenPaymentsExceptionFactory.Create(
                "Could not deserialize the response body stream as " + typeof(T).FullName + ".",
                (int)response.StatusCode,
                null,
                string.Empty,
                headers,
                exception
            );
        }
    }

    protected static string ConvertToString(object? value, CultureInfo cultureInfo)
    {
        switch (value)
        {
            case null:
                return string.Empty;

            case Enum:
            {
                var name = Enum.GetName(value.GetType(), value);
                if (name != null)
                {
                    var field = value.GetType().GetField(name);
                    if (
                        field != null
                        && Attribute.GetCustomAttribute(
                            field,
                            typeof(System.Runtime.Serialization.EnumMemberAttribute)
                        )
                            is System.Runtime.Serialization.EnumMemberAttribute attribute
                    )
                    {
                        return attribute.Value ?? name;
                    }

                    return Convert.ToString(
                            Convert.ChangeType(
                                value,
                                Enum.GetUnderlyingType(value.GetType()),
                                cultureInfo
                            )
                        ) ?? string.Empty;
                }

                break;
            }

            case bool flag:
                return Convert.ToString(flag, cultureInfo).ToLowerInvariant();

            case byte[] bytes:
                return Convert.ToBase64String(bytes);

            case string[] strings:
                return string.Join(",", strings);

            case Array array:
            {
                var items = new List<string>();
                foreach (var item in array)
                    items.Add(ConvertToString(item, cultureInfo));
                return string.Join(",", items);
            }
        }

        return Convert.ToString(value, cultureInfo) ?? string.Empty;
    }

    protected static string NormalizeBaseUrl(Uri baseUri)
    {
        var value = baseUri.ToString();
        return value.EndsWith("/") ? value : value + "/";
    }
}
```

- [ ] **Step 5: Create the three `*.Core.cs` partials and delete the two `.Auth.cs` partials**

Create `OpenPayments.Sdk/Generated/Auth/AuthServerClient.Core.cs`:

```csharp
using Newtonsoft.Json;

namespace Interledger.OpenPayments.Generated.Auth;

public partial class AuthServerClient : GeneratedClientBase
{
    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        ContractResolver = new AuthContractResolver(),
    };

    public AuthServerClient(HttpClient httpClient)
        : base(httpClient, SerializerSettings) { }

    /// <summary>Client wallet address URL sent as the <c>client</c> field of grant requests.</summary>
    public Uri ClientUrl { get; set; } = default!;

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url);

    partial void PrepareRequest(
        HttpClient client,
        HttpRequestMessage request,
        System.Text.StringBuilder urlBuilder
    );

    partial void ProcessResponse(HttpClient client, HttpResponseMessage response);
}
```

Create `OpenPayments.Sdk/Generated/Resource/ResourceServerClient.Core.cs`:

```csharp
using Newtonsoft.Json;

namespace Interledger.OpenPayments.Generated.Resource;

public partial class ResourceServerClient : GeneratedClientBase
{
    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        ContractResolver = new ResourceContractResolver(),
    };

    public ResourceServerClient(HttpClient httpClient)
        : base(httpClient, SerializerSettings) { }

    /// <summary>Client wallet address URL of the SDK consumer.</summary>
    public Uri ClientUrl { get; set; } = default!;

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url);

    partial void PrepareRequest(
        HttpClient client,
        HttpRequestMessage request,
        System.Text.StringBuilder urlBuilder
    );

    partial void ProcessResponse(HttpClient client, HttpResponseMessage response);
}
```

Create `OpenPayments.Sdk/Generated/Wallet/WalletAddressClient.Core.cs`:

```csharp
using Newtonsoft.Json;

namespace Interledger.OpenPayments.Generated.Wallet;

public partial class WalletAddressClient : GeneratedClientBase
{
    private static readonly JsonSerializerSettings SerializerSettings = new();

    public WalletAddressClient(HttpClient httpClient)
        : base(httpClient, SerializerSettings) { }

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url);

    partial void PrepareRequest(
        HttpClient client,
        HttpRequestMessage request,
        System.Text.StringBuilder urlBuilder
    );

    partial void ProcessResponse(HttpClient client, HttpResponseMessage response);
}
```

(The unimplemented `partial void` declarations satisfy the `PrepareRequest(...)`/`ProcessResponse(...)` call sites inside the untouched `*.Methods.*.cs` files — the compiler erases those calls. `NormalizeBaseUrl`, `ReadObjectResponseAsync`, `ConvertToString`, `_httpClient`, and `JsonSerializerSettings` now come from `GeneratedClientBase`. The wallet client keeps default serializer settings, matching today's behavior where its `UpdateJsonSerializerSettings` hook had no implementation.)

Delete the superseded partials:

```bash
git rm OpenPayments.Sdk/Generated/Auth/AuthServerClient.Auth.cs OpenPayments.Sdk/Generated/Resource/ResourceServerClient.Auth.cs
```

- [ ] **Step 6: Pin NSwag as a local dotnet tool**

Create `.config/dotnet-tools.json`:

```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "nswag.consolecore": {
      "version": "14.7.1",
      "commands": ["nswag"]
    }
  }
}
```

- [ ] **Step 7: Rewrite the Makefile with types-only flags and the pinned tool**

Replace the entire contents of `Makefile` with:

```make
.PHONY: tools auth-server-generate as-models resource-server-generate rs-models wallet-address-models wa-models models

NSWAG_FLAGS = /injectHttpClient:true /GenerateClientClasses:false /GenerateExceptionClasses:false /GenerateOptionalPropertiesAsNullable:true /GenerateNullableReferenceTypes:true

tools:
	dotnet tool restore

auth-server-generate: tools
	npx swagger-cli bundle open-payments-specifications/openapi/auth-server.yaml -o OpenPayments.Sdk/tmp/auth-bundled.json -t json && \
	dotnet nswag openapi2csclient /input:OpenPayments.Sdk/tmp/auth-bundled.json /output:OpenPayments.Sdk/Generated/Auth/AuthServerClient.g.cs /namespace:Interledger.OpenPayments.Generated.Auth /classname:AuthServerClient $(NSWAG_FLAGS) && \
	rm -rf OpenPayments.Sdk/tmp/auth-bundled.json

as-models: auth-server-generate

resource-server-generate: tools
	npx swagger-cli bundle open-payments-specifications/openapi/resource-server.yaml -o OpenPayments.Sdk/tmp/resource-bundled.json -t json && \
	dotnet nswag openapi2csclient /input:OpenPayments.Sdk/tmp/resource-bundled.json /output:OpenPayments.Sdk/Generated/Resource/ResourceServerClient.g.cs /namespace:Interledger.OpenPayments.Generated.Resource /classname:ResourceServerClient $(NSWAG_FLAGS) && \
	rm -rf OpenPayments.Sdk/tmp/resource-bundled.json

rs-models: resource-server-generate

wallet-address-models: tools
	npx swagger-cli bundle open-payments-specifications/openapi/wallet-address-server.yaml -o OpenPayments.Sdk/tmp/wallet-bundled.json -t json && \
	dotnet nswag openapi2csclient /input:OpenPayments.Sdk/tmp/wallet-bundled.json /output:OpenPayments.Sdk/Generated/Wallet/WalletAddressClient.g.cs /namespace:Interledger.OpenPayments.Generated.Wallet /classname:WalletAddressClient $(NSWAG_FLAGS) && \
	rm -rf OpenPayments.Sdk/tmp/wallet-bundled.json

wa-models: wallet-address-models

models: as-models rs-models wa-models
```

This unifies the nullable flags across all three specs (previously resource-server-only) and removes the client/exception generation everywhere.

- [ ] **Step 8: Regenerate the `.g.cs` files**

Run: `git submodule update --init && make models`
Expected: all three `.g.cs` files regenerate. Sanity checks:

```bash
grep -c "public partial class\|public enum" OpenPayments.Sdk/Generated/Resource/ResourceServerClient.g.cs   # ~22 type declarations
grep -n "class ApiException\|BaseUrl\|HttpClient" OpenPayments.Sdk/Generated/*/*.g.cs                        # expected: no matches
grep -n "public partial class Body\b\|class Response\b\|class PageInfo" OpenPayments.Sdk/Generated/Resource/ResourceServerClient.g.cs  # Body, Response, PageInfo still generated
```

The `Response`/`Body`/`Body2`/`Body3`/`Response2`/`Response3` inline types **are still generated** in types-only mode (verified against NSwag 14.7.1) — the hand-written subclasses in `Generated/Resource/Types.cs` (`IncomingPaymentBody : Body`, `QuoteBody : Body3`, `ListIncomingPaymentsResponse : Response`, …) keep compiling.

- [ ] **Step 9: Sweep stale `ApiException` doc references from the hand-written partials**

The `*.Methods.*.cs` files still carry `<exception cref="ApiException">` XML doc lines that now point at a deleted type:

```bash
perl -pi -e 's/<exception cref="ApiException">/<exception cref="Interledger.OpenPayments.OpenPaymentsApiException">/g' \
  OpenPayments.Sdk/Generated/Auth/AuthServerClient.Methods.Grant.cs \
  OpenPayments.Sdk/Generated/Auth/AuthServerClient.Methods.Token.cs \
  OpenPayments.Sdk/Generated/Resource/ResourceServerClient.Methods.IncomingPayment.cs \
  OpenPayments.Sdk/Generated/Resource/ResourceServerClient.Methods.OutgoingPayment.cs \
  OpenPayments.Sdk/Generated/Resource/ResourceServerClient.Methods.Quote.cs \
  OpenPayments.Sdk/Generated/Wallet/WalletAddressClient.Methods.cs
```

Verify: `grep -rn "ApiException" --include='*.cs' OpenPayments.Sdk | grep -v OpenPaymentsApiException`
Expected: no output.

- [ ] **Step 10: Build and run all tests**

Run: `dotnet build --configuration Release`
Expected: succeeds. (New *warnings* may appear in `OpenPayments.Snippets` from the now-nullable Auth/Wallet DTO properties — acceptable at this phase; Phase 3 turns warnings into errors and cleans them up. Errors are not acceptable — an error here means a member the hand-written code needs went missing; compare against the pre-regeneration file with `git diff` and check the Makefile flags.)

Run: `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj --filter FullyQualifiedName~GetWalletAddressAsync_MalformedJson_ThrowsOpenPaymentsApiException`
Expected: PASS.

Run: `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj && dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj`
Expected: all PASS.

- [ ] **Step 11: Commit**

```bash
git add -A
git commit -m "refactor!: generate DTOs only; hand-own client HTTP plumbing in GeneratedClientBase"
```

---

### Task 4: One stance on generated code — committed, with a CI drift check (no codegen in build/release)

**Files:**
- Modify: `.github/workflows/build.yaml` (full rewrite)
- Modify: `.github/workflows/release.yaml` (remove codegen steps only)
- Create: `.github/workflows/codegen-check.yaml`

**Interfaces:**
- Consumes: the pinned tool manifest and Makefile from Task 3.
- Produces: CI builds compile the *committed* `.g.cs` files (no Node/NSwag needed, no drift between what's reviewed and what ships); a separate `codegen-check` workflow proves `make models` is a no-op whenever the specs, Makefile, or tool pin change. Phase 3's CI tasks build on these files.

- [ ] **Step 1: Rewrite `build.yaml` without the codegen toolchain**

Replace the entire contents of `.github/workflows/build.yaml` with:

```yaml
name: Build and Test

on:
  workflow_dispatch:
  push:
    paths:
      - 'OpenPayments.Sdk/**'
      - 'OpenPayments.Sdk.Tests/**'
      - 'OpenPayments.Sdk.HttpSignatureUtils/**'
      - 'OpenPayments.Sdk.HttpSignatureUtils.Tests/**'
      - '.github/workflows/build.yaml'
  pull_request:
    paths:
      - 'OpenPayments.Sdk/**'
      - 'OpenPayments.Sdk.Tests/**'
      - 'OpenPayments.Sdk.HttpSignatureUtils/**'
      - 'OpenPayments.Sdk.HttpSignatureUtils.Tests/**'
      - '.github/workflows/build.yaml'

jobs:
  build-and-test:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Setup .NET 9.0 SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Install ReportGenerator
        run: dotnet tool install --global dotnet-reportgenerator-globaltool

      - name: Add dotnet tools to PATH
        run: echo "$HOME/.dotnet/tools" >> $GITHUB_PATH

      - name: Restore dependencies
        run: dotnet restore

      - name: Build solution
        run: dotnet build --no-restore --configuration Release

      - name: Run tests
        run: dotnet test --collect:"XPlat Code Coverage"

      - name: Generate coverage report
        run: reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html -classfilters:"-Interledger.OpenPayments.Generated.*"

      - name: Upload coverage report artifact
        uses: actions/upload-artifact@v4
        with:
          name: coverage-report
          path: coverage-report
```

(This also drops the checkout `submodules: true` — the committed `.g.cs` files build without the spec submodule — and removes the trigger path referencing the nonexistent `.github/workflows/dotnet.yml`, which `IMPROVEMENTS.md` #10 flags; Phase 3 must not re-add it.)

- [ ] **Step 2: Remove the codegen steps from `release.yaml`**

In `.github/workflows/release.yaml`, delete these four steps (leave every other step untouched — Phase 3 overhauls the rest of this file):

```yaml
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '24'

      - name: Install swagger-cli
        run: npm install -g swagger-cli

      - name: Install NSwag CLI
        run: dotnet tool install --global NSwag.ConsoleCore

      - name: Build models
        run: make models
```

and change the checkout step from:
```yaml
      - name: Checkout Code
        uses: actions/checkout@v4
        with:
          submodules: true
```
to:
```yaml
      - name: Checkout Code
        uses: actions/checkout@v4
```

- [ ] **Step 3: Add the drift-check workflow**

Create `.github/workflows/codegen-check.yaml`:

```yaml
name: Codegen Drift Check

on:
  workflow_dispatch:
  pull_request:
    paths:
      - 'open-payments-specifications'
      - 'Makefile'
      - '.config/dotnet-tools.json'
      - '.github/workflows/codegen-check.yaml'

jobs:
  codegen-check:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4
        with:
          submodules: true

      - name: Setup .NET 9.0 SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '24'

      - name: Install swagger-cli
        run: npm install -g swagger-cli

      - name: Regenerate models
        run: make models

      - name: Fail if committed generated code drifted
        run: git diff --exit-code -- 'OpenPayments.Sdk/Generated/**/*.g.cs'
```

- [ ] **Step 4: Validate the workflow YAML and the local build**

Run: `dotnet build --configuration Release && dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj`
Expected: green (workflows don't affect local builds; this confirms nothing else was accidentally touched).

If `actionlint` is available (`which actionlint`), run `actionlint` — expected: no findings. Otherwise rely on GitHub's parser after push.

- [ ] **Step 5: Commit**

```bash
git add .github/workflows/build.yaml .github/workflows/release.yaml .github/workflows/codegen-check.yaml
git commit -m "ci: build committed generated code; add codegen drift check workflow"
```

---

## Verification

After all 4 tasks:

```bash
grep -rn "OpenPayments\.Sdk" --include='*.cs' .            # expected: nothing
grep -rn "ApiException" --include='*.cs' OpenPayments.Sdk | grep -v OpenPaymentsApiException   # expected: nothing
grep -rn "BaseUrl" OpenPayments.Sdk/Generated/*/*.g.cs      # expected: nothing (hard-coded test URLs gone)
dotnet build --configuration Release
dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj
dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj
make models && git diff --exit-code -- 'OpenPayments.Sdk/Generated/**/*.g.cs'   # regeneration is a no-op
```

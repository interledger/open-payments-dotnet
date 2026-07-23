# Phase 3 — A-Grade Packaging, Docs, Tests & CI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring `Interledger.OpenPayments` and `Interledger.OpenPayments.HttpSignatureUtils` to release-grade packaging and repo hygiene: `Directory.Build.props` with warnings-as-errors, SourceLink, symbol packages, analyzers, central package management, one version source (MinVer from git tags), a slimmed crypto dependency set, complete XML docs on the hand-written public surface, corrected naming, and polished README/CHANGELOG/CI (improvements #7 and #10 from `IMPROVEMENTS.md`).

**Architecture:** Infrastructure lands in strict dependency order: SDK pin + central package versions first (so every later edit has one place for versions), then `Directory.Build.props` (metadata, strictness, MinVer), then per-project packaging (TFMs, HttpSignatureUtils metadata), then code-level quality gates (dependency removal, XML docs, Public API tracking, naming fixes), then CI/release/docs which consume all of the above. Portable.BouncyCastle (unmaintained) and Sodium.Core (verified: **zero usages**) are removed; the only BouncyCastle usage is Ed25519 PKCS#8 PEM read/write, replaced by ~40 lines over `System.Security.Cryptography.PemEncoding` and the fixed 16-byte PKCS#8 prefix for Ed25519 (RFC 8410).

**Tech Stack:** .NET SDK 9 (`global.json`-pinned), MinVer 6.0.0, Microsoft.CodeAnalysis.PublicApiAnalyzers 3.3.4, built-in SourceLink (implicit in the .NET 8+ SDK — no package needed), coverlet + ReportGenerator, GitHub Actions.

## Global Constraints

- **Prerequisite:** Phase 1 and Phase 2 plans are fully executed. All namespaces are `Interledger.OpenPayments.*`; test assemblies are `Interledger.OpenPayments.Tests` / `Interledger.OpenPayments.HttpSignatureUtils.Tests`; `.g.cs` files are types-only; CI builds committed generated code (no Node/NSwag in build/release workflows).
- Package IDs stay `Interledger.OpenPayments` and `Interledger.OpenPayments.HttpSignatureUtils`; both multi-target `net8.0;net9.0` after Task 3. Test/snippet projects stay `net9.0`.
- The git tag (`vX.Y.Z`) is the **single** version source (MinVer, `MinVerTagPrefix=v`). No `<Version>` or `-p:PackageVersion` anywhere.
- License: Apache-2.0 (`PackageLicenseExpression`) — the repo `LICENSE` file is Apache 2.0.
- Public-surface renames in Task 7 are intentional breaking changes (pre-1.0 window).
- After every task: `dotnet build --configuration Release` and both test suites green (`dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj`, `dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj`). From Task 2 onward, warnings are errors, so "build succeeds" implies zero warnings.

---

### Task 1: Pin the SDK and centralize package versions

**Files:**
- Create: `global.json`
- Create: `Directory.Packages.props`
- Modify: `OpenPayments.Sdk/OpenPayments.Sdk.csproj`, `OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj`, `OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj`, `OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj`, `OpenPayments.Snippets/OpenPayments.Snippets.csproj`

**Interfaces:**
- Consumes: nothing new.
- Produces: `Directory.Packages.props` as the only place package versions live; every `<PackageReference>` is version-less. This also delivers #10's "align xUnit versions" (HttpSignatureUtils.Tests moves 2.4.2 → 2.9.2) and adds Moq + FluentAssertions there so both test projects share one stack. Tasks 2, 4, 6 add/remove entries in this file.

- [ ] **Step 1: Create `global.json`**

```json
{
  "sdk": {
    "version": "9.0.100",
    "rollForward": "latestFeature"
  }
}
```

- [ ] **Step 2: Create `Directory.Packages.props`**

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="coverlet.collector" Version="6.0.4" />
    <PackageVersion Include="FluentAssertions" Version="8.4.0" />
    <PackageVersion Include="Microsoft.CodeAnalysis.PublicApiAnalyzers" Version="3.3.4" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="9.0.6" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.6" />
    <PackageVersion Include="Microsoft.Extensions.Http" Version="9.0.6" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageVersion Include="MinVer" Version="6.0.0" />
    <PackageVersion Include="Moq" Version="4.20.72" />
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageVersion Include="NSec.Cryptography" Version="25.4.0" />
    <PackageVersion Include="Portable.BouncyCastle" Version="1.9.0" />
    <PackageVersion Include="Sodium.Core" Version="1.4.0" />
    <PackageVersion Include="System.CommandLine" Version="2.0.0-beta5.25306.1" />
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
</Project>
```

(Portable.BouncyCastle and Sodium.Core are still listed — Task 4 deletes them together with their references. Microsoft.CodeAnalysis.PublicApiAnalyzers and MinVer are pre-listed for Tasks 2 and 6.)

- [ ] **Step 3: Strip versions from every `<PackageReference>`**

In each of the five csproj files, remove the `Version="…"` attribute from every `PackageReference` — e.g. in `OpenPayments.Sdk/OpenPayments.Sdk.csproj`:

```xml
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions"/>
    <PackageReference Include="Microsoft.Extensions.Http"/>
    <PackageReference Include="Newtonsoft.Json"/>
    <PackageReference Include="NSec.Cryptography"/>
```

Apply the same attribute-stripping to the other four csprojs (every `PackageReference` line, no exceptions — a leftover `Version=` attribute is an `NU1008` restore error, which makes misses self-detecting).

- [ ] **Step 4: Align the HttpSignatureUtils test stack**

In `OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj`, make the ItemGroup:

```xml
  <ItemGroup>
    <PackageReference Include="coverlet.collector"/>
    <PackageReference Include="FluentAssertions"/>
    <PackageReference Include="Microsoft.NET.Test.Sdk"/>
    <PackageReference Include="Moq"/>
    <PackageReference Include="xunit"/>
    <PackageReference Include="xunit.runner.visualstudio"/>
    <ProjectReference Include="..\OpenPayments.Sdk.HttpSignatureUtils\OpenPayments.Sdk.HttpSignatureUtils.csproj" />
  </ItemGroup>
```

(If the Phase 1 executor already added Moq here for the `SigningHttpMessageHandler` tests, this is a merge, not an addition. xunit jumps 2.4.2 → 2.9.2 via central versions — the existing tests use plain `[Fact]`/`Assert` and compile unchanged.)

- [ ] **Step 5: Build and test**

Run: `dotnet build --configuration Release && dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj && dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj`
Expected: restore + build succeed (any `NU1008` error names a csproj with a leftover `Version=` attribute — remove it), all tests pass.

- [ ] **Step 6: Commit**

```bash
git add global.json Directory.Packages.props OpenPayments.Sdk/OpenPayments.Sdk.csproj OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj OpenPayments.Snippets/OpenPayments.Snippets.csproj
git commit -m "build: pin SDK via global.json and adopt central package management"
```

---

### Task 2: `Directory.Build.props` — shared metadata, warnings-as-errors, SourceLink, symbols, MinVer

**Files:**
- Create: `Directory.Build.props`
- Modify: `OpenPayments.Sdk/OpenPayments.Sdk.csproj` (remove duplicated/superseded properties)
- Modify: `OpenPayments.Snippets/**` and/or test files as needed to reach zero warnings

**Interfaces:**
- Consumes: `Directory.Packages.props` (MinVer version).
- Produces: repo-wide `TreatWarningsAsErrors`, shared package metadata (`Authors`, `Company`, `RepositoryUrl`, `PackageLicenseExpression=Apache-2.0`, project URL), SourceLink enablement (`PublishRepositoryUrl`, `EmbedUntrackedSources` — the SourceLink.GitHub provider itself is built into the .NET 8+ SDK), `IncludeSymbols` + `SymbolPackageFormat=snupkg`, and MinVer with `MinVerTagPrefix=v` as the only version source. Task 9's release workflow relies on MinVer; Tasks 5–6 rely on warnings-as-errors as their enforcement mechanism.

- [ ] **Step 1: Create `Directory.Build.props`**

```xml
<Project>
  <PropertyGroup>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest</AnalysisLevel>

    <Authors>Interledger Foundation - Tech Team</Authors>
    <Company>Interledger Foundation</Company>
    <Copyright>Copyright (c) Interledger Foundation and contributors</Copyright>
    <RepositoryUrl>https://github.com/interledger/open-payments-dotnet</RepositoryUrl>
    <RepositoryType>git</RepositoryType>
    <PackageProjectUrl>https://openpayments.dev/</PackageProjectUrl>
    <PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>

    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>

    <MinVerTagPrefix>v</MinVerTagPrefix>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MinVer" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Remove the now-duplicated properties from `OpenPayments.Sdk.csproj`**

Delete these lines from `OpenPayments.Sdk/OpenPayments.Sdk.csproj` (each is now inherited or superseded):

```xml
    <Version>1.0.0</Version>
    <Authors>Interledger Foundation - Tech Team</Authors>
    <RepositoryUrl>https://github.com/interledger/open-payments-dotnet</RepositoryUrl>
    <PackageProjectUrl>https://openpayments.dev/</PackageProjectUrl>
    <PackageLicenseFile>LICENSE</PackageLicenseFile>
```

and delete the packed LICENSE item (the SPDX expression replaces the packed file):

```xml
    <None Include="../LICENSE" Pack="true" PackagePath=""/>
```

Keep `PackageReadmeFile`, `PackageIcon`, `PackageTags`, `Title`, `Product`, and the README/icon `None` items.

- [ ] **Step 3: Build; drive warnings to zero**

Run: `dotnet build --configuration Release`

Every remaining warning is now an error. Expected fallout and fixes:
- **`OpenPayments.Snippets` nullable warnings (CS86xx)** from Phase 2's nullable-annotation unification on Auth/Wallet DTOs: at each reported site, either guard (`?? throw new InvalidOperationException("…missing in response")`) when the value is required by the guide's logic, or use `?.`/null-forgiving `!` where the mock/guide guarantees presence. Fix exactly the sites the compiler lists.
- **`CS1591` in `OpenPayments.Sdk.HttpSignatureUtils`** (it has `GenerateDocumentationFile=true` and no `NoWarn`): add a `/// <summary>…</summary>` to each listed member describing what it does (these are signature-utility types: builders/parsers/validators). The SDK project itself still has `NoWarn=1591` until Task 5.
- Anything else the compiler reports: fix it at the site; do not add `NoWarn`.

Re-run until: build succeeds with zero warnings.

- [ ] **Step 4: Verify MinVer took over versioning**

Run: `dotnet pack OpenPayments.Sdk/OpenPayments.Sdk.csproj --configuration Release -o /tmp/op-pack-check && ls /tmp/op-pack-check`
Expected: a nupkg + snupkg pair whose version derives from the latest `v*` tag (e.g. `Interledger.OpenPayments.1.0.1-alpha.0.N.nupkg` when commits exist after tag `v1.0.0` — MinVer's height suffix), **not** the old hardcoded `1.0.0`. Delete `/tmp/op-pack-check` afterwards.

- [ ] **Step 5: Run both test suites**

Run: `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj && dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj`
Expected: all PASS.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "build: add Directory.Build.props (warnings-as-errors, SourceLink, snupkg, MinVer, shared metadata)"
```

---

### Task 3: Multi-target `net8.0;net9.0` and fix HttpSignatureUtils package metadata

**Files:**
- Modify: `OpenPayments.Sdk/OpenPayments.Sdk.csproj`
- Modify: `OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj`
- Create: `OpenPayments.Sdk.HttpSignatureUtils/README.md`

**Interfaces:**
- Consumes: `Directory.Build.props` metadata (Task 2).
- Produces: both shipped packages target `net8.0;net9.0`; `Interledger.OpenPayments.HttpSignatureUtils` finally ships with a description, correct authors (inherited), tags, readme, icon, and repo URL instead of near-empty metadata.

- [ ] **Step 1: Multi-target the SDK project**

In `OpenPayments.Sdk/OpenPayments.Sdk.csproj`, replace:
```xml
    <TargetFramework>net9.0</TargetFramework>
```
with:
```xml
    <TargetFrameworks>net8.0;net9.0</TargetFrameworks>
```

- [ ] **Step 2: Rewrite the HttpSignatureUtils csproj**

Replace the entire contents of `OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net9.0</TargetFrameworks>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <RootNamespace>Interledger.OpenPayments.HttpSignatureUtils</RootNamespace>
    <AssemblyName>Interledger.OpenPayments.HttpSignatureUtils</AssemblyName>
    <PackageId>Interledger.OpenPayments.HttpSignatureUtils</PackageId>
    <Title>OpenPayments HTTP Signature Utils</Title>
    <Product>Interledger.OpenPayments.HttpSignatureUtils</Product>
    <Description>HTTP Message Signatures (RFC 9421) utilities for the Open Payments APIs: Ed25519 key loading/generation, JWK export, request signing, and signature validation. Used by and published alongside Interledger.OpenPayments.</Description>
    <PackageTags>interledger openpayments http-signatures ed25519 gnap</PackageTags>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageIcon>icon.png</PackageIcon>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="NSec.Cryptography"/>
    <PackageReference Include="Portable.BouncyCastle"/>
    <PackageReference Include="Sodium.Core"/>
    <PackageReference Include="Newtonsoft.Json"/>
    <InternalsVisibleTo Include="Interledger.OpenPayments.HttpSignatureUtils.Tests"/>
    <None Include="README.md" Pack="true" PackagePath=""/>
    <None Include="../OpenPayments.Sdk/icon.png" Pack="true" PackagePath=""/>
  </ItemGroup>
</Project>
```

(The bogus `Authors` value — previously set to the package name — is gone; the real authors now inherit from `Directory.Build.props`. The two crypto packages disappear in Task 4.)

- [ ] **Step 3: Create the package README**

Create `OpenPayments.Sdk.HttpSignatureUtils/README.md`:

````markdown
# Interledger.OpenPayments.HttpSignatureUtils

HTTP Message Signatures (RFC 9421) utilities for the [Open Payments](https://openpayments.dev/) APIs:

- Ed25519 key generation, loading (raw, Base64, PKCS#8 PEM), and persistence — `KeyUtils`
- JWK (`kty: OKP`, `crv: Ed25519`) export for publishing client keys — `KeyUtils.GenerateJwk`
- Request signing (`Signature` / `Signature-Input` headers) — `HttpRequestSigner`, `SigningHttpMessageHandler`
- Signature validation for servers and tests — `HttpSignatureValidator`

This package is consumed by [`Interledger.OpenPayments`](https://www.nuget.org/packages/Interledger.OpenPayments),
the Open Payments .NET SDK — install that package instead if you want the full client. Install this package
directly when you only need key management or HTTP signature primitives.

```csharp
using Interledger.OpenPayments.HttpSignatureUtils;

var key = KeyUtils.LoadOrGenerateKey("private-key.pem");
var jwk = KeyUtils.GenerateJwk("my-key-id", key);
```

Source, issues, and license (Apache-2.0): https://github.com/interledger/open-payments-dotnet
````

- [ ] **Step 4: Build both TFMs and run tests**

Run: `dotnet build --configuration Release`
Expected: success for `net8.0` and `net9.0` targets of both shipped projects. (`ArgumentException.ThrowIfNullOrWhiteSpace`, `ReadAsStringAsync(CancellationToken)`, and C#12 collection expressions all exist on net8.0 — no conditional code needed.)

Run: `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj && dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj`
Expected: all PASS (tests stay net9.0-hosted).

- [ ] **Step 5: Commit**

```bash
git add OpenPayments.Sdk/OpenPayments.Sdk.csproj OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj OpenPayments.Sdk.HttpSignatureUtils/README.md
git commit -m "build: multi-target net8.0;net9.0 and add full HttpSignatureUtils package metadata"
```

---

### Task 4: Drop Portable.BouncyCastle and Sodium.Core

**Files:**
- Create: `OpenPayments.Sdk.HttpSignatureUtils/Ed25519Pkcs8.cs`
- Modify: `OpenPayments.Sdk.HttpSignatureUtils/KeyUtils.cs` (`LoadPem` rewrite)
- Modify: `OpenPayments.Sdk.HttpSignatureUtils/Extensions/KeyExtensions.cs` (full rewrite)
- Modify: `OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj`, `Directory.Packages.props` (remove the two packages)
- Test: `OpenPayments.Sdk.HttpSignatureUtils.Tests/KeyUtils_LoadPem_Tests.cs`

**Interfaces:**
- Consumes: `KeyUtils.GenerateKey(GenerateKeyArgs)` (existing, writes a PEM via `Key.ToPem`), NSec `Key.Import/Export`.
- Produces: `internal static class Ed25519Pkcs8` with `byte[] Encode(ReadOnlySpan<byte> seed)` and `byte[] DecodeSeed(ReadOnlySpan<byte> der)`; `KeyUtils.LoadPem(string pem)` and `KeyExtensions.ToPem(Key, string)` keep their exact public signatures and error behaviors, now implemented on `System.Security.Cryptography.PemEncoding`. NSec.Cryptography becomes the only crypto dependency.

Background (verified): Sodium.Core has zero references in the codebase; BouncyCastle is used only in `KeyUtils.LoadPem` (PEM→seed) and `KeyExtensions.ToPem` (seed→PEM). An RFC 8410 Ed25519 PKCS#8 DER blob is always the fixed 16-byte prefix `30 2e 02 01 00 30 05 06 03 2b 65 70 04 22 04 20` followed by the 32-byte seed.

- [ ] **Step 1: Write the failing tests**

Create `OpenPayments.Sdk.HttpSignatureUtils.Tests/KeyUtils_LoadPem_Tests.cs`:

```csharp
using System;
using System.IO;
using System.Security.Cryptography;
using FluentAssertions;
using NSec.Cryptography;
using Xunit;

namespace Interledger.OpenPayments.HttpSignatureUtils.Tests;

public class KeyUtils_LoadPem_Tests
{
    // Generated with: openssl genpkey -algorithm ed25519
    private const string FixturePem = """
        -----BEGIN PRIVATE KEY-----
        MC4CAQAwBQYDK2VwBCIEILGNquZIIajfyOBSv5HwSbBWCNHRPRud6bogzSznTuLH
        -----END PRIVATE KEY-----
        """;

    // openssl pkey -pubout -outform DER | tail -c 32 | base64 of the fixture key
    private const string FixturePublicKeyBase64 = "OLhFXh/6GCJhiBVPDLj4CIc+dKTZLn+31PiRe9Oq/3E=";

    [Fact]
    public void LoadPem_OpenSslGeneratedPem_ImportsExpectedKey()
    {
        var key = KeyUtils.LoadPem(FixturePem);

        Convert.ToBase64String(key.PublicKey.Export(KeyBlobFormat.RawPublicKey))
            .Should()
            .Be(FixturePublicKeyBase64);
    }

    [Fact]
    public void LoadPem_RoundTripsWithGenerateKey()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            var generated = KeyUtils.GenerateKey(
                new GenerateKeyArgs { Dir = dir, Filename = "roundtrip.pem" }
            );

            var loaded = KeyUtils.LoadPem(File.ReadAllText(Path.Combine(dir, "roundtrip.pem")));

            loaded
                .PublicKey.Export(KeyBlobFormat.RawPublicKey)
                .Should()
                .Equal(generated.PublicKey.Export(KeyBlobFormat.RawPublicKey));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void LoadPem_NotPem_Throws()
    {
        var act = () => KeyUtils.LoadPem("definitely not a pem");

        act.Should().Throw<ArgumentException>().WithMessage("*Invalid PEM*");
    }

    [Fact]
    public void LoadPem_WrongOid_ThrowsMentioningEd25519()
    {
        // Corrupt the Ed25519 OID's last byte (0x70 -> 0x71) in an otherwise valid blob.
        var der = Convert.FromBase64String(
            "MC4CAQAwBQYDK2VwBCIEILGNquZIIajfyOBSv5HwSbBWCNHRPRud6bogzSznTuLH"
        );
        der[11] = 0x71;
        var pem = new string(PemEncoding.Write("PRIVATE KEY", der));

        var act = () => KeyUtils.LoadPem(pem);

        act.Should().Throw<ArgumentException>().WithMessage("*Ed25519*");
    }
}
```

- [ ] **Step 2: Run the tests to verify the state**

Run: `dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj --filter FullyQualifiedName~KeyUtils_LoadPem_Tests`
Expected: the two happy-path tests PASS against the BouncyCastle implementation (they lock in behavior before the swap); `LoadPem_WrongOid_ThrowsMentioningEd25519` passes too (old code checks the OID string); `LoadPem_NotPem_Throws` passes. If all four pass, proceed — these are the safety net for the reimplementation.

- [ ] **Step 3: Implement `Ed25519Pkcs8`**

Create `OpenPayments.Sdk.HttpSignatureUtils/Ed25519Pkcs8.cs`:

```csharp
namespace Interledger.OpenPayments.HttpSignatureUtils;

/// <summary>
/// Minimal PKCS#8 (RFC 5208 / RFC 8410) encoding and decoding for Ed25519 private keys,
/// replacing the previous Portable.BouncyCastle dependency. An Ed25519 PKCS#8 blob is the
/// fixed prefix below followed by the 32-byte seed.
/// </summary>
internal static class Ed25519Pkcs8
{
    // SEQUENCE(46) { INTEGER 0, SEQUENCE(5) { OID 1.3.101.112 }, OCTET STRING(34) { OCTET STRING(32) seed } }
    private static readonly byte[] Prefix =
    [
        0x30, 0x2e,                                     // SEQUENCE, 46 bytes
        0x02, 0x01, 0x00,                               // INTEGER 0 (version)
        0x30, 0x05, 0x06, 0x03, 0x2b, 0x65, 0x70,       // AlgorithmIdentifier { OID 1.3.101.112 (Ed25519) }
        0x04, 0x22,                                     // OCTET STRING, 34 bytes (privateKey)
        0x04, 0x20,                                     // inner OCTET STRING, 32 bytes (CurvePrivateKey seed)
    ];

    private static readonly byte[] Ed25519Oid = [0x06, 0x03, 0x2b, 0x65, 0x70];

    public static byte[] Encode(ReadOnlySpan<byte> seed)
    {
        if (seed.Length != 32)
            throw new ArgumentException("Ed25519 seed must be 32 bytes.", nameof(seed));

        var der = new byte[Prefix.Length + seed.Length];
        Prefix.CopyTo(der, 0);
        seed.CopyTo(der.AsSpan(Prefix.Length));
        return der;
    }

    public static byte[] DecodeSeed(ReadOnlySpan<byte> der)
    {
        // Fast path: the canonical RFC 8410 layout.
        if (der.Length == Prefix.Length + 32 && der.StartsWith(Prefix))
            return der[Prefix.Length..].ToArray();

        // Tolerant path: verify structure field by field so the error names the real problem
        // (mirrors the OID check and single/double OCTET STRING handling of the old
        // BouncyCastle-based implementation).
        if (der.Length < 16 || der[0] != 0x30 || der[2] != 0x02 || der[3] != 0x01 || der[4] != 0x00)
            throw new ArgumentException("Not a PKCS#8 private key.");

        if (der[5] != 0x30 || !der[7..12].SequenceEqual(Ed25519Oid))
            throw new ArgumentException(
                "Unexpected key algorithm. Expected Ed25519 (OID 1.3.101.112)."
            );

        if (der[12] != 0x04)
            throw new ArgumentException("Malformed PKCS#8 private key: missing OCTET STRING.");

        int length = der[13];
        if (14 + length > der.Length)
            throw new ArgumentException("Malformed PKCS#8 private key: truncated OCTET STRING.");

        var content = der.Slice(14, length);

        // Standard double-wrap: OCTET STRING(34) containing OCTET STRING(32).
        if (length == 34 && content[0] == 0x04 && content[1] == 0x20)
            return content[2..].ToArray();

        // Some toolchains emit the seed directly.
        if (length == 32)
            return content.ToArray();

        throw new ArgumentException($"Ed25519 seed must be 32 bytes, got {length}.");
    }
}
```

- [ ] **Step 4: Rewrite `KeyUtils.LoadPem` on `PemEncoding`**

In `OpenPayments.Sdk.HttpSignatureUtils/KeyUtils.cs`, delete the three BouncyCastle usings:
```csharp
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.OpenSsl;
```
and replace the entire `LoadPem` method body (keep its XML doc comment) with:

```csharp
    public static Key LoadPem(string pem)
    {
        if (!PemEncoding.TryFind(pem, out var fields))
            throw new ArgumentException("Invalid PEM");

        var label = pem[fields.Label];
        if (label != "PRIVATE KEY")
            throw new ArgumentException(
                $"Unexpected PEM label: {label}. Expected a PKCS#8 \"PRIVATE KEY\" block."
            );

        var der = Convert.FromBase64String(pem[fields.Base64Data]);
        var seed = Ed25519Pkcs8.DecodeSeed(der);

        return Key.Import(SignatureAlgorithm.Ed25519, seed, KeyBlobFormat.RawPrivateKey);
    }
```

(`PemEncoding` lives in `System.Security.Cryptography`, already imported at the top of `KeyUtils.cs`.)

- [ ] **Step 5: Rewrite `KeyExtensions.ToPem`**

Replace the entire contents of `OpenPayments.Sdk.HttpSignatureUtils/Extensions/KeyExtensions.cs` with:

```csharp
using System.Security.Cryptography;
using NSec.Cryptography;

namespace Interledger.OpenPayments.HttpSignatureUtils;

internal static class KeyExtensions
{
    public static void ToPem(this Key key, string filePath)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required.", nameof(filePath));

        var seed = key.Export(KeyBlobFormat.RawPrivateKey);
        var pkcs8 = Ed25519Pkcs8.Encode(seed);

        File.WriteAllText(filePath, new string(PemEncoding.Write("PRIVATE KEY", pkcs8)) + "\n");
    }
}
```

- [ ] **Step 6: Remove the two packages**

In `OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj`, delete:
```xml
    <PackageReference Include="Portable.BouncyCastle"/>
    <PackageReference Include="Sodium.Core"/>
```
In `Directory.Packages.props`, delete:
```xml
    <PackageVersion Include="Portable.BouncyCastle" Version="1.9.0" />
    <PackageVersion Include="Sodium.Core" Version="1.4.0" />
```

- [ ] **Step 7: Run the tests to verify they pass**

Run: `dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj`
Expected: all PASS — including the four `KeyUtils_LoadPem_Tests` (identical observable behavior on the new implementation) and every pre-existing `KeyUtils_*`/signing test.

Run: `grep -rn "BouncyCastle\|Sodium" --include='*.cs' --include='*.csproj' --include='*.props' .`
Expected: no output.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "deps: replace Portable.BouncyCastle PEM handling with PemEncoding; drop unused Sodium.Core"
```

---

### Task 5: XML docs — drop `NoWarn 1591`, document the hand-written public surface

**Files:**
- Modify: `OpenPayments.Sdk/OpenPayments.Sdk.csproj` (remove `NoWarn`)
- Create: `OpenPayments.Sdk/Generated/.editorconfig`
- Modify: `OpenPayments.Sdk/Clients/AuthClientBase.cs`, `OpenPayments.Sdk/Clients/ResourceClientBase.cs`, `OpenPayments.Sdk/Clients/AuthenticatedClient.cs`, `OpenPayments.Sdk/OpenPaymentsApiException.cs`

**Interfaces:**
- Consumes: warnings-as-errors (Task 2) as the enforcement mechanism.
- Produces: `CS1591` enforced everywhere except the `Generated/` folder (generated `.g.cs` files self-suppress via their own `#pragma`; the hand-written DTO alias files in `Generated/` are exempted by a folder-scoped `.editorconfig`). All public hand-written client types are documented.

- [ ] **Step 1: Remove the suppression and add the Generated-folder exemption**

In `OpenPayments.Sdk/OpenPayments.Sdk.csproj`, delete:
```xml
    <NoWarn>1591</NoWarn>
```

Create `OpenPayments.Sdk/Generated/.editorconfig`:
```ini
[*.cs]
# Generated DTOs and the hand-written type aliases/partials that extend them are exempt
# from XML-doc coverage; the curated public surface lives outside this folder.
dotnet_diagnostic.CS1591.severity = none
```

- [ ] **Step 2: Build to enumerate the gaps**

Run: `dotnet build --configuration Release 2>&1 | grep -E "error CS1591" | sort -u`
Expected: a finite list of members in `Clients/` and the root `OpenPaymentsApiException.cs`. (If members under `Generated/` appear, the `.editorconfig` isn't being honored — fall back to adding `#pragma warning disable 1591` as the first line of each hand-written file in `Generated/` that the compiler names.)

- [ ] **Step 3: Document `IAuthClientBase` and `AuthClientBase`**

In `OpenPayments.Sdk/Clients/AuthClientBase.cs`, replace the `IAuthClientBase` interface declaration with this documented version:

```csharp
/// <summary>
/// Low-level client for the Open Payments authorization server (GNAP): grant lifecycle
/// and access-token management. Wrapped by <see cref="IAuthenticatedClient"/>, which is
/// the surface most consumers should use.
/// </summary>
public interface IAuthClientBase
{
    /// <summary>Requests a new grant from the authorization server.</summary>
    /// <param name="requestArgs">Authorization server grant endpoint URL.</param>
    /// <param name="body">The grant request (requested access, client, optional interact).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public Task<AuthResponse> RequestGrantAsync(
        RequestArgs requestArgs,
        GrantCreateBody body,
        CancellationToken cancellationToken = default
    );

    /// <summary>Continues a pending (interactive) grant.</summary>
    /// <param name="requestArgs">Continue URI and continuation access token from the initial grant response.</param>
    /// <param name="body">The continuation request (interaction reference).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public Task<AuthResponse> ContinueGrantAsync(
        AuthRequestArgs requestArgs,
        GrantContinueBody body,
        CancellationToken cancellationToken = default
    );

    /// <summary>Cancels a grant, revoking any access it carries.</summary>
    /// <param name="requestArgs">Grant management URL and access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task CancelGrantAsync(AuthRequestArgs requestArgs, CancellationToken cancellationToken);

    /// <summary>Rotates an access token, returning a newly issued replacement.</summary>
    /// <param name="requestArgs">Token management URL and current access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<RotateTokenResponse> RotateTokenAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken
    );

    /// <summary>Revokes an access token, rendering it invalid.</summary>
    /// <param name="requestArgs">Token management URL and access token.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public Task RevokeTokenAsync(
        AuthRequestArgs requestArgs,
        CancellationToken cancellationToken = default
    );
}
```

Then, on the `AuthClientBase` class itself: add above the class declaration

```csharp
/// <summary>Default <see cref="IAuthClientBase"/> implementation over <see cref="AuthServerClient"/>.</summary>
```

add above the constructor

```csharp
    /// <summary>Creates the client. Signing must already be configured on <paramref name="http"/>'s handler pipeline.</summary>
    /// <param name="http">The HTTP client used for all requests.</param>
    /// <param name="clientUrl">Client wallet address URL, sent as the <c>client</c> field of grant requests.</param>
```

and add `/// <inheritdoc/>` directly above each of the five public methods (`RequestGrantAsync`, `ContinueGrantAsync`, `CancelGrantAsync`, `RotateTokenAsync`, `RevokeTokenAsync`).

- [ ] **Step 4: Document `IResourceClientBase` and `ResourceClientBase`**

In `OpenPayments.Sdk/Clients/ResourceClientBase.cs`, give the interface the same treatment — replace the bare `IResourceClientBase` declaration header and add a `<summary>` per member:

```csharp
/// <summary>
/// Low-level client for the Open Payments resource server: incoming payments, quotes, and
/// outgoing payments. Wrapped by <see cref="IAuthenticatedClient"/>, which is the surface
/// most consumers should use.
/// </summary>
public interface IResourceClientBase
```

with member docs (add above each corresponding member, keeping signatures untouched):

```csharp
    /// <summary>Creates an incoming payment on the receiving wallet address.</summary>
    /// <summary>Fetches the latest state of an incoming payment.</summary>
    /// <summary>Marks an incoming payment as completed.</summary>
    /// <summary>Lists incoming payments on a wallet address, one page at a time.</summary>
    /// <summary>Creates a quote for a future outgoing payment.</summary>
    /// <summary>Fetches a quote.</summary>
    /// <summary>Creates an outgoing payment.</summary>
    /// <summary>Fetches the latest state of an outgoing payment.</summary>
    /// <summary>Lists outgoing payments on a wallet address, one page at a time.</summary>
```

(Each line above goes on the member it describes, in the order the members appear in the interface. Each method's `requestArgs`/`body`/`query`/`cancellationToken` parameters get `<param>` tags following the same pattern as `IAuthClientBase` in Step 3 — resource server URL + access token for `requestArgs`, request payload for `body`, filter/paging parameters for `query`.)

Then on `ResourceClientBase`: class summary

```csharp
/// <summary>Default <see cref="IResourceClientBase"/> implementation over <see cref="ResourceServerClient"/>.</summary>
```

constructor doc (same two-parameter text as `AuthClientBase` in Step 3), and `/// <inheritdoc/>` above each of the nine public methods.

- [ ] **Step 5: Document `RequestArgs`, `AuthRequestArgs`, and the exception constructor**

In `OpenPayments.Sdk/Clients/AuthenticatedClient.cs`, replace the two trailing classes with:

```csharp
/// <summary>Target of an unauthenticated Open Payments request.</summary>
public class RequestArgs
{
    /// <summary>Absolute URL of the resource or endpoint to call.</summary>
    public required Uri Url { get; set; }
}

/// <summary>Target of an authenticated Open Payments request.</summary>
public class AuthRequestArgs : RequestArgs
{
    /// <summary>GNAP access token authorizing the request.</summary>
    public required string AccessToken { get; set; }
}
```

In `OpenPayments.Sdk/OpenPaymentsApiException.cs`, add above the constructor:

```csharp
    /// <summary>Creates the exception. See the property documentation for parameter semantics.</summary>
```

and above the `ToString` override:

```csharp
    /// <inheritdoc/>
```

- [ ] **Step 6: Build until zero CS1591 remains, run tests, commit**

Run: `dotnet build --configuration Release`
Expected: success (any residual CS1591 error names the member — document it in the same style).

Run: `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj && dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj`
Expected: all PASS.

```bash
git add -A
git commit -m "docs: enforce XML docs (drop NoWarn 1591) and document the public client surface"
```

---

### Task 6: Public API tracking with PublicApiAnalyzers

**Files:**
- Modify: `OpenPayments.Sdk/OpenPayments.Sdk.csproj`, `OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj`
- Create: `OpenPayments.Sdk/PublicAPI.Shipped.txt`, `OpenPayments.Sdk/PublicAPI.Unshipped.txt`, `OpenPayments.Sdk.HttpSignatureUtils/PublicAPI.Shipped.txt`, `OpenPayments.Sdk.HttpSignatureUtils/PublicAPI.Unshipped.txt`

**Interfaces:**
- Consumes: warnings-as-errors (Task 2).
- Produces: every public symbol of both shipped assemblies is tracked in `PublicAPI.*.txt`; any accidental public-surface change breaks the build (RS0016/RS0017). Task 7 and all Phase 4 public-surface work must update `PublicAPI.Unshipped.txt`.

- [ ] **Step 1: Add the analyzer and the (empty) API files**

Add to the `<ItemGroup>` of **both** shipped csprojs:

```xml
    <PackageReference Include="Microsoft.CodeAnalysis.PublicApiAnalyzers" PrivateAssets="all"/>
    <AdditionalFiles Include="PublicAPI.Shipped.txt"/>
    <AdditionalFiles Include="PublicAPI.Unshipped.txt"/>
```

Create all four text files, each with exactly this single line (nothing has shipped under the new namespaces yet, so everything goes to Unshipped):

```
#nullable enable
```

- [ ] **Step 2: Build to enumerate the surface, then auto-populate**

Run: `dotnet build --configuration Release 2>&1 | grep -c "RS0016"`
Expected: a large count — every undeclared public symbol.

Run: `dotnet format analyzers OpenPayments.sln --diagnostics RS0016 --severity warn`
Expected: the "add to public API" code fix fills both `PublicAPI.Unshipped.txt` files. If `dotnet format` does not apply the fix (it prints "0 fixed"), fall back to manual population: for each `RS0016` error, copy the symbol string from the diagnostic message (`Symbol 'X' is not part of the declared public API`) into the owning project's `PublicAPI.Unshipped.txt`, one per line, below `#nullable enable`.

- [ ] **Step 3: Build clean, run tests, commit**

Run: `dotnet build --configuration Release`
Expected: success, zero RS-diagnostics. (If `RS0041`/`RS0037` appear, ensure both files start with `#nullable enable`; if a symbol is listed twice, dedupe the file.)

Run: `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj`
Expected: PASS.

```bash
git add -A
git commit -m "build: track the public API surface with PublicApiAnalyzers"
```

---

### Task 7: Fix public naming — `CompleteIncomingPaymentAsync` and `ListOutgoingPaymentsAsync`

**Files:**
- Modify: `OpenPayments.Sdk/Clients/IAuthenticatedClient.cs`, `OpenPayments.Sdk/Clients/AuthenticatedClient.cs`, `OpenPayments.Sdk/Clients/ResourceClientBase.cs`
- Modify: every caller in `OpenPayments.Sdk.Tests/**` and `OpenPayments.Snippets/**` (scripted)
- Modify: `OpenPayments.Sdk/PublicAPI.Unshipped.txt`

**Interfaces:**
- Consumes: the Task 6 API files (must be updated in the same commit).
- Produces: `IAuthenticatedClient.CompleteIncomingPaymentAsync(AuthRequestArgs, CancellationToken)` (was `CompleteIncomingPayments…`) and `IResourceClientBase.ListOutgoingPaymentsAsync(AuthRequestArgs, ListOutgoingPaymentQuery, CancellationToken)` (was `ListOutgoingPayment…` on the class/interface). Phase 4's pagination work calls `ListOutgoingPaymentsAsync` under this corrected name.

- [ ] **Step 1: Run the scripted rename**

```bash
perl -pi -e 's/CompleteIncomingPaymentsAsync/CompleteIncomingPaymentAsync/g; s/ListOutgoingPaymentAsync/ListOutgoingPaymentsAsync/g' $(git ls-files '*.cs')
```

(The second pattern cannot touch the already-correct `ListOutgoingPaymentsAsync` occurrences — `PaymentAsync` ≠ `PaymentsAsync`. The first only hits the wrongly-pluralized complete-method. This renames the interface members, both implementations, the `_resClient.ListOutgoingPaymentAsync(...)` delegation call inside `AuthenticatedClient`, and every test/snippet call site in one pass.)

- [ ] **Step 2: Fix the stale doc line on the renamed member**

In `OpenPayments.Sdk/Clients/IAuthenticatedClient.cs`, the renamed `CompleteIncomingPaymentAsync` still carries a wrong `<returns>` tag — replace:
```csharp
    /// <returns>ListIncomingPaymentsResponse</returns>
```
(the occurrence directly above `CompleteIncomingPaymentAsync`) with:
```csharp
    /// <returns>The completed incoming payment.</returns>
```

- [ ] **Step 3: Update the public API declaration**

Run: `dotnet build --configuration Release 2>&1 | grep -E "RS0016|RS0017"`
Expected: RS0017 (removed symbol) for the two old names and RS0016 for the two new names. Edit `OpenPayments.Sdk/PublicAPI.Unshipped.txt`: change `CompleteIncomingPaymentsAsync` → `CompleteIncomingPaymentAsync` and `ListOutgoingPaymentAsync` → `ListOutgoingPaymentsAsync` in the affected lines (or re-run `dotnet format analyzers OpenPayments.sln --diagnostics RS0016 --severity warn` after deleting the two stale lines).

- [ ] **Step 4: Build, test, commit**

Run: `dotnet build --configuration Release && dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj`
Expected: green.

```bash
git add -A
git commit -m "fix!: correct method naming (CompleteIncomingPaymentAsync, ListOutgoingPaymentsAsync)"
```

---

### Task 8: CI build workflow — caching and a coverage floor

**Files:**
- Modify: `.github/workflows/build.yaml` (full rewrite)

**Interfaces:**
- Consumes: the Phase 2 Task 4 version of this file; `Directory.Packages.props` (cache key).
- Produces: cached NuGet restores and a hard line-coverage floor (60%, ratchet upward later).

- [ ] **Step 1: Rewrite `build.yaml`**

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
      - 'Directory.Build.props'
      - 'Directory.Packages.props'
      - 'global.json'
      - '.github/workflows/build.yaml'
  pull_request:
    paths:
      - 'OpenPayments.Sdk/**'
      - 'OpenPayments.Sdk.Tests/**'
      - 'OpenPayments.Sdk.HttpSignatureUtils/**'
      - 'OpenPayments.Sdk.HttpSignatureUtils.Tests/**'
      - 'Directory.Build.props'
      - 'Directory.Packages.props'
      - 'global.json'
      - '.github/workflows/build.yaml'

jobs:
  build-and-test:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Setup .NET SDK
        uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('Directory.Packages.props', '**/*.csproj') }}
          restore-keys: nuget-${{ runner.os }}-

      - name: Install ReportGenerator
        run: dotnet tool install --global dotnet-reportgenerator-globaltool

      - name: Add dotnet tools to PATH
        run: echo "$HOME/.dotnet/tools" >> $GITHUB_PATH

      - name: Restore dependencies
        run: dotnet restore

      - name: Build solution
        run: dotnet build --no-restore --configuration Release

      - name: Run tests with coverage
        run: dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage"

      - name: Generate coverage report
        run: reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:"Html;TextSummary" -classfilters:"-Interledger.OpenPayments.Generated.*"

      - name: Enforce coverage threshold
        run: |
          COVERAGE=$(grep -Eo 'Line coverage: [0-9.]+' coverage-report/Summary.txt | grep -Eo '[0-9.]+' | head -1)
          echo "Line coverage: ${COVERAGE}%"
          awk -v c="$COVERAGE" 'BEGIN { exit !(c + 0 >= 60) }' || { echo "::error::Line coverage ${COVERAGE}% is below the 60% floor"; exit 1; }

      - name: Upload coverage report artifact
        uses: actions/upload-artifact@v4
        with:
          name: coverage-report
          path: coverage-report
```

- [ ] **Step 2: Verify locally what CI will measure**

Run:
```bash
dotnet test --collect:"XPlat Code Coverage"
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"/tmp/coverage-check" -reporttypes:TextSummary -classfilters:"-Interledger.OpenPayments.Generated.*" && grep "Line coverage" /tmp/coverage-check/Summary.txt
```
Expected: a line-coverage figure ≥ 60%. If it is below 60%, lower the floor in the workflow to 5 points beneath the measured value (a floor must never fail on day one) and note the measured number in the commit message.
(Install the tool first if missing: `dotnet tool install --global dotnet-reportgenerator-globaltool`.)

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/build.yaml
git commit -m "ci: cache NuGet restores and enforce a line-coverage floor"
```

---

### Task 9: Release workflow — MinVer, snupkg, skip-duplicate, CHANGELOG

**Files:**
- Modify: `.github/workflows/release.yaml` (full rewrite)
- Create: `CHANGELOG.md`

**Interfaces:**
- Consumes: MinVer configuration (Task 2), snupkg output (Task 2).
- Produces: tag-driven releases where the tag is the only version input; both packages + both symbol packages pushed with `--skip-duplicate`; a committed `CHANGELOG.md` (the generator action is dropped; GitHub release notes are auto-generated).

- [ ] **Step 1: Create `CHANGELOG.md`**

```markdown
# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
Versions are derived from git tags (`vX.Y.Z`) via MinVer.

## [Unreleased]

### Changed
- **Breaking:** all namespaces renamed from `OpenPayments.Sdk.*` to `Interledger.OpenPayments.*`, matching the package ID.
- **Breaking:** all client errors now throw a single `OpenPaymentsApiException` (status code, error code, raw body) instead of per-namespace `ApiException` types.
- **Breaking:** `CompleteIncomingPaymentsAsync` → `CompleteIncomingPaymentAsync`; `ListOutgoingPaymentAsync` → `ListOutgoingPaymentsAsync`.
- Request signing moved to an async `SigningHttpMessageHandler` on the HTTP pipeline (no more sync-over-async blocking).
- NSwag now generates DTOs only; HTTP plumbing is hand-owned and shared.
- Packages multi-target `net8.0;net9.0`, ship SourceLink + snupkg symbols, and version from git tags (MinVer).

### Fixed
- Thread-safety: concurrent requests through a singleton client no longer race on a shared `BaseUrl`.
- Eager validation of `OpenPaymentsOptions` at registration time with clear messages.

### Removed
- Dependencies `Portable.BouncyCastle` (replaced by `PemEncoding` + minimal PKCS#8 handling) and `Sodium.Core` (unused).
```

- [ ] **Step 2: Rewrite `release.yaml`**

Replace the entire contents of `.github/workflows/release.yaml` with:

```yaml
name: Release on Tag

on:
  push:
    tags:
      - 'v*.*.*'

jobs:
  release:
    name: Release & Publish to NuGet
    runs-on: ubuntu-latest
    permissions:
      contents: write

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Setup .NET SDK
        uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('Directory.Packages.props', '**/*.csproj') }}
          restore-keys: nuget-${{ runner.os }}-

      - name: Restore dependencies
        run: dotnet restore

      - name: Build solution
        run: dotnet build --no-restore --configuration Release -p:ContinuousIntegrationBuild=true

      - name: Run tests
        run: dotnet test --no-build --configuration Release

      - name: Pack Interledger.OpenPayments
        run: dotnet pack OpenPayments.Sdk/OpenPayments.Sdk.csproj --configuration Release --no-build -o ./nupkg

      - name: Pack Interledger.OpenPayments.HttpSignatureUtils
        run: dotnet pack OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj --configuration Release --no-build -o ./nupkg

      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          tag_name: ${{ github.ref }}
          name: Release ${{ github.ref_name }}
          generate_release_notes: true

      - name: Push packages to NuGet
        run: dotnet nuget push "./nupkg/*.nupkg" --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate

      - name: Push symbol packages to NuGet
        run: dotnet nuget push "./nupkg/*.snupkg" --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate
```

(`fetch-depth: 0` gives MinVer the tag history; the checked-out tag itself yields the exact `X.Y.Z` version. The duplicated "Pack with version from tag" step names, the redundant second build, the `-p:PackageVersion` override, and the CHANGELOG-generator action are all gone. Coverage reporting stays in `build.yaml` where it belongs.)

- [ ] **Step 3: Commit**

```bash
git add .github/workflows/release.yaml CHANGELOG.md
git commit -m "ci: MinVer-driven release with snupkg push, skip-duplicate, and committed CHANGELOG"
```

---

### Task 10: README overhaul

**Files:**
- Modify: `README.md`

**Interfaces:**
- Consumes: everything shipped above (package set, TFMs, versioning story, local-tool codegen from Phase 2).
- Produces: a README with correct links, both packages documented, and versioning/frameworks/changelog sections.

- [ ] **Step 1: Fix the broken badge and clone URLs**

Replace:
```markdown
[![GitHub Issues](https://img.shields.io/github/issues/interledger/open-payments-dotnet.svg)](https://github.com/kylelobo/open-payments-dotnet/issues)
```
with:
```markdown
[![GitHub Issues](https://img.shields.io/github/issues/interledger/open-payments-dotnet.svg)](https://github.com/interledger/open-payments-dotnet/issues)
[![NuGet](https://img.shields.io/nuget/v/Interledger.OpenPayments.svg)](https://www.nuget.org/packages/Interledger.OpenPayments)
```

Replace (the clone example currently points at the **node** repo):
```markdown
git clone --recurse-submodules git@github.com:interledger/open-payments-node.git
```
with:
```markdown
git clone --recurse-submodules git@github.com:interledger/open-payments-dotnet.git
```

- [ ] **Step 2: Update the environment setup for the pinned local tool**

In the `### Environment Setup` section, replace the fenced bash block whose contents are:

````markdown
```bash
npm install -g swagger-cli && \
dotnet tool install --global NSwag.ConsoleCore
```
````

with this block plus a following paragraph:

````markdown
```bash
npm install -g swagger-cli && \
dotnet tool restore
```

NSwag is pinned as a local dotnet tool in `.config/dotnet-tools.json`; `make models` restores it
automatically. Regenerated code is committed — CI verifies it stays in sync via the
*Codegen Drift Check* workflow.
````

- [ ] **Step 3: Add the Packages, Supported frameworks, Versioning, and Changelog sections**

Insert immediately after the `## 🎈 Usage` section's closing paragraph (`Please visit [OpenPayments Docs]…`):

```markdown
## 📦 Packages

| Package | Description |
|---|---|
| [`Interledger.OpenPayments`](https://www.nuget.org/packages/Interledger.OpenPayments) | The Open Payments SDK: authenticated & unauthenticated clients, DI integration, generated API models. |
| [`Interledger.OpenPayments.HttpSignatureUtils`](https://www.nuget.org/packages/Interledger.OpenPayments.HttpSignatureUtils) | HTTP Message Signature utilities: Ed25519 key management, JWK export, request signing/validation. Installed automatically with the SDK; install directly if you only need signing primitives. |

## 🎯 Supported frameworks

Both packages target `net8.0` and `net9.0`.

## 🔖 Versioning & releases

Releases follow [Semantic Versioning](https://semver.org). The git tag (`vX.Y.Z`) is the single
source of the package version ([MinVer](https://github.com/adamralph/minver)); pushing a tag
publishes both packages (with SourceLink'd `snupkg` symbols) to NuGet.

See the [CHANGELOG](CHANGELOG.md) for notable changes per release.
```

- [ ] **Step 4: Sanity-check the usage snippet still matches the API**

The usage snippet was namespace-corrected in Phase 2 Task 1. Confirm it reads `using Interledger.OpenPayments.Clients;` / `using Interledger.OpenPayments.Extensions;` / `using Interledger.OpenPayments.HttpSignatureUtils;` and that `KeyUtils.LoadPem(...)` is the API it calls (it is — `LoadPem` exists and kept its signature in Task 4). Fix any mismatch to match the real API.

- [ ] **Step 5: Commit**

```bash
git add README.md
git commit -m "docs: fix README links, document both packages, add frameworks/versioning/changelog sections"
```

---

## Verification

After all 10 tasks:

```bash
dotnet build --configuration Release          # zero warnings (TreatWarningsAsErrors)
dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj
dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj

# Pack and inspect both packages
dotnet pack OpenPayments.Sdk/OpenPayments.Sdk.csproj --configuration Release -o /tmp/op-verify
dotnet pack OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj --configuration Release -o /tmp/op-verify
ls /tmp/op-verify        # expect: 2 .nupkg + 2 .snupkg, versions derived from the latest v* tag
cd /tmp/op-verify && for f in *.nupkg; do unzip -o -q "$f" -d "${f%.nupkg}"; done && grep -l "Apache-2.0" */Interledger.*.nuspec && ls */README.md
```

Checklist on the extracted nuspecs: description, authors ("Interledger Foundation - Tech Team"), Apache-2.0 license expression, repository URL, readme, icon — present in **both** packages; no dependency on Portable.BouncyCastle or Sodium.Core; TFM folders `lib/net8.0` and `lib/net9.0` in each.

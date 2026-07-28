# Revert Namespace to OpenPayments.Sdk Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Revert the C# namespace, `AssemblyName`, `RootNamespace`, and `InternalsVisibleTo` identity from `Interledger.OpenPayments.*` back to `OpenPayments.Sdk.*` across the whole codebase, while leaving the NuGet `PackageId`s (`Interledger.OpenPayments`, `Interledger.OpenPayments.HttpSignatureUtils`) unchanged.

**Architecture:** A single scripted `perl` find/replace over every `*.cs` file and the `Makefile` handles all namespace declarations, `using` directives, fully-qualified references, and the in-code `InternalsVisibleTo` attribute in one atomic pass (verified safe — no `.cs` file mixes a `PackageId`/NuGet-identity string literal with namespace usage). The four `.csproj` files, `README.md`, and `.github/workflows/build.yaml` are hand-edited because each contains `PackageId`, badge, or package-name strings that must **not** change alongside the ones that must.

**Tech Stack:** .NET 9 (`net8.0;net9.0` multi-target), xUnit, `perl` (macOS/Linux built-in, used for scripted regex rename), `make`/NSwag for codegen verification.

## Global Constraints

- `PackageId` in `OpenPayments.Sdk.csproj` (`Interledger.OpenPayments`) and `OpenPayments.Sdk.HttpSignatureUtils.csproj` (`Interledger.OpenPayments.HttpSignatureUtils`) must NOT change — verify after every task.
- README badge, `dotnet add package` command, and the package table keep `Interledger.OpenPayments` / `Interledger.OpenPayments.HttpSignatureUtils` — only the `using` snippet lines change.
- `.github/workflows/release.yaml` "Pack Interledger.OpenPayments" / "Pack Interledger.OpenPayments.HttpSignatureUtils" step names are untouched (they name the package being packed, not a namespace).
- Historical docs (`docs/IMPROVEMENTS.md` #5, `docs/superpowers/plans/2026-07-23-phase2-namespaces-and-typegen.md`, any ADRs) and the existing CHANGELOG entry for the original rename are left untouched — this reversal is recorded only via a new CHANGELOG entry.
- After every task: `dotnet build --configuration Release` succeeds and both test suites pass (`dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj`, `dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj`).

---

### Task 1: Revert namespace identity across C# sources, the Makefile, and the four `.csproj` files

**Files:**
- Modify: every tracked `*.cs` file (scripted; includes `.g.cs`, tests, snippets) — 218 occurrences across ~87 files
- Modify: `Makefile` (three `/namespace:` flags)
- Modify: `OpenPayments.Sdk/OpenPayments.Sdk.csproj` (`AssemblyName`, `RootNamespace`, `Product`)
- Modify: `OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj` (`RootNamespace`, `AssemblyName`, `Product`, `InternalsVisibleTo`)
- Modify: `OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj` (`RootNamespace`, `AssemblyName`, `Company`, `Product`)
- Modify: `OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj` (`RootNamespace`, `AssemblyName`)

**Interfaces:**
- Consumes: nothing new.
- Produces: every namespace becomes `OpenPayments.Sdk`, `OpenPayments.Sdk.Clients`, `OpenPayments.Sdk.Configuration`, `OpenPayments.Sdk.Extensions`, `OpenPayments.Sdk.Generated{,.Auth,.Resource,.Wallet}`, `OpenPayments.Sdk.HttpSignatureUtils`, `OpenPayments.Sdk.Tests.*`, `OpenPayments.Sdk.HttpSignatureUtils.Tests`. Assembly names match (`OpenPayments.Sdk.Tests`, `OpenPayments.Sdk.HttpSignatureUtils.Tests`) so `InternalsVisibleTo` (used by `AuthenticatedClient`, `UnauthenticatedClient`, `WalletAddressClientBase`, `OpenPaymentsExceptionFactory` — all `internal`) keeps working. `PackageId` in both shipping `.csproj` files stays `Interledger.OpenPayments` / `Interledger.OpenPayments.HttpSignatureUtils`. Task 2 and Task 3 build on these names.

There is no failing-test step for a rename; the "test" is: the old prefix is gone from code/build config, the build compiles, and every existing test still passes.

- [ ] **Step 1: Run the scripted rename over all C# sources and the Makefile**

From the repo root:

```bash
perl -pi -e 's/Interledger\.OpenPayments/OpenPayments.Sdk/g' $(git ls-files '*.cs') Makefile
```

This rewrites, in one consistent pass: all `namespace` declarations (including inside `.g.cs`), all `using` directives and fully-qualified type references (e.g. `Interledger.OpenPayments.Generated.Resource.Amount` → `OpenPayments.Sdk.Generated.Resource.Amount`), the `[assembly: InternalsVisibleTo("Interledger.OpenPayments.Tests")]` attribute in `OpenPayments.Sdk/Clients/UnauthenticatedClient.cs` (becomes `"OpenPayments.Sdk.Tests"`), the doc-comment mention in `OpenPayments.Sdk/OpenPaymentsApiException.cs:4`, and the three `/namespace:Interledger.OpenPayments.Generated.*` flags in the `Makefile`.

- [ ] **Step 2: Verify the old prefix is gone from code and the Makefile**

Run: `grep -rn "Interledger\.OpenPayments" --include='*.cs' . ; grep -n "Interledger.OpenPayments" Makefile`
Expected: **no output** from either command (exit code 1).

- [ ] **Step 3: Revert identity settings in `OpenPayments.Sdk.csproj`**

Modify `OpenPayments.Sdk/OpenPayments.Sdk.csproj` — replace:
```xml
    <AssemblyName>Interledger.OpenPayments</AssemblyName>
    <RootNamespace>Interledger.OpenPayments</RootNamespace>
    <PackageTags>interledger openpayments</PackageTags>
    <Product>Interledger.OpenPayments</Product>
```
with:
```xml
    <AssemblyName>OpenPayments.Sdk</AssemblyName>
    <RootNamespace>OpenPayments.Sdk</RootNamespace>
    <PackageTags>interledger openpayments</PackageTags>
    <Product>OpenPayments.Sdk</Product>
```
Leave `<PackageId>Interledger.OpenPayments</PackageId>` (line 3) and `<Description>.NET SDK for OpenPayments</Description>` untouched.

- [ ] **Step 4: Revert identity settings in `OpenPayments.Sdk.HttpSignatureUtils.csproj`**

Modify `OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj` — replace:
```xml
    <RootNamespace>Interledger.OpenPayments.HttpSignatureUtils</RootNamespace>
    <AssemblyName>Interledger.OpenPayments.HttpSignatureUtils</AssemblyName>
    <PackageId>Interledger.OpenPayments.HttpSignatureUtils</PackageId>
    <Title>OpenPayments HTTP Signature Utils</Title>
    <Product>Interledger.OpenPayments.HttpSignatureUtils</Product>
```
with:
```xml
    <RootNamespace>OpenPayments.Sdk.HttpSignatureUtils</RootNamespace>
    <AssemblyName>OpenPayments.Sdk.HttpSignatureUtils</AssemblyName>
    <PackageId>Interledger.OpenPayments.HttpSignatureUtils</PackageId>
    <Title>OpenPayments HTTP Signature Utils</Title>
    <Product>OpenPayments.Sdk.HttpSignatureUtils</Product>
```
(`PackageId` and `Title` are unchanged — shown only for context so the replacement block is unambiguous.) Then replace:
```xml
    <InternalsVisibleTo Include="Interledger.OpenPayments.HttpSignatureUtils.Tests"/>
```
with:
```xml
    <InternalsVisibleTo Include="OpenPayments.Sdk.HttpSignatureUtils.Tests"/>
```
Leave the `<Description>` field (mentions "Interledger.OpenPayments" as the published package name) untouched.

- [ ] **Step 5: Revert identity settings in `OpenPayments.Sdk.Tests.csproj`**

Modify `OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj` — replace:
```xml
    <RootNamespace>Interledger.OpenPayments.Tests</RootNamespace>
    <AssemblyName>Interledger.OpenPayments.Tests</AssemblyName>
    <Authors />
    <Company>Interledger.OpenPayments.Tests</Company>
    <Product>Interledger.OpenPayments.Tests</Product>
```
with:
```xml
    <RootNamespace>OpenPayments.Sdk.Tests</RootNamespace>
    <AssemblyName>OpenPayments.Sdk.Tests</AssemblyName>
    <Authors />
    <Company>OpenPayments.Sdk.Tests</Company>
    <Product>OpenPayments.Sdk.Tests</Product>
```

- [ ] **Step 6: Revert identity settings in `OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj`**

Modify `OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj` — replace:
```xml
    <RootNamespace>Interledger.OpenPayments.HttpSignatureUtils.Tests</RootNamespace>
    <AssemblyName>Interledger.OpenPayments.HttpSignatureUtils.Tests</AssemblyName>
```
with:
```xml
    <RootNamespace>OpenPayments.Sdk.HttpSignatureUtils.Tests</RootNamespace>
    <AssemblyName>OpenPayments.Sdk.HttpSignatureUtils.Tests</AssemblyName>
```

- [ ] **Step 7: Verify `PackageId` was not touched**

Run: `grep -n "PackageId" OpenPayments.Sdk/OpenPayments.Sdk.csproj OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj`
Expected:
```
OpenPayments.Sdk/OpenPayments.Sdk.csproj:    <PackageId>Interledger.OpenPayments</PackageId>
OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj:    <PackageId>Interledger.OpenPayments.HttpSignatureUtils</PackageId>
```

- [ ] **Step 8: Build and run all tests**

Run: `dotnet build --configuration Release`
Expected: succeeds. If any `CS0246` (type/namespace not found) or `CS0122` (inaccessible due to protection level) appears, it means a `using` or an `InternalsVisibleTo` pairing was missed — cross-check against Steps 1–6.

Run: `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj`
Run: `dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj`
Expected: all tests PASS.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "refactor!: revert namespaces Interledger.OpenPayments.* to OpenPayments.Sdk.*"
```

---

### Task 2: Update the README usage snippet

**Files:**
- Modify: `README.md:129-132`

**Interfaces:**
- Consumes: the reverted namespaces from Task 1.
- Produces: no code interface — documentation only. The badge (`README.md:9`), `dotnet add package` command (`README.md:121`), and package table (`README.md:172-173`) are unaffected and keep `Interledger.OpenPayments`.

- [ ] **Step 1: Replace the `using` lines in the usage snippet**

Modify `README.md` — replace:
```csharp
// Import dependencies
using Microsoft.Extensions.DependencyInjection;
using Interledger.OpenPayments.Clients;
using Interledger.OpenPayments.Extensions;
using Interledger.OpenPayments.Generated.Resource;
using Interledger.OpenPayments.HttpSignatureUtils;
```
with:
```csharp
// Import dependencies
using Microsoft.Extensions.DependencyInjection;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Extensions;
using OpenPayments.Sdk.Generated.Resource;
using OpenPayments.Sdk.HttpSignatureUtils;
```

- [ ] **Step 2: Verify only the intended lines changed**

Run: `grep -n "Interledger.OpenPayments\|OpenPayments.Sdk" README.md`
Expected: the `using` lines now show `OpenPayments.Sdk.*`; the badge (line 9), install command (line 121), and package table (lines 172-173) still show `Interledger.OpenPayments`.

- [ ] **Step 3: Commit**

```bash
git add README.md
git commit -m "docs: update README usage snippet for reverted namespace"
```

---

### Task 3: Update CI coverage class filter and assembly-coverage check names

**Files:**
- Modify: `.github/workflows/build.yaml:63,80-81`

**Interfaces:**
- Consumes: the reverted `AssemblyName`s from Task 1 (`OpenPayments.Sdk`, `OpenPayments.Sdk.HttpSignatureUtils`).
- Produces: no code interface — CI configuration only.

- [ ] **Step 1: Update the coverage report class filter**

Modify `.github/workflows/build.yaml` — replace:
```yaml
      - name: Generate coverage report
        run: reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:"Html;TextSummary" -classfilters:"-Interledger.OpenPayments.Generated.*"
```
with:
```yaml
      - name: Generate coverage report
        run: reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:"Html;TextSummary" -classfilters:"-OpenPayments.Sdk.Generated.*"
```

- [ ] **Step 2: Update the per-assembly coverage floor checks**

Modify `.github/workflows/build.yaml` — replace:
```yaml
          check_assembly "Interledger.OpenPayments" 60
          check_assembly "Interledger.OpenPayments.HttpSignatureUtils" 60
```
with:
```yaml
          check_assembly "OpenPayments.Sdk" 60
          check_assembly "OpenPayments.Sdk.HttpSignatureUtils" 60
```

- [ ] **Step 3: Verify no stale references remain in the workflow file**

Run: `grep -n "Interledger.OpenPayments" .github/workflows/build.yaml`
Expected: no output.

Run: `grep -n "Pack Interledger" .github/workflows/release.yaml`
Expected: the two "Pack Interledger.OpenPayments..." step names still present (unchanged — they name the packed `PackageId`, not the assembly).

- [ ] **Step 4: Commit**

```bash
git add .github/workflows/build.yaml
git commit -m "ci: track reverted assembly names in coverage checks"
```

---

### Task 4: Add a CHANGELOG entry for the reversal

**Files:**
- Modify: `CHANGELOG.md` (`## [Unreleased]` → `### Changed` section)

**Interfaces:**
- Consumes: nothing.
- Produces: no code interface — changelog only. The existing bullet documenting the original rename (`CHANGELOG.md:15`) is left untouched, per the design's decision to keep it as historical record.

- [ ] **Step 1: Add the new bullet directly under the existing rename bullet**

Modify `CHANGELOG.md` — replace:
```markdown
### Changed
- **Breaking:** all namespaces renamed from `OpenPayments.Sdk.*` to `Interledger.OpenPayments.*`, matching the package ID.
- **Breaking:** all client errors now throw a single `OpenPaymentsApiException` (status code, error code, raw body) instead of per-namespace `ApiException` types.
```
with:
```markdown
### Changed
- **Breaking:** all namespaces renamed from `OpenPayments.Sdk.*` to `Interledger.OpenPayments.*`, matching the package ID.
- **Breaking:** namespaces reverted from `Interledger.OpenPayments.*` back to `OpenPayments.Sdk.*`. The NuGet `PackageId`s (`Interledger.OpenPayments`, `Interledger.OpenPayments.HttpSignatureUtils`) are unchanged, so the namespace and package identity intentionally no longer match.
- **Breaking:** all client errors now throw a single `OpenPaymentsApiException` (status code, error code, raw body) instead of per-namespace `ApiException` types.
```

- [ ] **Step 2: Verify the new entry reads correctly and the old one is untouched**

Run: `grep -n "Breaking" CHANGELOG.md`
Expected: three "Breaking" bullets in this order — the original rename, the new reversal, and the `OpenPaymentsApiException` unification bullet — plus the `CompleteIncomingPaymentAsync`/`ListOutgoingPaymentsAsync` breaking bullet further down.

- [ ] **Step 3: Commit**

```bash
git add CHANGELOG.md
git commit -m "docs: record namespace-reversal breaking change in CHANGELOG"
```

---

### Task 5: Full-repo verification sweep

**Files:**
- None modified — this task only runs checks across the work from Tasks 1-4.

**Interfaces:**
- Consumes: the completed state of Tasks 1-4.
- Produces: confirmation that the revert is complete and consistent, and that the codegen toolchain still regenerates identical output under the new namespace.

- [ ] **Step 1: Confirm no stale namespace references remain anywhere**

Run:
```bash
grep -rn "Interledger\.OpenPayments" --include='*.cs' .
grep -n "Interledger.OpenPayments" Makefile README.md .github/workflows/build.yaml
```
Expected: no output from any of these. (`README.md`'s badge/install/table and `.github/workflows/release.yaml`'s pack step names still legitimately contain `Interledger.OpenPayments` — this command intentionally excludes `release.yaml`.)

- [ ] **Step 2: Confirm `PackageId`s are still intact**

Run: `grep -n "PackageId" OpenPayments.Sdk/OpenPayments.Sdk.csproj OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj`
Expected: `Interledger.OpenPayments` and `Interledger.OpenPayments.HttpSignatureUtils` respectively (unchanged from before Task 1).

- [ ] **Step 3: Full build and test run**

Run: `dotnet build --configuration Release`
Expected: succeeds, zero warnings/errors.

Run: `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj && dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj`
Expected: all PASS.

- [ ] **Step 4: Confirm codegen regeneration is a no-op under the new namespace**

Run: `git submodule update --init && dotnet tool restore && make models`
Expected: `AuthServerClient.g.cs`, `ResourceServerClient.g.cs`, and `WalletAddressClient.g.cs` regenerate with `namespace OpenPayments.Sdk.Generated.*`.

Run: `git diff --exit-code -- 'OpenPayments.Sdk/Generated/**/*.g.cs'`
Expected: exit code 0 (no diff) — regeneration reproduces exactly what Task 1's scripted rename already produced.

- [ ] **Step 5: Report**

No commit needed (this task modifies nothing). If Step 4 shows a diff, it means a `.g.cs` file was hand-edited out of sync with the Makefile's `/namespace:` flag — go back to Task 1 Step 1 and re-run the scripted rename across the affected `.g.cs` file, then re-run Steps 3-4 of this task.

---

## Verification

After all 5 tasks:

```bash
grep -rn "Interledger\.OpenPayments" --include='*.cs' .              # expected: nothing
grep -n "Interledger.OpenPayments" Makefile                          # expected: nothing
grep -n "PackageId" OpenPayments.Sdk/OpenPayments.Sdk.csproj OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj
                                                                       # expected: Interledger.OpenPayments(.HttpSignatureUtils) — unchanged
dotnet build --configuration Release
dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj
dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj
make models && git diff --exit-code -- 'OpenPayments.Sdk/Generated/**/*.g.cs'   # regeneration is a no-op
```

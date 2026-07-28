# Revert namespace to `OpenPayments.Sdk` — Design

## Context

Commits `1617396` (rename `OpenPayments.Sdk.*` → `Interledger.OpenPayments.*`) and `9da0800` (give the HttpSignatureUtils types the same namespace) executed `docs/IMPROVEMENTS.md` #5: aligning the C# namespace with the NuGet `PackageId` (`Interledger.OpenPayments`). That decision is being reversed — the namespace goes back to `OpenPayments.Sdk`. The `PackageId` is **not** reverting, so this reintroduces the original mismatch (package `Interledger.OpenPayments`, namespace `OpenPayments.Sdk`) as an accepted trade-off in exchange for the shorter, preferred namespace.

The package is still pre-1.0 in practice, so this is treated the same way the original rename was: an accepted breaking change, executed as a mechanical, scripted pass.

## Scope

**Reverts (namespace identity):**
- Every C# `namespace` declaration, `using` directive, and fully-qualified type reference: `Interledger.OpenPayments.*` → `OpenPayments.Sdk.*`.
- `RootNamespace` and `AssemblyName` in all four `.csproj` files.
- `InternalsVisibleTo` attribute values (they name assemblies, not packages).
- `Company`/`Product` in `OpenPayments.Sdk.Tests.csproj`.
- The three NSwag `/namespace:` flags in the `Makefile`.
- The `using` lines in the `README.md` usage snippet.
- The coverage `classfilters` value and `check_assembly` calls in `.github/workflows/build.yaml` (these track `AssemblyName`, not `PackageId`).

**Does not revert (package identity, left as `Interledger.OpenPayments`):**
- `PackageId` in `OpenPayments.Sdk.csproj` and `OpenPayments.Sdk.HttpSignatureUtils.csproj`.
- The NuGet badge, `dotnet add package` command, and the package table in `README.md`.
- The "Pack Interledger.OpenPayments" / "Pack Interledger.OpenPayments.HttpSignatureUtils" step names in `.github/workflows/release.yaml` (they name the package being packed).

**Left untouched (historical record):**
- `docs/IMPROVEMENTS.md` #5, the `docs/superpowers/plans/2026-07-23-phase2-namespaces-and-typegen.md` plan, and any ADRs that describe the original rename. These document a decision made at the time; the reversal is recorded going forward via a new `CHANGELOG.md` entry and git history, not by rewriting past planning docs.
- The existing `CHANGELOG.md` entry for the original namespace rename — left as-is; a new entry is added for this reversal.

## Mechanics

1. **Scripted pass:** `perl -pi -e 's/Interledger\.OpenPayments/OpenPayments.Sdk/g'` over every tracked `*.cs` file and the `Makefile`. This single pass is safe across all matches — verified no `.cs` file mixes a `PackageId`/NuGet-identity string literal in with namespace/using/fully-qualified-reference occurrences of `Interledger.OpenPayments`.
2. **Hand-edit the 4 `.csproj` files** (`OpenPayments.Sdk`, `OpenPayments.Sdk.Tests`, `OpenPayments.Sdk.HttpSignatureUtils`, `OpenPayments.Sdk.HttpSignatureUtils.Tests`): change `RootNamespace`/`AssemblyName`/`InternalsVisibleTo`/`Company`/`Product` as scoped above; leave `PackageId` lines untouched.
3. **Hand-edit `README.md`:** only the 4 `using Interledger.OpenPayments...;` lines in the usage snippet change; the badge, install command, and package table keep `Interledger.OpenPayments`.
4. **Hand-edit `.github/workflows/build.yaml`:** update the `classfilters` value and the two `check_assembly` calls to the new assembly names.
5. **Add a `CHANGELOG.md` entry** under a new "Unreleased"/next-version heading documenting this as a breaking change: namespace reverted to `OpenPayments.Sdk.*`; `PackageId` unaffected.

No other files are expected to need changes — `.github/workflows/release.yaml`, ADRs, and the plan docs are out of scope per above.

## Verification

- `grep -rn "Interledger\.OpenPayments" --include='*.cs' .` → no output.
- `grep -n "Interledger.OpenPayments" Makefile` → no output.
- `dotnet build --configuration Release` succeeds.
- `dotnet test OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj` and `dotnet test OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj` both pass.
- `make models` (regenerate `.g.cs` from the spec submodule) is a no-op against the committed files, now under the `OpenPayments.Sdk.Generated.*` namespace.
- `grep -n "PackageId" OpenPayments.Sdk/OpenPayments.Sdk.csproj OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj` still shows `Interledger.OpenPayments` / `Interledger.OpenPayments.HttpSignatureUtils` — confirms the package identity was not touched.

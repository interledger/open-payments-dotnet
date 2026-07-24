# Replace FluentAssertions 8.x with AwesomeAssertions 9.5.0

**Date:** 2026-07-24
**Status:** Approved, ready for implementation
**Branch:** cozminu/pelican

## Problem

The test suite depends on **FluentAssertions 8.4.0**. As of v8, FluentAssertions
was relicensed by Xceed under the **Xceed Fluent Assertions Community License** —
free for non-commercial use only, paid subscription required for commercial use.
It is not an OSI-approved open-source license. Every test run now prints a
license nag directing users to `sales@xceed.com`.

This is a liability for an Open Payments **payments SDK** intended to be consumed
downstream, including by commercial adopters. "Non-commercial use" in Xceed's
license refers to how the software is used, not whether the consuming project is
itself open source — so a commercial consumer running this test suite would fall
outside the free tier.

## Decision

Replace FluentAssertions with **[AwesomeAssertions](https://www.nuget.org/packages/AwesomeAssertions)
9.5.0**, the actively maintained community fork of FluentAssertions 7.x, published
under **Apache-2.0**.

### Why AwesomeAssertions over pinning to FluentAssertions 7.x

Both options leave the test *source logic* unchanged and cost roughly the same to
apply, so this is a maintenance-posture decision, not a code-migration one. Pinning
to FluentAssertions 7.x freezes the tooling on a dead branch (no fixes, no future
.NET support). AwesomeAssertions forward-ports fixes and new-.NET support at the
same one-line switching cost, so it was chosen.

### Why v9.5.0 (latest) over the v8.x compatibility branch

AwesomeAssertions renamed its namespace in v9 (`FluentAssertions` →
`AwesomeAssertions`). The v8.x branch keeps the old namespace and would require
**zero** `.cs` edits — but it is a superseded compatibility branch that will go
stale, recreating the exact "frozen branch" weakness we are trying to avoid.
Targeting v9.5.0 keeps us on the maintained major line; the cost is a mechanical
`using` find-replace across 11 files. The 84 `.Should()` call sites are unaffected.

## Scope

Three kinds of change, all mechanical. Confirmed no `FluentAssertions.`-qualified
references, `AssertionScope`, `FluentActions`, or `AssertionOptions` usages exist,
so the `using` directives are the entire source surface.

1. **Central package version** — `Directory.Packages.props`:
   - Remove `<PackageVersion Include="FluentAssertions" Version="8.4.0" />`
   - Add `<PackageVersion Include="AwesomeAssertions" Version="9.5.0" />`

2. **Package references** (2 files):
   - `OpenPayments.Sdk.Tests/OpenPayments.Sdk.Tests.csproj`
   - `OpenPayments.Sdk.HttpSignatureUtils.Tests/OpenPayments.Sdk.HttpSignatureUtils.Tests.csproj`
   - `<PackageReference Include="FluentAssertions"/>` → `<PackageReference Include="AwesomeAssertions"/>`

3. **Namespace `using` directive** (11 files):
   - `using FluentAssertions;` → `using AwesomeAssertions;`
   - 10 files in `OpenPayments.Sdk.Tests`, 1 in `OpenPayments.Sdk.HttpSignatureUtils.Tests`
   - The 84 `.Should()` call sites and all fluent chains (`.Which`, `.And`,
     `.BeEquivalentTo`, etc.) stay byte-identical.

4. **Changelog** — add an entry under `[Unreleased] > Changed` in `CHANGELOG.md`
   noting the FluentAssertions → AwesomeAssertions swap and the licensing rationale.

### Out of scope

- Production SDK code (never referenced FluentAssertions).
- Test *logic* — assertions themselves are unchanged.
- Any other dependency.

## Verification

This is the entire safety net; the risk is near-zero because any API drift between
FluentAssertions 8.x and the AwesomeAssertions 9.x (fork-of-7.x) surfaces as a
compile error or test failure here, before commit.

1. `dotnet test --configuration Release` on both target frameworks (net8.0, net9.0):
   - All **122 tests still pass**.
   - The `sales@xceed.com` / Xceed license warning is **gone** from output.
2. `grep -rn "FluentAssertions" --include=*.cs --include=*.csproj --include=*.props`
   (excluding `bin/`/`obj/`) returns **zero** hits — proves a clean cutover with no
   lingering `FluentAssertions` package that would collide with the
   `AwesomeAssertions` namespace.

## Risk

Near-zero. Fully caught by the verification step; nothing ships blind. The only
theoretical failure mode is an assertion API that changed shape between FA 8.x and
the AA 9.x fork line, which the test run would expose immediately.

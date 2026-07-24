# Keeping Up With the Open Payments Specification

This is the maintainer runbook for pulling upstream `open-payments-specifications` changes
into this SDK. It covers how new spec releases get noticed and the step-by-step procedure
for applying one once you decide to update.

## Detection

No CI polling — two passive, push-based triggers. Both lead into the same update procedure
below.

1. **Minor/major releases.** Upstream's `sdk-repo-notifier` workflow files an issue in this
   repo titled `Update to Open Payments Specification vX.Y.Z` whenever a minor or major
   version is tagged in `interledger/open-payments-specifications`. Triage it when it
   arrives.
2. **Patch releases.** The notifier above does *not* fire for patch releases, and patch
   releases have shipped real fixes (e.g. v1.3.1–v1.3.3) that would otherwise go unnoticed.
   A maintainer should set GitHub's **Watch → Custom → Releases** on
   `interledger/open-payments-specifications` to get notified directly. This is a one-time
   setup per maintainer, not something this repo can automate.

## Update procedure

### 1. Identify the target version

Read every release's notes between the currently pinned tag and the target tag — not just
the target release's — so cumulative changes across skipped releases aren't missed. Check
the currently pinned tag with:

```bash
cd open-payments-specifications && git describe --tags && cd ..
```

### 2. Bump the submodule

```bash
cd open-payments-specifications && git fetch --tags && git checkout vX.Y.Z && cd ..
git add open-payments-specifications
```

### 3. Diff the OpenAPI specs for human-relevant changes

Compiler errors won't catch new endpoints or new optional fields — they compile fine
untouched:

```bash
git -C open-payments-specifications diff <old-tag> <new-tag> -- openapi/
```

Look for: new `paths:` (new endpoints), new required fields, removed/renamed fields, new
enum values, changed auth/grant flows.

### 4. Regenerate types and refresh the public API baseline

```bash
make models   # rewrites OpenPayments.Sdk/Generated/**/*.g.cs from the OpenAPI specs
make api      # syncs PublicAPI.Shipped.txt / PublicAPI.Unshipped.txt with the regenerated surface
```

Both `Generated/**/*.g.cs` and `PublicAPI.*.txt` are committed to the repo — always run
these two together and commit both outputs in the same PR (see
`docs/adr/0002-public-api-tracking-of-generated-types.md` for why generated types stay
tracked). Review the resulting diff on `PublicAPI.Unshipped.txt`: it's a readable summary
of exactly what public surface the spec bump changed (new DTOs, new/removed properties,
renamed types) — a faster way to spot spec-driven breakage than reading the OpenAPI diff
alone.

### 5. Build and fix structural breaks

```bash
dotnet build
```

Compile errors point at renamed/removed generated members referenced by the hand-written
`*.Methods.*.cs` partials or `Types.cs` subclasses — fix the hand-written code to match.

### 6. Add hand-written support for new endpoints/features

For anything found in step 3 that didn't surface as a compile error (purely additive
changes — new endpoints, new optional fields), add hand-written support: new wrapper
methods in the relevant `*.Methods*.cs` partial, following the existing pattern for that
client (Auth/Resource use per-feature partials like `AuthServerClient.Methods.Grant.cs`;
Wallet uses a single `WalletAddressClient.Methods.cs`).

If this adds new public methods or types, rerun `make api` to capture them in
`PublicAPI.Unshipped.txt` — CI's `codegen-check.yaml` fails the PR if the baseline is out
of sync with the code, so don't skip this even for hand-written additions.

### 7. Tests

Add/update unit tests in `OpenPayments.Sdk.Tests` for anything changed or added in steps
5–6.

```bash
dotnet test
```

Full suite must pass.

### 8. Version and changelog

Versions are derived from git tags (`vX.Y.Z`) via MinVer — there's no version file to bump;
that happens at tag time (see "Cutting a release" below). Add an entry to `CHANGELOG.md`
under `[Unreleased]`, following its existing Keep a Changelog structure:

- Spec-additive changes (new endpoints, new optional fields) → `### Added`, ships as SDK
  minor.
- Anything that broke step 5's build (renamed/removed fields, changed required-ness) is
  technically a breaking change, but per `docs/IMPROVEMENTS.md` #5 this SDK is still pre-1.0
  in practice, so it still ships as SDK minor. Call it out explicitly under `### Changed`
  with a **Breaking:** prefix (matching the existing `CHANGELOG.md` convention), and repeat
  the call-out in the PR description so reviewers don't miss it.

### 9. PR and close-out

Open a PR referencing the spec release (and the auto-filed issue, if one exists — check off
its todo items). `codegen-check.yaml` independently reruns `make models` + `make api` on the
PR and fails if the committed generated code or baselines drift from what's in the PR — this
is enforcement, not just informational, so a clean CI run on this PR confirms step 4 was done
completely, and that any new public surface added in step 6 was captured in the baseline
(it can't confirm you actually wrote hand-written wrappers for every new endpoint from step
3 — that's a human review judgment). Once merged, tag and release via the existing
`release.yaml` flow (`make ship-api` first — see "Cutting a release" below).

## Out of scope

- Automating submodule bumps or PR creation — this is deliberately a manual runbook.
- Catching the SDK up from the currently pinned spec version to the latest upstream release
  — that's separate follow-up work, done by running this runbook once merged.

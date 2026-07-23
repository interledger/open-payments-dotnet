# Keeping Up With the Open Payments Specification — Design

## Context

`open-payments-specifications` (the OpenAPI submodule at `open-payments-specifications/`) is developed and released independently of this SDK. The submodule is currently pinned to `v1.0.3`; upstream is at `v1.3.3` (3 minor releases and several patches ahead). Upstream already runs an `sdk-repo-notifier` workflow that files a GitHub issue in this repo (`Update to Open Payments Specification vX.Y.Z`) on every minor/major spec release, but not on patch releases, and it only started firing after 2026-06-18 — so it has never yet fired here.

This design defines an ongoing **process** (not tooling/automation) for noticing new spec releases and pulling their changes into the SDK. It targets today's architecture: full-client NSwag generation via `make models`, regenerated (not committed) in CI, `OpenPayments.Sdk.*` namespaces. It is independent of the separate `IMPROVEMENTS.md` phase plan (namespace rename, types-only codegen, committed-generated-code + drift check) — if/when that plan lands, this runbook's mechanics (steps 2–9 below) stay valid; only the namespace names and the "regenerated in CI" framing would need a documentation touch-up at that time, out of scope here.

**Explicit scope boundary:** this design covers the *process* going forward. Catching the SDK up from `v1.0.3` to `v1.3.3` is separate follow-up work, done by running this same runbook once it exists.

## Detection strategy

No CI automation. Two passive triggers, both push-based (nothing to poll):

1. **Minor/major releases:** the existing upstream `sdk-repo-notifier` issue. Triage it when it arrives.
2. **Patch releases:** GitHub's native **Watch → Custom → Releases** notification on `interledger/open-payments-specifications`, set once by a maintainer. This closes the gap the notifier doesn't cover — patch releases have shipped real fixes (e.g. v1.3.1–v1.3.3) that would otherwise go unnoticed.

Both triggers lead into the same update procedure below.

## Update procedure

Documented as a maintainer runbook at **`docs/SPEC_UPDATES.md`**, linked from `.github/contributing.md`.

1. **Identify the target version.** Read every release's notes between the currently pinned tag and the target tag — not just the target's — so cumulative changes across skipped releases aren't missed.

2. **Bump the submodule:**
   ```bash
   cd open-payments-specifications && git fetch --tags && git checkout vX.Y.Z && cd ..
   git add open-payments-specifications
   ```

3. **Diff the OpenAPI specs for human-relevant changes** — the step compiler errors won't catch, since new endpoints or new optional fields compile fine untouched:
   ```bash
   git -C open-payments-specifications diff <old-tag> <new-tag> -- openapi/
   ```
   Look for: new `paths:` (new endpoints), new required fields, removed/renamed fields, new enum values, changed auth/grant flows.

4. **Regenerate:** `make models` (rewrites the three `.g.cs` files).

5. **Build and fix structural breaks:** `dotnet build`. Compile errors point at renamed/removed generated members referenced by the hand-written `*.Methods.*.cs` partials or `Types.cs` subclasses — fix the hand-written code to match.

6. **Add hand-written support for new endpoints/features** found in step 3 that didn't surface as compile errors — new wrapper methods in the relevant `*.Methods.*.cs` partial, following the existing pattern for that client (Auth/Resource/Wallet).

7. **Tests:** add/update unit tests in `OpenPayments.Sdk.Tests` for anything changed or added in steps 5–6. Full suite (`dotnet test`) must pass.

8. **Version and changelog:** bump SDK version per semver. Spec-additive changes → SDK minor. Anything that broke step 5's build is technically a breaking change but still ships as SDK minor pre-1.0 (per `IMPROVEMENTS.md` #5) — call it out explicitly in the PR description and changelog entry regardless.

9. **PR and close-out:** open a PR referencing the spec release (and the auto-filed issue, if one exists — check off its todo items). Merge, then tag/release via the existing `release.yaml` flow.

## Out of scope

- Automating submodule bumps or PR creation (explicitly rejected in favor of a manual runbook).
- Catching the SDK up from v1.0.3 to v1.3.3 (separate follow-up task).
- Reconciling this runbook with the `IMPROVEMENTS.md` Phase 2 architecture (types-only codegen, committed generated code, `codegen-check.yaml`) — a documentation update if/when that phase ships.

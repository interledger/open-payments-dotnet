# Spec Update Strategy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Document a maintainer runbook for noticing and pulling in new `open-payments-specifications` releases, as a new `docs/SPEC_UPDATES.md` linked from `.github/contributing.md`.

**Architecture:** This is a documentation-only change — no code, no CI. One new file (`docs/SPEC_UPDATES.md`) holds the detection strategy and the 9-step update procedure; `.github/contributing.md` gets a short pointer section to it. "Testing" a doc task means verifying every path, command, and file reference it makes is actually correct against the current repo — not running the full runbook end-to-end (that would mean actually catching up the spec version, which is explicitly out of scope).

**Tech Stack:** Markdown only.

## Global Constraints

- Detection is two passive, push-based triggers only — no CI polling/automation (explicitly rejected by the spec in favor of a manual runbook).
- Runbook file location: `docs/SPEC_UPDATES.md`, linked from `.github/contributing.md`.
- Scope boundary: this plan documents the *process* only. Catching the SDK up from the currently pinned spec tag to the latest upstream release is separate follow-up work (out of scope here).
- **Architecture deviation from the spec text (confirmed with user before writing this plan):** the spec's own context section describes "today's architecture" as full-client NSwag generation, models regenerated (not committed) in CI, `OpenPayments.Sdk.*` namespaces — and treats types-only codegen / committed generated code / a drift-check workflow as a future `IMPROVEMENTS.md` phase, out of scope here. That phase has already shipped on this branch: `Makefile` already generates types-only (`/GenerateClientClasses:false`), `OpenPayments.Sdk/Generated/**/*.g.cs` is already committed, namespaces are already `Interledger.OpenPayments.*`, and `.github/workflows/codegen-check.yaml` already enforces no drift between regenerated output and committed files (verified live against the repo, not from memory). Per user decision, the runbook in this plan documents the **current** architecture, not the spec's stated one — this affects steps 4, 6, and 8 of the procedure (adds a `make api` baseline-refresh sub-step, notes the drift check is enforcement not just CI regeneration, and drops "regenerated in CI" framing from the version/changelog step).

---

## File Structure

- Create: `docs/SPEC_UPDATES.md` — the runbook: detection strategy + 9-step update procedure + out-of-scope note.
- Modify: `.github/contributing.md:136-138` — insert a new `## Updating the Open Payments specification` section between the existing "Regenerating models and public API baselines" section (ends line 136) and "Cutting a release" (starts line 138), pointing at the new runbook.

---

### Task 1: Write the `docs/SPEC_UPDATES.md` runbook

**Files:**
- Create: `docs/SPEC_UPDATES.md`

**Interfaces:**
- Consumes: nothing (first task, no code dependencies).
- Produces: `docs/SPEC_UPDATES.md` — Task 2 links to this file and must not restate its content, only point at it.

- [ ] **Step 1: Write the runbook file**

Create `docs/SPEC_UPDATES.md` with this exact content:

````markdown
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
methods in the relevant `*.Methods.*.cs` partial, following the existing pattern for that
client (Auth/Resource/Wallet).

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
is enforcement, not just informational, so a clean CI run on this PR confirms steps 4 and 6
were done completely. Once merged, tag and release via the existing `release.yaml` flow
(`make ship-api` first — see "Cutting a release" below).

## Out of scope

- Automating submodule bumps or PR creation — this is deliberately a manual runbook.
- Catching the SDK up from the currently pinned spec version to the latest upstream release
  — that's separate follow-up work, done by running this runbook once merged.
````

- [ ] **Step 2: Verify every path and command the runbook references actually exists**

```bash
# Makefile targets used in step 4/6
grep -n "^models:\|^api:\|^ship-api:" Makefile

# Files/paths referenced
test -f docs/adr/0002-public-api-tracking-of-generated-types.md && echo "adr-0002 OK"
test -f docs/IMPROVEMENTS.md && echo "IMPROVEMENTS.md OK"
test -f CHANGELOG.md && echo "CHANGELOG.md OK"
test -f .github/workflows/codegen-check.yaml && echo "codegen-check.yaml OK"
grep -n "^### 5" docs/IMPROVEMENTS.md

# Submodule commands are runnable from repo root
test -d open-payments-specifications/.git -o -f open-payments-specifications/.git && echo "submodule present"
```

Expected: every check prints its `OK`/match — no missing file, no missing Makefile target,
and `### 5` in `docs/IMPROVEMENTS.md` is the namespace-alignment/pre-1.0 item the runbook
cites for the "still ships as minor" claim.

- [ ] **Step 3: Commit**

```bash
git add docs/SPEC_UPDATES.md
git commit -m "docs: add spec update strategy runbook"
```

---

### Task 2: Link the runbook from `.github/contributing.md`

**Files:**
- Modify: `.github/contributing.md:136-138`

**Interfaces:**
- Consumes: `docs/SPEC_UPDATES.md` (Task 1) — links to it by relative path `../docs/SPEC_UPDATES.md` from `.github/contributing.md`.
- Produces: nothing further consumes this — end of plan.

- [ ] **Step 1: Insert a new section pointing at the runbook**

In `.github/contributing.md`, between the end of the "Regenerating models and public API
baselines" section and the start of "## Cutting a release", insert:

```markdown
## Updating the Open Payments specification

This repo tracks `open-payments-specifications` as a submodule, pinned to a specific tag.
For how to notice new upstream spec releases and the full step-by-step procedure for
pulling one in (submodule bump, regenerating models, updating hand-written code, versioning),
see [`docs/SPEC_UPDATES.md`](../docs/SPEC_UPDATES.md).

```

The surrounding lines 122–138 should read, in full, after this edit:

```markdown
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

## Updating the Open Payments specification

This repo tracks `open-payments-specifications` as a submodule, pinned to a specific tag.
For how to notice new upstream spec releases and the full step-by-step procedure for
pulling one in (submodule bump, regenerating models, updating hand-written code, versioning),
see [`docs/SPEC_UPDATES.md`](../docs/SPEC_UPDATES.md).

## Cutting a release

Versions are derived from git tags via MinVer (`v*.*.*`). Before pushing a release tag, promote
the accumulated `PublicAPI.Unshipped.txt` entries into `PublicAPI.Shipped.txt` in a normal PR to
`main`:

```bash
make ship-api   # move Unshipped.txt entries into Shipped.txt for both packages
```

Commit the result, merge it, then tag that commit as `vX.Y.Z` and push the tag. `release.yaml`
verifies `PublicAPI.Unshipped.txt` is empty (aside from its `#nullable enable` header) before
building and publishing, and fails the release with instructions to run `make ship-api` if not.
```

- [ ] **Step 2: Verify the link target resolves and the Table of Contents entry isn't silently expected**

```bash
# Relative link resolves from .github/ to docs/SPEC_UPDATES.md
test -f .github/../docs/SPEC_UPDATES.md && echo "link target OK"

# Confirm the new heading was inserted exactly once, in the right place
grep -n "^## " .github/contributing.md
```

Expected: `link target OK` prints, and the heading list shows `## Updating the Open Payments
specification` appearing once, after `## Regenerating models and public API baselines` and
before `## Cutting a release`. Note: `.github/contributing.md`'s Table of Contents (lines
10–26) is manually written and not regenerated by any tool in this repo (no doctoc/markdownlint
config found) — leave it as-is; adding a ToC entry for the new section is optional polish, not
required for this task to be complete.

- [ ] **Step 3: Commit**

```bash
git add .github/contributing.md
git commit -m "docs: link the spec update runbook from contributing.md"
```

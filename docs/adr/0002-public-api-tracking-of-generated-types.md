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

#!/usr/bin/env bash
# Promotes PublicAPI.Unshipped.txt entries into PublicAPI.Shipped.txt for each
# project directory given as an argument, then clears Unshipped.txt back to
# just its header. Run this as part of the release-prep PR, before tagging -
# see .github/contributing.md for the full release flow.
set -euo pipefail

if [ "$#" -eq 0 ]; then
  echo "usage: $0 <project-dir> [project-dir ...]" >&2
  exit 1
fi

for dir in "$@"; do
  shipped="$dir/PublicAPI.Shipped.txt"
  unshipped="$dir/PublicAPI.Unshipped.txt"

  if [ ! -f "$shipped" ] || [ ! -f "$unshipped" ]; then
    echo "skip $dir: missing PublicAPI.Shipped.txt or PublicAPI.Unshipped.txt" >&2
    continue
  fi

  work=$(mktemp -d)
  header=$(head -n1 "$shipped")

  tail -n +2 "$shipped" > "$work/shipped-entries"
  tail -n +2 "$unshipped" | grep -v '^[[:space:]]*$' > "$work/unshipped-entries" || true

  grep '^\*REMOVED\*' "$work/unshipped-entries" | sed 's/^\*REMOVED\*//' > "$work/removals" || true
  grep -v '^\*REMOVED\*' "$work/unshipped-entries" > "$work/additions" || true

  LC_ALL=C sort -u "$work/shipped-entries" "$work/additions" \
    | LC_ALL=C comm -23 - <(LC_ALL=C sort -u "$work/removals") \
    > "$work/next-shipped"

  {
    echo "$header"
    cat "$work/next-shipped"
  } > "$shipped"

  echo "$header" > "$unshipped"

  added=$(wc -l < "$work/additions" | tr -d ' ')
  removed=$(wc -l < "$work/removals" | tr -d ' ')
  echo "$dir: promoted $added addition(s), $removed removal(s) into PublicAPI.Shipped.txt"

  rm -rf "$work"
done

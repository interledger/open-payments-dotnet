#!/usr/bin/env bash
# Folds each PublicAPI.Unshipped.txt into its sibling PublicAPI.Shipped.txt.
# Run from the repository root at release time, before tagging, and commit the
# result. See docs/adr/0001-track-the-public-api-surface.md.
set -e

header='#nullable enable'
promoted=0

while IFS= read -r unshipped; do
  shipped="$(dirname "$unshipped")/PublicAPI.Shipped.txt"

  if [ ! -f "$shipped" ]; then
    echo "::error::Found $unshipped with no sibling PublicAPI.Shipped.txt. The baseline pair is incomplete; a promote would silently discard the unshipped entries."
    exit 1
  fi

  # Entries only: drop the header and any blank lines.
  entries="$(grep -v -e "^$header\$" -e '^[[:space:]]*$' "$unshipped" || true)"

  if [ -z "$entries" ]; then
    continue
  fi

  removals="$(printf '%s\n' "$entries" | grep '^\*REMOVED\*' || true)"
  additions="$(printf '%s\n' "$entries" | grep -v '^\*REMOVED\*' || true)"

  # Current shipped entries, header stripped.
  body="$(grep -v -e "^$header\$" -e '^[[:space:]]*$' "$shipped" || true)"

  # Apply removals first, so a rename that removes and re-adds the same
  # symbol name ends up with the addition rather than nothing.
  if [ -n "$removals" ]; then
    while IFS= read -r removal; do
      [ -z "$removal" ] && continue
      symbol="${removal#\*REMOVED\*}"
      if ! printf '%s\n' "$body" | grep -qxF "$symbol"; then
        echo "::error::'$unshipped' removes '$symbol', but that entry is not present in '$shipped'. The baseline has drifted; promoting would corrupt it. Reconcile the two files by hand before releasing."
        exit 1
      fi
      body="$(printf '%s\n' "$body" | grep -vxF "$symbol" || true)"
    done <<< "$removals"
  fi

  if [ -n "$additions" ]; then
    body="$(printf '%s\n%s\n' "$body" "$additions")"
  fi

  # Rewrite the pair: header pinned first, entries sorted and de-duplicated.
  {
    printf '%s\n' "$header"
    printf '%s\n' "$body" | grep -v '^[[:space:]]*$' | LC_ALL=C sort -u || true
  } > "$shipped.tmp"
  mv "$shipped.tmp" "$shipped"

  printf '%s\n' "$header" > "$unshipped"

  echo "Promoted $(printf '%s\n' "$entries" | wc -l | tr -d ' ') entr(y|ies) into $shipped"
  promoted=$((promoted + 1))
done < <(find . -name 'PublicAPI.Unshipped.txt' -not -path './bin/*' -not -path './obj/*' | LC_ALL=C sort)

if [ "$promoted" -eq 0 ]; then
  echo "No unshipped public API entries to promote."
fi

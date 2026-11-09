#!/usr/bin/env bash
# Fixture test for promote-public-api.sh. Runs the script against throwaway
# baseline pairs in a temp directory and asserts the five behaviours the
# release process depends on.
set -e

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
promote="$script_dir/promote-public-api.sh"
failures=0

fail() {
  echo "::error::promote-public-api.sh: $1"
  failures=$((failures + 1))
}

# Builds a fixture repo in $work containing one project dir with the given
# Shipped/Unshipped contents, then runs the promote script from its root.
setup() {
  work="$(mktemp -d)"
  mkdir -p "$work/Proj"
  printf '%s\n' "$1" > "$work/Proj/PublicAPI.Shipped.txt"
  printf '%s\n' "$2" > "$work/Proj/PublicAPI.Unshipped.txt"
}

teardown() {
  rm -rf "$work"
}

# --- 1. An addition in Unshipped lands in Shipped ---------------------------
setup '#nullable enable
Lib.Existing' '#nullable enable
Lib.Added'
( cd "$work" && "$promote" >/dev/null )
grep -qx 'Lib.Added' "$work/Proj/PublicAPI.Shipped.txt" \
  || fail "case 1: added symbol did not land in Shipped.txt"
grep -qx 'Lib.Existing' "$work/Proj/PublicAPI.Shipped.txt" \
  || fail "case 1: pre-existing symbol was lost from Shipped.txt"
teardown

# --- 2. A *REMOVED* entry deletes its target and leaves no residue ----------
setup '#nullable enable
Lib.Existing
Lib.Doomed' '#nullable enable
*REMOVED*Lib.Doomed'
( cd "$work" && "$promote" >/dev/null )
if grep -qx 'Lib.Doomed' "$work/Proj/PublicAPI.Shipped.txt"; then
  fail "case 2: removed symbol is still present in Shipped.txt"
fi
if grep -q '\*REMOVED\*' "$work/Proj/PublicAPI.Shipped.txt"; then
  fail "case 2: *REMOVED* residue was appended to Shipped.txt"
fi
grep -qx 'Lib.Existing' "$work/Proj/PublicAPI.Shipped.txt" \
  || fail "case 2: unrelated symbol was lost from Shipped.txt"
teardown

# --- 3. A second run is a byte-identical no-op ------------------------------
setup '#nullable enable
Lib.Existing' '#nullable enable
Lib.Added'
( cd "$work" && "$promote" >/dev/null )
first="$(cat "$work/Proj/PublicAPI.Shipped.txt")"
( cd "$work" && "$promote" >/dev/null )
second="$(cat "$work/Proj/PublicAPI.Shipped.txt")"
[ "$first" = "$second" ] || fail "case 3: re-running the script was not a no-op"
teardown

# --- 4. An unresolvable *REMOVED* entry exits non-zero ----------------------
setup '#nullable enable
Lib.Existing' '#nullable enable
*REMOVED*Lib.NeverExisted'
if ( cd "$work" && "$promote" >/dev/null 2>&1 ); then
  fail "case 4: unresolvable *REMOVED* entry did not fail the script"
fi
teardown

# --- 5. The #nullable enable header survives exactly once per file ----------
setup '#nullable enable
Lib.Existing' '#nullable enable
Lib.Added'
( cd "$work" && "$promote" >/dev/null )
for f in Shipped Unshipped; do
  count=$(grep -cx '#nullable enable' "$work/Proj/PublicAPI.$f.txt")
  [ "$count" -eq 1 ] \
    || fail "case 5: PublicAPI.$f.txt has $count '#nullable enable' headers, expected 1"
done
head -n1 "$work/Proj/PublicAPI.Shipped.txt" | grep -qx '#nullable enable' \
  || fail "case 5: '#nullable enable' is not the first line of Shipped.txt"
# Unshipped must be reset to the bare header
[ "$(wc -l < "$work/Proj/PublicAPI.Unshipped.txt")" -eq 1 ] \
  || fail "case 5: Unshipped.txt was not truncated to its header"
teardown

if [ "$failures" -ne 0 ]; then
  echo "::error::promote-public-api.sh failed $failures fixture check(s)."
  exit 1
fi

echo "promote-public-api.sh: all 5 fixture checks passed."

#!/usr/bin/env bash
set -e

scanned_count=$(find OpenPayments.Sdk/ -name '*.cs' ! -name '*.g.cs' 2>/dev/null | wc -l)
if [ "$scanned_count" -eq 0 ]; then
  echo "::error::No non-generated .cs files found under OpenPayments.Sdk/ to scan for BaseUrl writes. Did the directory move or get renamed? Update this guard's path so it can't silently pass."
  exit 1
fi

matches=$(grep -rnE "BaseUrl[[:space:]]*=" --include='*.cs' --exclude='*.g.cs' OpenPayments.Sdk/ || true)
if [ -n "$matches" ]; then
  echo "$matches"
  echo "::error::Writes to BaseUrl outside generated code reintroduce the singleton race from issue #16. Pass the target URL per call instead."
  exit 1
fi

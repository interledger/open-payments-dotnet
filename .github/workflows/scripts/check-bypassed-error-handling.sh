#!/usr/bin/env bash
set -e

for dir in OpenPayments.Sdk/Clients/ OpenPayments.Sdk/Http/; do
  scanned_count=$(find "$dir" -name '*.cs' 2>/dev/null | wc -l)
  if [ "$scanned_count" -eq 0 ]; then
    echo "::error::No .cs files found under $dir to scan. Did the directory move or get renamed? Update this guard's path so it can't silently pass."
    exit 1
  fi
done

ensure=$(grep -rn "EnsureSuccessStatusCode" --include='*.cs' OpenPayments.Sdk/Clients/ OpenPayments.Sdk/Http/ || true)
if [ -n "$ensure" ]; then
  echo "$ensure"
  echo "::error::EnsureSuccessStatusCode throws HttpRequestException, bypassing OpenPaymentsApiException (issue #18). Use OpenPaymentsResponse.ThrowIfErrorAsync instead."
  exit 1
fi

legacy=$(grep -rnw "ApiException" OpenPayments.Sdk/Http/ OpenPayments.Sdk/Clients/ || true)
if [ -n "$legacy" ]; then
  echo "$legacy"
  echo "::error::The generated ApiException types must not escape client methods (issue #18). Throw OpenPaymentsApiException via OpenPaymentsResponse instead."
  exit 1
fi

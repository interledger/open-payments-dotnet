#!/usr/bin/env bash
set -e

scanned_count=$(find OpenPayments.Sdk/Clients/ -name '*.cs' 2>/dev/null | wc -l)
if [ "$scanned_count" -eq 0 ]; then
  echo "::error::No .cs files found under OpenPayments.Sdk/Clients/ to scan. Did the directory move or get renamed? Update this guard's path so it can't silently pass."
  exit 1
fi

ensure=$(grep -rn "EnsureSuccessStatusCode" --include='*.cs' OpenPayments.Sdk/Clients/ || true)
if [ -n "$ensure" ]; then
  echo "$ensure"
  echo "::error::EnsureSuccessStatusCode throws HttpRequestException, bypassing OpenPaymentsApiException (issue #18). Use OpenPaymentsResponse.ThrowIfErrorAsync instead."
  exit 1
fi

method_count=$(ls OpenPayments.Sdk/Generated/*/*.Methods*.cs 2>/dev/null | wc -l)
if [ "$method_count" -eq 0 ]; then
  echo "::error::No *.Methods*.cs files found under OpenPayments.Sdk/Generated/. Did they move or get renamed? Update this guard's path so it can't silently pass."
  exit 1
fi

legacy=$(grep -rnw "ApiException" OpenPayments.Sdk/Generated/*/*.Methods*.cs OpenPayments.Sdk/Clients/ || true)
if [ -n "$legacy" ]; then
  echo "$legacy"
  echo "::error::The generated ApiException types must not escape client methods (issue #18). Throw OpenPaymentsApiException via OpenPaymentsResponse instead."
  exit 1
fi

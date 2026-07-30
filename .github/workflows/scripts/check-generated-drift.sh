#!/usr/bin/env bash
set -e

status=$(git status --porcelain -- OpenPayments.Sdk/Generated)
if [ -n "$status" ]; then
  echo "$status"
  git --no-pager diff -- OpenPayments.Sdk/Generated
  echo "::error::Committed output under OpenPayments.Sdk/Generated/ differs from regeneration with the pinned toolchain (NSwag 14.6.2). Run 'make models' locally and commit the result."
  exit 1
fi

stray=$(find OpenPayments.Sdk/Generated -name '*.cs' ! -name '*.g.cs')
if [ -n "$stray" ]; then
  echo "$stray"
  echo "::error::Found hand-written .cs file(s) under OpenPayments.Sdk/Generated/ without a .g.cs suffix. Only regenerable *.g.cs output belongs there; move hand-owned files to OpenPayments.Sdk/Models/ or OpenPayments.Sdk/Http/."
  exit 1
fi

endpoints=$(grep -rlnE "public[^;{]*Task<" --include='*.g.cs' OpenPayments.Sdk/Generated/ || true)
if [ -n "$endpoints" ]; then
  echo "$endpoints"
  echo "::error::Found an endpoint method declared inside OpenPayments.Sdk/Generated/*.g.cs. Generated output must stay DTO-only; endpoint methods belong in OpenPayments.Sdk/Http/."
  exit 1
fi

baked_url=$(grep -rln "interledger-test.dev" OpenPayments.Sdk/Generated/ || true)
if [ -n "$baked_url" ]; then
  echo "$baked_url"
  echo "::error::Found a reference to interledger-test.dev inside OpenPayments.Sdk/Generated/. No hard-coded server URL is allowed in generated output."
  exit 1
fi

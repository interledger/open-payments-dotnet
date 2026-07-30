#!/usr/bin/env bash
set -e

status=$(git status --porcelain -- OpenPayments.Sdk/Generated)
if [ -n "$status" ]; then
  echo "$status"
  git --no-pager diff -- OpenPayments.Sdk/Generated
  echo "::error::Committed output under OpenPayments.Sdk/Generated/ differs from regeneration with the pinned toolchain (NSwag 14.6.2). Run 'make models' locally and commit the result."
  exit 1
fi

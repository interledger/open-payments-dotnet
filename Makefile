.PHONY: as-models resource-server-generate rs-models wallet-address-models wa-models models

# Generated output is committed. CI regenerates with the pinned toolchain and fails on drift.
# Regeneration needs only: dotnet tool install --global NSwag.ConsoleCore --version 14.6.2
# (no Node/swagger-cli — NSwag reads the OpenAPI YAML directly; every $ref is file-internal).
GENERATE_FLAGS := /GenerateClientClasses:false /GenerateExceptionClasses:false /GenerateOptionalPropertiesAsNullable:true /GenerateNullableReferenceTypes:true

as-models:
	nswag openapi2csclient /input:open-payments-specifications/openapi/auth-server.yaml /output:OpenPayments.Sdk/Generated/Auth/AuthModels.g.cs /namespace:OpenPayments.Sdk.Generated.Auth /classname:AuthClient $(GENERATE_FLAGS)

resource-server-generate:
	npx swagger-cli bundle open-payments-specifications/openapi/resource-server.yaml -o OpenPayments.Sdk/tmp/resource-bundled.json -t json && \
	nswag openapi2csclient /input:OpenPayments.Sdk/tmp/resource-bundled.json /output:OpenPayments.Sdk/Generated/Resource/ResourceServerClient.g.cs /namespace:OpenPayments.Sdk.Generated.Resource /classname:ResourceServerClient /injectHttpClient:true /GenerateOptionalPropertiesAsNullable:true /GenerateNullableReferenceTypes:true && \
	rm -rf OpenPayments.Sdk/tmp/resource-bundled.json

rs-models: resource-server-generate

wallet-address-models:
	npx swagger-cli bundle open-payments-specifications/openapi/wallet-address-server.yaml -o OpenPayments.Sdk/tmp/wallet-bundled.json -t json && \
	nswag openapi2csclient /input:OpenPayments.Sdk/tmp/wallet-bundled.json /output:OpenPayments.Sdk/Generated/Wallet/WalletAddressClient.g.cs /namespace:OpenPayments.Sdk.Generated.Wallet /classname:WalletAddressClient /injectHttpClient:true && \
	rm -rf OpenPayments.Sdk/tmp/wallet-bundled.json

wa-models: wallet-address-models

models: as-models rs-models wa-models

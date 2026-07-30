.PHONY: as-models rs-models wa-models models

# Generated output is committed. CI regenerates with the pinned toolchain and fails on drift.
# Regeneration needs only: dotnet tool install --global NSwag.ConsoleCore --version 14.6.2
# (no Node/swagger-cli — NSwag reads the OpenAPI YAML directly; every $ref is file-internal).
GENERATE_FLAGS := /GenerateClientClasses:false /GenerateExceptionClasses:false /GenerateOptionalPropertiesAsNullable:true /GenerateNullableReferenceTypes:true

as-models:
	nswag openapi2csclient /input:open-payments-specifications/openapi/auth-server.yaml /output:OpenPayments.Sdk/Generated/Auth/AuthModels.g.cs /namespace:OpenPayments.Sdk.Generated.Auth /classname:AuthClient $(GENERATE_FLAGS)

rs-models:
	nswag openapi2csclient /input:open-payments-specifications/openapi/resource-server.yaml /output:OpenPayments.Sdk/Generated/Resource/ResourceModels.g.cs /namespace:OpenPayments.Sdk.Generated.Resource /classname:ResourceClient $(GENERATE_FLAGS)

wa-models:
	nswag openapi2csclient /input:open-payments-specifications/openapi/wallet-address-server.yaml /output:OpenPayments.Sdk/Generated/Wallet/WalletModels.g.cs /namespace:OpenPayments.Sdk.Generated.Wallet /classname:WalletClient $(GENERATE_FLAGS)

models: as-models rs-models wa-models

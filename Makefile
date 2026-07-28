.PHONY: tools auth-server-generate as-models resource-server-generate rs-models wallet-address-models wa-models models api ship-api

NSWAG_FLAGS = /injectHttpClient:true /GenerateClientClasses:false /GenerateExceptionClasses:false /GenerateOptionalPropertiesAsNullable:true /GenerateNullableReferenceTypes:true

tools:
	dotnet tool restore

auth-server-generate: tools
	npx swagger-cli bundle open-payments-specifications/openapi/auth-server.yaml -o OpenPayments.Sdk/tmp/auth-bundled.json -t json && \
	dotnet nswag openapi2csclient /input:OpenPayments.Sdk/tmp/auth-bundled.json /output:OpenPayments.Sdk/Generated/Auth/AuthServerClient.g.cs /namespace:OpenPayments.Sdk.Generated.Auth /classname:AuthServerClient $(NSWAG_FLAGS) && \
	rm -rf OpenPayments.Sdk/tmp/auth-bundled.json

as-models: auth-server-generate

resource-server-generate: tools
	npx swagger-cli bundle open-payments-specifications/openapi/resource-server.yaml -o OpenPayments.Sdk/tmp/resource-bundled.json -t json && \
	dotnet nswag openapi2csclient /input:OpenPayments.Sdk/tmp/resource-bundled.json /output:OpenPayments.Sdk/Generated/Resource/ResourceServerClient.g.cs /namespace:OpenPayments.Sdk.Generated.Resource /classname:ResourceServerClient $(NSWAG_FLAGS) && \
	rm -rf OpenPayments.Sdk/tmp/resource-bundled.json

rs-models: resource-server-generate

wallet-address-models: tools
	npx swagger-cli bundle open-payments-specifications/openapi/wallet-address-server.yaml -o OpenPayments.Sdk/tmp/wallet-bundled.json -t json && \
	dotnet nswag openapi2csclient /input:OpenPayments.Sdk/tmp/wallet-bundled.json /output:OpenPayments.Sdk/Generated/Wallet/WalletAddressClient.g.cs /namespace:OpenPayments.Sdk.Generated.Wallet /classname:WalletAddressClient $(NSWAG_FLAGS) && \
	rm -rf OpenPayments.Sdk/tmp/wallet-bundled.json

wa-models: wallet-address-models

models: as-models rs-models wa-models

api:
	dotnet format analyzers OpenPayments.Sdk/OpenPayments.Sdk.csproj --diagnostics RS0016 RS0017 --severity warn --include-generated
	dotnet format analyzers OpenPayments.Sdk.HttpSignatureUtils/OpenPayments.Sdk.HttpSignatureUtils.csproj --diagnostics RS0016 RS0017 --severity warn --include-generated

# Promotes PublicAPI.Unshipped.txt into PublicAPI.Shipped.txt. Run this as part
# of the release-prep PR, before tagging a release - see .github/contributing.md.
ship-api:
	./scripts/promote-public-api.sh OpenPayments.Sdk OpenPayments.Sdk.HttpSignatureUtils

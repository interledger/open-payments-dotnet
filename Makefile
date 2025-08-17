.PHONY: auth-server-generate as-models resource-server-generate rs-models wallet-address-models wa-models models

auth-server-generate:
	npx swagger-cli bundle open-payments-specifications/openapi/auth-server.yaml -o OpenPayments.Sdk/tmp/auth-bundled.json -t json && \
	nswag openapi2csclient /input:OpenPayments.Sdk/tmp/auth-bundled.json /output:OpenPayments.Sdk/Generated/AuthServerClient.cs /namespace:OpenPayments.Sdk.Generated.Auth /classname:AuthServerClient /injectHttpClient:true && \
	rm -rf OpenPayments.Sdk/tmp/auth-bundled.json

as-models: auth-server-generate

resource-server-generate:
	npx swagger-cli bundle open-payments-specifications/openapi/resource-server.yaml -o OpenPayments.Sdk/tmp/resource-bundled.json -t json && \
	nswag openapi2csclient /input:OpenPayments.Sdk/tmp/resource-bundled.json /output:OpenPayments.Sdk/Generated/ResourceServerClient.cs /namespace:OpenPayments.Sdk.Generated.Resource /classname:ResourceServerClient /injectHttpClient:true && \
	rm -rf OpenPayments.Sdk/tmp/resource-bundled.json

rs-models: resource-server-generate

wallet-address-models:
	npx swagger-cli bundle open-payments-specifications/openapi/wallet-address-server.yaml -o OpenPayments.Sdk/tmp/wallet-bundled.json -t json && \
	nswag openapi2csclient /input:OpenPayments.Sdk/tmp/wallet-bundled.json /output:OpenPayments.Sdk/Generated/WalletAddressClient.cs /namespace:OpenPayments.Sdk.Generated.Wallet /classname:WalletAddressClient /injectHttpClient:true && \
	rm -rf OpenPayments.Sdk/tmp/wallet-bundled.json

wa-models: wallet-address-models

models: as-models rs-models wa-models
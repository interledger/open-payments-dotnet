.PHONY: auth-server-generate as-models resource-server-generate rs-models wallet-address-models wa-models models

auth-server-generate:
	npx swagger-cli bundle open-payments-specifications/openapi/auth-server.yaml -o Interledger.OpenPayments/tmp/auth-bundled.json -t json && \
	nswag openapi2csclient /input:Interledger.OpenPayments/tmp/auth-bundled.json /output:Interledger.OpenPayments/Generated/Auth/AuthServerClient.g.cs /namespace:Interledger.OpenPayments.Generated.Auth /classname:AuthServerClient /injectHttpClient:true && \
	rm -rf Interledger.OpenPayments/tmp/auth-bundled.json

as-models: auth-server-generate

resource-server-generate:
	npx swagger-cli bundle open-payments-specifications/openapi/resource-server.yaml -o Interledger.OpenPayments/tmp/resource-bundled.json -t json && \
	nswag openapi2csclient /input:Interledger.OpenPayments/tmp/resource-bundled.json /output:Interledger.OpenPayments/Generated/Resource/ResourceServerClient.g.cs /namespace:Interledger.OpenPayments.Generated.Resource /classname:ResourceServerClient /injectHttpClient:true /GenerateOptionalPropertiesAsNullable:true /GenerateNullableReferenceTypes:true && \
	rm -rf Interledger.OpenPayments/tmp/resource-bundled.json

rs-models: resource-server-generate

wallet-address-models:
	npx swagger-cli bundle open-payments-specifications/openapi/wallet-address-server.yaml -o Interledger.OpenPayments/tmp/wallet-bundled.json -t json && \
	nswag openapi2csclient /input:Interledger.OpenPayments/tmp/wallet-bundled.json /output:Interledger.OpenPayments/Generated/Wallet/WalletAddressClient.g.cs /namespace:Interledger.OpenPayments.Generated.Wallet /classname:WalletAddressClient /injectHttpClient:true && \
	rm -rf Interledger.OpenPayments/tmp/wallet-bundled.json

wa-models: wallet-address-models

models: as-models rs-models wa-models

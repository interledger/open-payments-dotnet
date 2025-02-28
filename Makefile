.PHONY: auth-server-generate as-models resource-server-generate rs-models wallet-address-models wa-models models

auth-server-generate:
	swagger-cli bundle OpenPaymentsSDK/OpenAPI/auth-server.yaml --outfile OpenPaymentsSDK/OpenAPI/auth-server-bundled.yaml --type yaml && \
	nswag openapi2csclient /input:OpenPaymentsSDK/OpenAPI/auth-server-bundled.yaml /output:OpenPaymentsSDK/Generated/OpenPaymentsAuthServer.cs /namespace:OpenPaymentsSDK.Generated && \
	rm -rf OpenPaymentsSDK/OpenAPI/auth-server-bundled.yaml

as-models: auth-server-generate

resource-server-generate:
	swagger-cli bundle OpenPaymentsSDK/OpenAPI/resource-server.yaml --outfile OpenPaymentsSDK/OpenAPI/resource-server-bundled.yaml --type yaml && \
	nswag openapi2csclient /input:OpenPaymentsSDK/OpenAPI/resource-server-bundled.yaml /output:OpenPaymentsSDK/Generated/OpenPaymentsResourceServer.cs /namespace:OpenPaymentsSDK.Generated && \
	rm -rf OpenPaymentsSDK/OpenAPI/resource-server-bundled.yaml

rs-models: resource-server-generate

wallet-address-models:
	swagger-cli bundle OpenPaymentsSDK/OpenAPI/wallet-address-server.yaml --outfile OpenPaymentsSDK/OpenAPI/wallet-address-server-bundled.yaml --type yaml && \
	nswag openapi2csclient /input:OpenPaymentsSDK/OpenAPI/wallet-address-server-bundled.yaml /output:OpenPaymentsSDK/Generated/OpenPaymentsWalletAddressServer.cs /namespace:OpenPaymentsSDK.Generated && \
	rm -rf OpenPaymentsSDK/OpenAPI/wallet-address-server-bundled.yaml

wa-models: wallet-address-models

models: as-models rs-models wa-models
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenPayments.Sdk;
using OpenPayments.Sdk.Generated.Resource;
using OpenPayments.Sdk.Generated.Wallet;
using Xunit;

namespace OpenPayments.Sdk.Tests;

public class UnauthenticatedClientTests
{
    private static HttpClient CreateStubClient(string expectedPath, object responseObject, out CaptureHandler handler)
    {
        handler = new CaptureHandler(expectedPath, responseObject);
        return new HttpClient(handler, disposeHandler: true);
    }

    [Fact]
    public async Task GetWalletAddressAsync_WithAbsoluteUrl_ReturnsModel()
    {
        // Arrange
        var walletJson = new WalletAddress
        {
            Id = new Uri("https://example.com/alice"),
            PublicName = "Alice",
            AssetCode = "USD",
            AssetScale = 2,
            AuthServer = new Uri("https://auth.example.com"),
            ResourceServer = new Uri("https://example.com")
        };

        var http = CreateStubClient("https://example.com/alice/", walletJson, out var handler);
        var client = new UnauthenticatedClient(http);

        // Act
        var result = await client.GetWalletAddressAsync("https://example.com/alice");

        // Assert
        Assert.Equal("USD", result.AssetCode);
        Assert.Equal(2, result.AssetScale);
        Assert.Equal("Alice", result.PublicName);
        Assert.Equal("https://example.com/alice/", handler.LastRequest?.RequestUri.ToString());
    }

    [Fact]
    public async Task GetWalletAddressAsync_RealLifePaymentPointer_ReturnsModel() {
        // Arrange
        var walletJson = new WalletAddress {
            Id = new Uri("https://ilp.dev/007"),
            PublicName = "Tadej Golobic",
            AssetCode = "EUR",
            AssetScale = 2,
            AuthServer = new Uri("https://auth.interledger.cards"),
            ResourceServer = new Uri("https://ilp.interledger.cards")
        };

        var unauth = new UnauthenticatedClient(new HttpClient());

        // Act
        var result = await unauth.GetWalletAddressAsync("$ilp.dev/007");

        // Assert
        Assert.Equal("EUR", result.AssetCode);
        Assert.Equal(2, result.AssetScale);
        Assert.Equal("Tadej Golobic", result.PublicName);
    }

    [Fact]
    public async Task GetWalletAddressAsync_WithPaymentPointer_TransformsAndReturnsModel()
    {
        // Arrange
        var walletJson = new WalletAddress
        {
            Id = new Uri("https://wallet.test/bob"),
            PublicName = "Bob",
            AssetCode = "EUR",
            AssetScale = 2,
            AuthServer = new Uri("https://auth.test"),
            ResourceServer = new Uri("https://wallet.test")
        };

        var http = CreateStubClient("https://wallet.test/bob/", walletJson, out var handler);
        var unauth = new UnauthenticatedClient(http);

        // Act
        var res = await unauth.GetWalletAddressAsync("$wallet.test/bob");

        // Assert
        Assert.Equal("Bob", res.PublicName);
        Assert.Equal("https://wallet.test/bob/", handler.LastRequest?.RequestUri.ToString());
    }

    [Fact]
    public async Task GetIncomingPaymentAsync_ReturnsModel()
    {
        // Arrange
        var paymentJson = new IncomingPayment
        {
            Id = new Uri("https://example.com/incoming-payments/123"),
            WalletAddress = new Uri("https://example.com/alice"),
            Completed = false,
            IncomingAmount = new IncomingAmount { Value = "100", AssetCode = "USD", AssetScale = 2 },
            ReceivedAmount = new IncomingAmount { Value = "0", AssetCode = "USD", AssetScale = 2 },
            CreatedAt = DateTime.UtcNow
        };

        var http = CreateStubClient("https://example.com/incoming-payments/123", paymentJson, out var handler);
        var unauth = new UnauthenticatedClient(http);

        // Act
        var result = await unauth.GetIncomingPaymentAsync("https://example.com/incoming-payments/123");

        // Assert
        Assert.Equal("USD", result.IncomingAmount?.AssetCode);
        Assert.False(result.Completed);
        Assert.Equal("https://example.com/incoming-payments/123", handler.LastRequest?.RequestUri.ToString());
    }

    // ---------------------------------------------------------------------
    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly string _expectedUri;
        private readonly string _json;

        public HttpRequestMessage? LastRequest { get; private set; }

        public CaptureHandler(string expectedUri, object payload)
        {
            _expectedUri = expectedUri;
            _json = JsonConvert.SerializeObject(payload);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(_expectedUri, request.RequestUri!.ToString());

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
} 
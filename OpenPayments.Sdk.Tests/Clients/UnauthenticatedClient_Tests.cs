using System.Net;
using AwesomeAssertions;
using Interledger.OpenPayments.Clients;

namespace Interledger.OpenPayments.Tests.Clients;

public class UnauthenticatedClient_Tests
{
    [Collection("UnauthenticatedClient")]
    public class UnauthenticatedClient_WalletAddress_Tests
    {
        private readonly UnauthenticatedClient _client;
        private readonly UnauthenticatedClientFixture _fixture;

        public UnauthenticatedClient_WalletAddress_Tests(UnauthenticatedClientFixture fixture)
        {
            _fixture = fixture;

            var httpClient = _fixture.CreateHttpClientMock(_fixture.WalletAddress);
            _client = new UnauthenticatedClient(httpClient);
        }

        [Theory]
        [InlineData("https://example.com/alice")]
        [InlineData("$example.com/bond")]
        public async Task GetWalletAddressAsync_WithUrlOrPaymentPointer_ReturnsModel(string url)
        {
            var result = await _client.GetWalletAddressAsync(url);

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_fixture.WalletAddress);
        }

        [Theory]
        [InlineData("")]
        [InlineData("foobar")]
        public async Task GetWalletAddressAsync_InvalidInput_Throws(string url)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _client.GetWalletAddressAsync(url));
        }

        [Fact]
        public async Task GetWalletAddressAsync_MalformedJson_ThrowsOpenPaymentsApiException()
        {
            var httpClient = _fixture.CreateHttpClientMock(HttpStatusCode.OK, "{ this is not json");
            var client = new UnauthenticatedClient(httpClient);

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(
                () => client.GetWalletAddressAsync("https://example.com/alice")
            );

            exception.StatusCode.Should().Be(200);
            exception.InnerException.Should().BeAssignableTo<Newtonsoft.Json.JsonException>();
        }
    }

    [Collection("UnauthenticatedClient")]
    public class UnauthenticatedClient_WalletAddressKeys_Tests
    {
        private readonly UnauthenticatedClient _client;
        private readonly UnauthenticatedClientFixture _fixture;

        public UnauthenticatedClient_WalletAddressKeys_Tests(UnauthenticatedClientFixture fixture)
        {
            _fixture = fixture;

            var httpClient = _fixture.CreateHttpClientMock(_fixture.WalletAddressKeys);
            _client = new UnauthenticatedClient(httpClient);
        }

        [Theory]
        [InlineData("https://example.com/alice")]
        [InlineData("$example.com/bond")]
        public async Task GetWalletAddressKeysAsync_WithUrlOrPaymentPointer_ReturnsModel(string url)
        {
            var result = await _client.GetWalletAddressKeysAsync(url);

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_fixture.WalletAddressKeys);
        }

        [Theory]
        [InlineData("")]
        [InlineData("foobar")]
        public async Task GetWalletAddressKeysAsync_InvalidInput_Throws(string url)
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _client.GetWalletAddressKeysAsync(url)
            );
        }
    }

    [Collection("UnauthenticatedClient")]
    public class UnauthenticatedClient_IncomingPayment_Tests
    {
        private readonly string _url;
        private readonly UnauthenticatedClient _client;
        private readonly UnauthenticatedClientFixture _fixture;

        public UnauthenticatedClient_IncomingPayment_Tests(UnauthenticatedClientFixture fixture)
        {
            _fixture = fixture;

            var httpClient = _fixture.CreateHttpClientMock(_fixture.IncomingPayment);
            _client = new UnauthenticatedClient(httpClient);

            _url = _fixture.BaseUrl + "/incoming";
        }

        [Fact]
        public async Task GetIncomingPaymentAsync_Valid_ReturnsModel()
        {
            var result = await _client.GetIncomingPaymentAsync(_url);

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_fixture.IncomingPayment);
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("   ")]
        public async Task GetIncomingPaymentAsync_InvalidInput_Throws(string url)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _client.GetIncomingPaymentAsync(url));
        }

        [Fact]
        public async Task GetIncomingPaymentAsync_ServerReturns404_ThrowsOpenPaymentsApiException()
        {
            var httpClient = _fixture.CreateHttpClientMock(
                HttpStatusCode.NotFound,
                "{\"error\":\"not found\"}"
            );
            var client = new UnauthenticatedClient(httpClient);

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(
                () => client.GetIncomingPaymentAsync(_url)
            );

            exception.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetIncomingPaymentAsync_NullJson_ThrowsOpenPaymentsApiException()
        {
            var httpClient = _fixture.CreateHttpClientMock(HttpStatusCode.OK, "null");
            var client = new UnauthenticatedClient(httpClient);

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(
                () => client.GetIncomingPaymentAsync(_url)
            );

            exception.StatusCode.Should().Be(200);
        }
    }
}

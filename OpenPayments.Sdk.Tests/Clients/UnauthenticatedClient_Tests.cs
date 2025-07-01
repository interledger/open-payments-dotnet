using FluentAssertions;
using OpenPayments.Sdk.Clients;

namespace OpenPayments.Sdk.Tests.Clients;

public class UnauthenticatedClient_Tests
{
    [Collection("UnauthenticatedClient")]
    public class UnauthenticatedClient_WalletAddress_Tests
    {
        private readonly IUnauthenticatedClient _client;
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
    }
}
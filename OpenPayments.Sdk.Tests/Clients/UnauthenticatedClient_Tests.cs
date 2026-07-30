using System.Net;
using FluentAssertions;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Exceptions;

namespace OpenPayments.Sdk.Tests.Clients;

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
    }

    [Collection("UnauthenticatedClient")]
    public class UnauthenticatedClient_WalletAddressErrors_Tests
    {
        private readonly UnauthenticatedClientFixture _fixture;

        public UnauthenticatedClient_WalletAddressErrors_Tests(UnauthenticatedClientFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetWalletAddressAsync_On404_Throws()
        {
            var httpClient = _fixture.CreateHttpClientMock(HttpStatusCode.NotFound, "");
            var client = new UnauthenticatedClient(httpClient);

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
                client.GetWalletAddressAsync("https://example.com/alice")
            );

            exception.StatusCode.Should().Be(404);
            exception.ErrorCode.Should().BeNull();
            exception.ResponseBody.Should().BeEmpty();
        }

        [Fact]
        public async Task GetWalletAddressKeysAsync_On429_ThrowsWithRetryAfter()
        {
            var httpClient = _fixture.CreateHttpClientMock(
                HttpStatusCode.TooManyRequests,
                "rate limited",
                ("Retry-After", "60")
            );
            var client = new UnauthenticatedClient(httpClient);

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
                client.GetWalletAddressKeysAsync("https://example.com/alice")
            );

            exception.StatusCode.Should().Be(429);
            exception.RetryAfter.Should().Be(TimeSpan.FromSeconds(60));
            exception.ResponseBody.Should().Be("rate limited");
        }

        [Fact]
        public async Task GetWalletAddressAsync_On200WithEmptyBody_ThrowsCarryingThe200()
        {
            var httpClient = _fixture.CreateHttpClientMock(HttpStatusCode.OK, "");
            var client = new UnauthenticatedClient(httpClient);

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
                client.GetWalletAddressAsync("https://example.com/alice")
            );

            exception.StatusCode.Should().Be(200);
        }
    }

    [Collection("UnauthenticatedClient")]
    public class UnauthenticatedClient_IncomingPaymentErrors_Tests
    {
        private readonly UnauthenticatedClientFixture _fixture;

        public UnauthenticatedClient_IncomingPaymentErrors_Tests(
            UnauthenticatedClientFixture fixture
        )
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetIncomingPaymentAsync_On404_ThrowsOpenPaymentsApiException()
        {
            var body = """{"error":{"code":"not_found","description":"No such payment"}}""";
            var httpClient = _fixture.CreateHttpClientMock(HttpStatusCode.NotFound, body);
            var client = new UnauthenticatedClient(httpClient);

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
                client.GetIncomingPaymentAsync("https://example.com/incoming/1234")
            );

            exception.StatusCode.Should().Be(404);
            exception.ErrorCode.Should().Be("not_found");
            exception.Description.Should().Be("No such payment");
            exception.ResponseBody.Should().Be(body);
        }

        [Fact]
        public async Task GetIncomingPaymentAsync_On429_ThrowsWithRetryAfter()
        {
            var httpClient = _fixture.CreateHttpClientMock(
                HttpStatusCode.TooManyRequests,
                "",
                ("Retry-After", "15")
            );
            var client = new UnauthenticatedClient(httpClient);

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
                client.GetIncomingPaymentAsync("https://example.com/incoming/1234")
            );

            exception.StatusCode.Should().Be(429);
            exception.RetryAfter.Should().Be(TimeSpan.FromSeconds(15));
        }

        [Fact]
        public async Task GetIncomingPaymentAsync_On200WithEmptyBody_ThrowsInsteadOfInvalidOperation()
        {
            var httpClient = _fixture.CreateHttpClientMock(HttpStatusCode.OK, "");
            var client = new UnauthenticatedClient(httpClient);

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
                client.GetIncomingPaymentAsync("https://example.com/incoming/1234")
            );

            exception.StatusCode.Should().Be(200);
            exception.Description.Should().Be("The server returned an empty or null response body.");
        }
    }
}

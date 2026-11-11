using System.Net;
using FluentAssertions;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Generated.Resource;

namespace OpenPayments.Sdk.Tests.Clients;

public class AuthenticatedClient_Tests
{
    [Collection("AuthenticatedClient")]
    public class AuthenticatedClient_RequestGrant_Tests
    {
        private readonly AuthenticatedClient _client;
        private readonly AuthenticatedClientFixture _fixture;

        public AuthenticatedClient_RequestGrant_Tests(AuthenticatedClientFixture fixture)
        {
            _fixture = fixture;
            var httpClient = _fixture.CreateHttpClientMock(_fixture.ApprovedGrantResponse);
            _client = new AuthenticatedClient(
                httpClient,
                _fixture.PrivateKey,
                _fixture.KeyId,
                _fixture.ClientUrl
            );
        }

        [Fact]
        public async Task RequestGrantAsync_ReturnsModel()
        {
            var result = await _client.RequestGrantAsync(
                _fixture.RequestGrantArgs,
                _fixture.RequestGrantBody
            );

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_fixture.ApprovedGrantResponse);
        }
    }

    [Collection("AuthenticatedClient")]
    public class AuthenticatedClient_ContinueGrant_Tests
    {
        private readonly AuthenticatedClient _client;
        private readonly AuthenticatedClientFixture _fixture;

        public AuthenticatedClient_ContinueGrant_Tests(AuthenticatedClientFixture fixture)
        {
            _fixture = fixture;
            var httpClient = _fixture.CreateHttpClientMock(_fixture.ApprovedGrantResponse);
            _client = new AuthenticatedClient(
                httpClient,
                _fixture.PrivateKey,
                _fixture.KeyId,
                _fixture.ClientUrl
            );
        }

        [Fact]
        public async Task ContinueGrantAsync_ReturnsModel()
        {
            var result = await _client.ContinueGrantAsync(
                _fixture.GrantWithTokenArgs,
                _fixture.ContinueGrantBody
            );

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_fixture.ApprovedGrantResponse);
        }
    }

    [Collection("AuthenticatedClient")]
    public class AuthenticatedClient_CancelGrant_Tests
    {
        private readonly AuthenticatedClient _client;
        private readonly AuthenticatedClientFixture _fixture;

        public AuthenticatedClient_CancelGrant_Tests(AuthenticatedClientFixture fixture)
        {
            _fixture = fixture;
            var httpClient = _fixture.CreateHttpClientMock();
            _client = new AuthenticatedClient(
                httpClient,
                _fixture.PrivateKey,
                _fixture.KeyId,
                _fixture.ClientUrl
            );
        }

        [Fact]
        public async Task CancelGrantAsync_ReturnsModel()
        {
            await _client.CancelGrantAsync(_fixture.GrantWithTokenArgs);
        }
    }

    [Collection("AuthenticatedClient")]
    public class AuthenticatedClient_RotateToken_Tests
    {
        private readonly AuthenticatedClient _client;
        private readonly AuthenticatedClientFixture _fixture;

        public AuthenticatedClient_RotateToken_Tests(AuthenticatedClientFixture fixture)
        {
            _fixture = fixture;
            var httpClient = _fixture.CreateHttpClientMock(_fixture.TokenResponse);
            _client = new AuthenticatedClient(
                httpClient,
                _fixture.PrivateKey,
                _fixture.KeyId,
                _fixture.ClientUrl
            );
        }

        [Fact]
        public async Task RotateTokenAsync_ReturnsModel()
        {
            var result = await _client.RotateTokenAsync(_fixture.GrantWithTokenArgs);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_fixture.TokenResponse);
        }
    }

    [Collection("AuthenticatedClient")]
    public class AuthenticatedClient_RevokeToken_Tests
    {
        private readonly AuthenticatedClient _client;
        private readonly AuthenticatedClientFixture _fixture;

        public AuthenticatedClient_RevokeToken_Tests(AuthenticatedClientFixture fixture)
        {
            _fixture = fixture;
            var httpClient = _fixture.CreateHttpClientMock();
            _client = new AuthenticatedClient(
                httpClient,
                _fixture.PrivateKey,
                _fixture.KeyId,
                _fixture.ClientUrl
            );
        }

        [Fact]
        public async Task RevokeTokenAsync_ReturnsModel()
        {
            await _client.RevokeTokenAsync(_fixture.GrantWithTokenArgs);
        }
    }

    [Collection("AuthenticatedClient")]
    public class AuthenticatedClient_CreateIncomingPayment_Tests
    {
        private AuthenticatedClient? _client;
        private readonly AuthenticatedClientFixture _fixture;

        public AuthenticatedClient_CreateIncomingPayment_Tests(AuthenticatedClientFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task CreateIncomingPaymentAsync_ReturnsModel()
        {
            var httpClient = _fixture.CreateHttpClientMock(
                _fixture.CreateIncomingPaymentResponse,
                HttpStatusCode.Created
            );
            _client = new AuthenticatedClient(
                httpClient,
                _fixture.PrivateKey,
                _fixture.KeyId,
                _fixture.ClientUrl
            );
            var result = await _client.CreateIncomingPaymentAsync(
                _fixture.GrantWithTokenArgs,
                _fixture.CreateIncomingPaymentBody
            );
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_fixture.CreateIncomingPaymentResponse);
        }

        [Fact]
        public async Task CreateIncomingPaymentAsync_ReturnsModelWithMetadata()
        {
            var httpClient = _fixture.CreateHttpClientMock(
                _fixture.CreateIncomingPaymentResponseWithMetadata,
                HttpStatusCode.Created
            );
            _client = new AuthenticatedClient(
                httpClient,
                _fixture.PrivateKey,
                _fixture.KeyId,
                _fixture.ClientUrl
            );
            var result = await _client.CreateIncomingPaymentAsync(
                _fixture.GrantWithTokenArgs,
                _fixture.CreateIncomingPaymentBody
            );
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_fixture.CreateIncomingPaymentResponseWithMetadata);
        }
    }

    [Collection("AuthenticatedClient")]
    public class AuthenticatedClient_CreateQuote_Tests
    {
        private readonly AuthenticatedClient _client;
        private readonly AuthenticatedClientFixture _fixture;

        public AuthenticatedClient_CreateQuote_Tests(AuthenticatedClientFixture fixture)
        {
            _fixture = fixture;
            var httpClient = _fixture.CreateHttpClientMock(
                _fixture.CreateQuoteResponse,
                HttpStatusCode.Created
            );
            _client = new AuthenticatedClient(
                httpClient,
                _fixture.PrivateKey,
                _fixture.KeyId,
                _fixture.ClientUrl
            );
        }

        [Fact]
        public async Task CreateQuoteAsync_ReturnsModel()
        {
            var result = await _client.CreateQuoteAsync(
                _fixture.GrantWithTokenArgs,
                _fixture.CreateQuoteBody
            );
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_fixture.CreateQuoteResponse);
        }

        [Fact]
        public async Task CreateQuoteWithDebitAmountAsync_ReturnsModel()
        {
            var result = await _client.CreateQuoteAsync(
                _fixture.GrantWithTokenArgs,
                _fixture.CreateQuoteBodyWithDebitAmount
            );
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_fixture.CreateQuoteResponse);
        }

        [Fact]
        public async Task CreateQuoteWithReceiveAmountAsync_ReturnsModel()
        {
            var result = await _client.CreateQuoteAsync(
                _fixture.GrantWithTokenArgs,
                _fixture.CreateQuoteBodyWithReceiveAmount
            );
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_fixture.CreateQuoteResponse);
        }
    }

    [Collection("AuthenticatedClient")]
    public class AuthenticatedClient_CreateOutgoingPayment_Tests
    {
        private readonly AuthenticatedClient _client;
        private readonly AuthenticatedClientFixture _fixture;

        public AuthenticatedClient_CreateOutgoingPayment_Tests(AuthenticatedClientFixture fixture)
        {
            _fixture = fixture;
            var httpClient = _fixture.CreateHttpClientMock(
                _fixture.CreateOutgoingPaymentResponse,
                HttpStatusCode.Created
            );
            _client = new AuthenticatedClient(
                httpClient,
                _fixture.PrivateKey,
                _fixture.KeyId,
                _fixture.ClientUrl
            );
        }

        [Fact]
        public async Task CreateOutgoingPaymentFromQuoteAsync_ReturnsModel()
        {
            var result = await _client.CreateOutgoingPaymentAsync(
                _fixture.GrantWithTokenArgs,
                _fixture.CreateOutgoingPaymentBodyFromQuote
            );
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_fixture.CreateOutgoingPaymentResponse);
        }

        [Fact]
        public async Task CreateOutgoingPaymentFromIncomingAsync_ReturnsModel()
        {
            var result = await _client.CreateOutgoingPaymentAsync(
                _fixture.GrantWithTokenArgs,
                _fixture.CreateOutgoingPaymentBodyFromIncomingPayment
            );
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_fixture.CreateOutgoingPaymentResponse);
        }
    }

    [Collection("AuthenticatedClient")]
    public class AuthenticatedClient_GetOutgoingPayment_Tests
    {
        private readonly AuthenticatedClient _client;
        private readonly AuthenticatedClientFixture _fixture;

        public AuthenticatedClient_GetOutgoingPayment_Tests(AuthenticatedClientFixture fixture)
        {
            _fixture = fixture;
            var httpClient = _fixture.CreateHttpClientMock(
                _fixture.GetOutgoingPaymentResponse,
                HttpStatusCode.OK
            );
            _client = new AuthenticatedClient(
                httpClient,
                _fixture.PrivateKey,
                _fixture.KeyId,
                _fixture.ClientUrl
            );
        }

        [Fact]
        public async Task GetOutgoingPaymentAsync_ReturnsModel()
        {
            var result = await _client.GetOutgoingPaymentAsync(_fixture.GrantWithTokenArgs);
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_fixture.GetOutgoingPaymentResponse);
        }
    }

    [Collection("AuthenticatedClient")]
    public class AuthenticatedClient_ListOutgoingPayments_Tests
    {
        private readonly AuthenticatedClient _client;
        private readonly AuthenticatedClientFixture _fixture;

        public AuthenticatedClient_ListOutgoingPayments_Tests(AuthenticatedClientFixture fixture)
        {
            _fixture = fixture;
            var httpClient = _fixture.CreateHttpClientMock(
                _fixture.ListOutgoingPaymentsResponse,
                HttpStatusCode.OK
            );
            _client = new AuthenticatedClient(
                httpClient,
                _fixture.PrivateKey,
                _fixture.KeyId,
                _fixture.ClientUrl
            );
        }

        [Fact]
        public async Task ListOutgoingPaymentsAsync_ReturnsModel()
        {
            var result = await _client.ListOutgoingPaymentsAsync(
                _fixture.GrantWithTokenArgs,
                _fixture.ListOutgoingPaymentQuery
            );
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_fixture.ListOutgoingPaymentsResponse);
        }
    }

    [Collection("AuthenticatedClient")]
    public class AuthenticatedClient_GetGrantSpentAmounts_Tests
    {
        private const string Usd2600 =
            "{\"value\":\"2600\",\"assetCode\":\"USD\",\"assetScale\":2}";
        private const string Usd2500 =
            "{\"value\":\"2500\",\"assetCode\":\"USD\",\"assetScale\":2}";

        private readonly AuthenticatedClientFixture _fixture;

        public AuthenticatedClient_GetGrantSpentAmounts_Tests(AuthenticatedClientFixture fixture)
        {
            _fixture = fixture;
        }

        private AuthenticatedClient CreateClient(
            List<CapturedRequest> captured,
            string? json,
            HttpStatusCode code = HttpStatusCode.OK
        ) =>
            new(
                _fixture.CreateCapturingHttpClientMock(captured, json, code),
                _fixture.PrivateKey,
                _fixture.KeyId,
                _fixture.ClientUrl
            );

        private static AuthRequestArgs Args(string url = "https://example.com/op") =>
            new() { Url = new Uri(url), AccessToken = "tok" };

        [Fact]
        public async Task GetGrantSpentAmountsAsync_ReturnsBothAmounts()
        {
            var client = CreateClient(
                [],
                $"{{\"spentDebitAmount\":{Usd2600},\"spentReceiveAmount\":{Usd2500}}}"
            );

            var result = await client.GetGrantSpentAmountsAsync(Args());

            result.SpentDebitAmount!.Value.Should().Be("2600");
            result.SpentReceiveAmount!.Value.Should().Be("2500");
            result.SpentReceiveAmount.AssetCode.Should().Be("USD");
        }

        [Fact]
        public async Task GetGrantSpentAmountsAsync_NullAmounts_ReturnsNulls()
        {
            var client = CreateClient(
                [],
                "{\"spentDebitAmount\":null,\"spentReceiveAmount\":null}"
            );

            var result = await client.GetGrantSpentAmountsAsync(Args());

            result.SpentDebitAmount.Should().BeNull();
            result.SpentReceiveAmount.Should().BeNull();
        }

        [Fact]
        public async Task GetGrantSpentAmountsAsync_OneNullAmount()
        {
            var client = CreateClient(
                [],
                $"{{\"spentDebitAmount\":{Usd2600},\"spentReceiveAmount\":null}}"
            );

            var result = await client.GetGrantSpentAmountsAsync(Args());

            result.SpentDebitAmount!.Value.Should().Be("2600");
            result.SpentReceiveAmount.Should().BeNull();
        }

        [Theory]
        [InlineData("https://example.com/op")]
        [InlineData("https://example.com/op/")]
        public async Task GetGrantSpentAmountsAsync_SendsGetToOutgoingPaymentGrant(string url)
        {
            var captured = new List<CapturedRequest>();
            var client = CreateClient(
                captured,
                "{\"spentDebitAmount\":null,\"spentReceiveAmount\":null}"
            );

            await client.GetGrantSpentAmountsAsync(Args(url));

            captured.Should().ContainSingle();
            captured[0].Method.Should().Be(HttpMethod.Get);
            captured[0].Uri.Should().Be(new Uri("https://example.com/op/outgoing-payment-grant"));
            captured[0].Authorization.Should().Be("GNAP tok");
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.Forbidden)]
        public async Task GetGrantSpentAmountsAsync_ErrorStatus_ThrowsApiException(
            HttpStatusCode code
        )
        {
            var client = CreateClient([], "{\"error\":{\"description\":\"denied\"}}", code);

            var act = () => client.GetGrantSpentAmountsAsync(Args());

            await act.Should().ThrowAsync<ApiException>().Where(e => e.StatusCode == (int)code);
        }
    }
}

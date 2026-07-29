using System.Net;
using FluentAssertions;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Exceptions;
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
            _client = new AuthenticatedClient(httpClient, httpClient, _fixture.ClientUrl);
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
            _client = new AuthenticatedClient(httpClient, httpClient, _fixture.ClientUrl);
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
            _client = new AuthenticatedClient(httpClient, httpClient, _fixture.ClientUrl);
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
            _client = new AuthenticatedClient(httpClient, httpClient, _fixture.ClientUrl);
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
            _client = new AuthenticatedClient(httpClient, httpClient, _fixture.ClientUrl);
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
            _client = new AuthenticatedClient(httpClient, httpClient, _fixture.ClientUrl);
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
            _client = new AuthenticatedClient(httpClient, httpClient, _fixture.ClientUrl);
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
            _client = new AuthenticatedClient(httpClient, httpClient, _fixture.ClientUrl);
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
            _client = new AuthenticatedClient(httpClient, httpClient, _fixture.ClientUrl);
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
            _client = new AuthenticatedClient(httpClient, httpClient, _fixture.ClientUrl);
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
            _client = new AuthenticatedClient(httpClient, httpClient, _fixture.ClientUrl);
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
    public class AuthenticatedClient_AuthServerErrors_Tests
    {
        private readonly AuthenticatedClientFixture _fixture;

        public AuthenticatedClient_AuthServerErrors_Tests(AuthenticatedClientFixture fixture)
        {
            _fixture = fixture;
        }

        private AuthenticatedClient ClientReturning(
            HttpStatusCode status,
            string body,
            params (string Name, string Value)[] headers
        )
        {
            var httpClient = _fixture.CreateHttpClientMock(status, body, headers);
            return new AuthenticatedClient(httpClient, httpClient, _fixture.ClientUrl);
        }

        [Fact]
        public async Task RequestGrantAsync_On500_ThrowsWithStatusCodeAndRawBody()
        {
            var body =
                """{"error":{"code":"invalid_client","description":"Client is not valid"}}""";
            var client = ClientReturning(HttpStatusCode.InternalServerError, body);

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
                client.RequestGrantAsync(_fixture.RequestGrantArgs, _fixture.RequestGrantBody)
            );

            exception.StatusCode.Should().Be(500);
            exception.ErrorCode.Should().Be("invalid_client");
            exception.Description.Should().Be("Client is not valid");
            exception.ResponseBody.Should().Be(body);
        }

        [Fact]
        public async Task ContinueGrantAsync_On429_ThrowsWithRetryAfter()
        {
            var body = """{"error":{"code":"too_fast","description":"Slow down"}}""";
            var client = ClientReturning(
                HttpStatusCode.TooManyRequests,
                body,
                ("Retry-After", "30")
            );

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
                client.ContinueGrantAsync(_fixture.GrantWithTokenArgs, _fixture.ContinueGrantBody)
            );

            exception.StatusCode.Should().Be(429);
            exception.ErrorCode.Should().Be("too_fast");
            exception.RetryAfter.Should().Be(TimeSpan.FromSeconds(30));
            exception.ResponseBody.Should().Be(body);
        }

        [Fact]
        public async Task CancelGrantAsync_On404_Throws()
        {
            var body = """{"error":{"code":"invalid_continuation","description":"Unknown grant"}}""";
            var client = ClientReturning(HttpStatusCode.NotFound, body);

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
                client.CancelGrantAsync(_fixture.GrantWithTokenArgs)
            );

            exception.StatusCode.Should().Be(404);
            exception.ErrorCode.Should().Be("invalid_continuation");
            exception.ResponseBody.Should().Be(body);
        }

        [Fact]
        public async Task RotateTokenAsync_On200WithUnusableBody_ThrowsCarryingThe200()
        {
            var client = ClientReturning(HttpStatusCode.OK, "");

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
                client.RotateTokenAsync(_fixture.GrantWithTokenArgs)
            );

            exception.StatusCode.Should().Be(200);
            exception.ErrorCode.Should().BeNull();
        }

        [Fact]
        public async Task RevokeTokenAsync_On401_Throws()
        {
            var client = ClientReturning(HttpStatusCode.Unauthorized, """{"error":"invalid_client"}""");

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
                client.RevokeTokenAsync(_fixture.GrantWithTokenArgs)
            );

            exception.StatusCode.Should().Be(401);
            exception.ErrorCode.Should().Be("invalid_client");
        }
    }

    [Collection("AuthenticatedClient")]
    public class AuthenticatedClient_ResourceServerErrors_Tests
    {
        private readonly AuthenticatedClientFixture _fixture;

        public AuthenticatedClient_ResourceServerErrors_Tests(AuthenticatedClientFixture fixture)
        {
            _fixture = fixture;
        }

        private AuthenticatedClient ClientReturning(
            HttpStatusCode status,
            string body,
            params (string Name, string Value)[] headers
        )
        {
            var httpClient = _fixture.CreateHttpClientMock(status, body, headers);
            return new AuthenticatedClient(httpClient, httpClient, _fixture.ClientUrl);
        }

        [Fact]
        public async Task CreateIncomingPaymentAsync_On401_ThrowsWithStatusCodeAndRawBody()
        {
            var body =
                """{"error":{"code":"unauthorized","description":"Access token is invalid"}}""";
            var client = ClientReturning(HttpStatusCode.Unauthorized, body);

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
                client.CreateIncomingPaymentAsync(
                    _fixture.GrantWithTokenArgs,
                    _fixture.CreateIncomingPaymentBody
                )
            );

            exception.StatusCode.Should().Be(401);
            exception.ErrorCode.Should().Be("unauthorized");
            exception.Description.Should().Be("Access token is invalid");
            exception.ResponseBody.Should().Be(body);
        }

        [Fact]
        public async Task GetQuoteAsync_On429WithHtmlBody_ThrowsWithRetryAfterAndRawBody()
        {
            var body = "<html><body>Too Many Requests</body></html>";
            var client = ClientReturning(
                HttpStatusCode.TooManyRequests,
                body,
                ("Retry-After", "120")
            );

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
                client.GetQuoteAsync(_fixture.GrantWithTokenArgs)
            );

            exception.StatusCode.Should().Be(429);
            exception.ErrorCode.Should().BeNull();
            exception.ResponseBody.Should().Be(body);
            exception.RetryAfter.Should().Be(TimeSpan.FromMinutes(2));
        }

        [Fact]
        public async Task GetOutgoingPaymentAsync_On500_Throws()
        {
            var body = """{"error":{"code":"internal","description":"Server error"}}""";
            var client = ClientReturning(HttpStatusCode.InternalServerError, body);

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
                client.GetOutgoingPaymentAsync(_fixture.GrantWithTokenArgs)
            );

            exception.StatusCode.Should().Be(500);
            exception.ErrorCode.Should().Be("internal");
            exception.ResponseBody.Should().Be(body);
        }

        [Fact]
        public async Task ListIncomingPaymentsAsync_On403_Throws()
        {
            var body = """{"error":{"code":"forbidden","description":"Not permitted"}}""";
            var client = ClientReturning(HttpStatusCode.Forbidden, body);

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
                client.ListIncomingPaymentsAsync(
                    _fixture.GrantWithTokenArgs,
                    new ListIncomingPaymentQuery { WalletAddress = "https://example.com/wallet/1234" }
                )
            );

            exception.StatusCode.Should().Be(403);
            exception.ErrorCode.Should().Be("forbidden");
        }

        [Fact]
        public async Task CreateQuoteAsync_On201WithMalformedBody_ThrowsCarryingThe201()
        {
            var client = ClientReturning(HttpStatusCode.Created, "{not json");

            var exception = await Assert.ThrowsAsync<OpenPaymentsApiException>(() =>
                client.CreateQuoteAsync(_fixture.GrantWithTokenArgs, _fixture.CreateQuoteBody)
            );

            exception.StatusCode.Should().Be(201);
            exception.ResponseBody.Should().Be("{not json");
        }
    }
}

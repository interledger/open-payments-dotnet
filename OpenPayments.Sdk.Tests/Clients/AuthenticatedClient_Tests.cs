using System.Net;
using FluentAssertions;
using OpenPayments.Sdk.Clients;

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
        private readonly AuthenticatedClient _client;
        private readonly AuthenticatedClientFixture _fixture;

        public AuthenticatedClient_CreateIncomingPayment_Tests(AuthenticatedClientFixture fixture)
        {
            _fixture = fixture;
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
        }

        [Fact]
        public async Task CreateIncomingPaymentAsync_ReturnsModel()
        {
            var result = await _client.CreateIncomingPaymentAsync(
                _fixture.GrantWithTokenArgs,
                _fixture.CreateIncomingPaymentBody
            );
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_fixture.CreateIncomingPaymentResponse);
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
        public async Task CreateOutgoingPaymentAsync_ReturnsModel()
        {
            var result = await _client.CreateOutgoingPaymentAsync(
                _fixture.GrantWithTokenArgs,
                _fixture.CreateOutgoingPaymentBody
            );
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_fixture.CreateOutgoingPaymentResponse);
        }
    }
}

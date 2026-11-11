using System.Net;
using FluentAssertions;
using OpenPayments.Sdk.Clients;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.Generated.Resource;
using Amount = OpenPayments.Sdk.Generated.Resource.Amount;
using ApiException = OpenPayments.Sdk.Generated.Resource.ApiException;
using AuthAmount = OpenPayments.Sdk.Generated.Auth.Amount;

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

    [Collection("AuthenticatedClient")]
    public class AuthenticatedClient_SubjectGrant_Tests
    {
        private const string SubjectJson =
            "\"subject\":{\"sub_ids\":[{\"id\":\"https://example.com/alice\",\"format\":\"uri\"}]}";
        private const string ContinueJson =
            "\"continue\":{\"access_token\":{\"value\":\"c\"},\"uri\":\"https://example.com/continue/1\"}";

        private readonly AuthenticatedClientFixture _fixture;

        public AuthenticatedClient_SubjectGrant_Tests(AuthenticatedClientFixture fixture)
        {
            _fixture = fixture;
        }

        private AuthenticatedClient CreateClient(List<CapturedRequest> captured, string json) =>
            new(
                _fixture.CreateCapturingHttpClientMock(captured, json),
                _fixture.PrivateKey,
                _fixture.KeyId,
                _fixture.ClientUrl
            );

        private static Subject AliceSubject =>
            new()
            {
                Sub_ids = [new Sub_ids { Id = "https://example.com/alice", Format = Sub_idsFormat.Uri }],
            };

        private static InteractRequest Interact =>
            new()
            {
                Start = [Start.Redirect],
                Finish = new Finish
                {
                    Method = FinishMethod.Redirect,
                    Uri = new Uri("https://example.com/finish"),
                    Nonce = "n",
                },
            };

        [Fact]
        public async Task RequestGrantAsync_SubjectOnlyInteractive_SerializesSubjectWithoutAccessToken()
        {
            var captured = new List<CapturedRequest>();
            var client = CreateClient(
                captured,
                JsonConvert.SerializeObject(_fixture.ApprovedGrantResponse)
            );

            await client.RequestGrantAsync(
                _fixture.RequestGrantArgs,
                new GrantCreateBodyWithInteract { Subject = AliceSubject, Interact = Interact }
            );

            var json = JObject.Parse(captured[0].Body!);
            json["access_token"].Should().BeNull();
            json["subject"]!["sub_ids"]![0]!["id"]!.Value<string>().Should().Be("https://example.com/alice");
            json["subject"]!["sub_ids"]![0]!["format"]!.Value<string>().Should().Be("uri");
            json["client"]!.Type.Should().Be(JTokenType.String);
            json["interact"].Should().NotBeNull();
        }

        [Fact]
        public async Task RequestGrantAsync_InteractiveWithAccessToken_StillSerializesAccessToken()
        {
            var captured = new List<CapturedRequest>();
            var client = CreateClient(
                captured,
                JsonConvert.SerializeObject(_fixture.ApprovedGrantResponse)
            );
            var body = new GrantCreateBodyWithInteract
            {
                AccessToken = new AccessToken
                {
                    Access =
                    [
                        new OutgoingAccess
                        {
                            Identifier = new Uri("https://example.com/alice"),
                            Actions = [Actions.Create],
                            Limits = new OutgoingAccessLimits
                            {
                                DebitAmount = new AuthAmount("100", "USD", 2),
                            },
                        },
                    ],
                },
                Interact = Interact,
            };

            await client.RequestGrantAsync(_fixture.RequestGrantArgs, body);

            var json = JObject.Parse(captured[0].Body!);
            json["access_token"]!["access"]![0]!["type"]!.Value<string>().Should().Be("outgoing-payment");
            json["access_token"]!["access"]![0]!["limits"]!["debitAmount"]!["value"]!.Value<string>().Should().Be("100");
            json["subject"].Should().BeNull();
        }

        [Fact]
        public async Task ContinueGrantAsync_SubjectResponse_DeserializesSubject()
        {
            var client = CreateClient([], "{" + SubjectJson + "," + ContinueJson + "}");

            var result = await client.ContinueGrantAsync(_fixture.GrantWithTokenArgs, null);

            result.Subject!.Sub_ids.Single().Id.Should().Be("https://example.com/alice");
            result.Subject.Sub_ids.Single().Format.Should().Be(Sub_idsFormat.Uri);
            result.AccessToken.Should().BeNull();
        }

        [Fact]
        public async Task ContinueGrantAsync_SubjectAndAccessTokenResponse_DeserializesBoth()
        {
            const string token =
                "\"access_token\":{\"value\":\"t\",\"manage\":\"https://example.com/token/1\",\"access\":[{\"type\":\"incoming-payment\",\"actions\":[\"read\"]}]}";
            var client = CreateClient([], "{" + SubjectJson + "," + token + "," + ContinueJson + "}");

            var result = await client.ContinueGrantAsync(_fixture.GrantWithTokenArgs, null);

            result.Subject!.Sub_ids.Single().Id.Should().Be("https://example.com/alice");
            result.AccessToken!.Value.Should().Be("t");
        }
    }

    [Collection("AuthenticatedClient")]
    public class AuthenticatedClient_RequestGrantValidation_Tests
    {
        private readonly AuthenticatedClientFixture _fixture;

        public AuthenticatedClient_RequestGrantValidation_Tests(AuthenticatedClientFixture fixture)
        {
            _fixture = fixture;
        }

        private AuthenticatedClient CreateClient(List<CapturedRequest> captured) =>
            new(
                _fixture.CreateCapturingHttpClientMock(
                    captured,
                    JsonConvert.SerializeObject(_fixture.ApprovedGrantResponse)
                ),
                _fixture.PrivateKey,
                _fixture.KeyId,
                _fixture.ClientUrl
            );

        private static OutgoingAccessLimits Both =>
            new()
            {
                DebitAmount = new AuthAmount("1", "USD", 2),
                ReceiveAmount = new AuthAmount("1", "USD", 2),
            };

        private static OutgoingAccess Outgoing(OutgoingAccessLimits limits) =>
            new()
            {
                Identifier = new Uri("https://example.com/alice"),
                Actions = [Actions.Create],
                Limits = limits,
            };

        private static GrantCreateBody BodyWith(params AccessItem[] items) =>
            new() { AccessToken = new AccessToken { Access = [.. items] } };

        [Fact]
        public async Task RequestGrantAsync_NoAccessTokenNoSubject_Throws()
        {
            var captured = new List<CapturedRequest>();
            var client = CreateClient(captured);

            var act = () => client.RequestGrantAsync(_fixture.RequestGrantArgs, new GrantCreateBody());

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*\"access_token\" or \"subject\"*");
            captured.Should().BeEmpty();
        }

        [Fact]
        public async Task RequestGrantAsync_OutgoingLimitsWithBothAmounts_Throws()
        {
            var captured = new List<CapturedRequest>();
            var client = CreateClient(captured);

            var act = () => client.RequestGrantAsync(_fixture.RequestGrantArgs, BodyWith(Outgoing(Both)));

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*\"debitAmount\" and \"receiveAmount\"*");
            captured.Should().BeEmpty();
        }

        [Fact]
        public async Task RequestGrantAsync_SecondOutgoingItemWithBothAmounts_Throws()
        {
            var captured = new List<CapturedRequest>();
            var client = CreateClient(captured);
            var debitOnly = new OutgoingAccessLimits { DebitAmount = new AuthAmount("1", "USD", 2) };

            var act = () =>
                client.RequestGrantAsync(
                    _fixture.RequestGrantArgs,
                    BodyWith(Outgoing(debitOnly), Outgoing(Both))
                );

            await act.Should().ThrowAsync<ArgumentException>();
            captured.Should().BeEmpty();
        }

        [Fact]
        public async Task RequestGrantAsync_PlainAccessItemOutgoingWithBothAmounts_Throws()
        {
            var captured = new List<CapturedRequest>();
            var client = CreateClient(captured);
            var item = new AccessItem
            {
                Type = AccessType.OutgoingPayment,
                Actions = [Actions.Create],
                Limits = Both,
            };

            var act = () => client.RequestGrantAsync(_fixture.RequestGrantArgs, BodyWith(item));

            await act.Should().ThrowAsync<ArgumentException>();
            captured.Should().BeEmpty();
        }

        [Fact]
        public async Task RequestGrantAsync_SubjectOnly_Sends()
        {
            var captured = new List<CapturedRequest>();
            var client = CreateClient(captured);

            await client.RequestGrantAsync(
                _fixture.RequestGrantArgs,
                new GrantCreateBodyWithInteract
                {
                    Subject = new Subject
                    {
                        Sub_ids = [new Sub_ids { Id = "https://example.com/alice", Format = Sub_idsFormat.Uri }],
                    },
                    Interact = new InteractRequest
                    {
                        Start = [Start.Redirect],
                        Finish = new Finish
                        {
                            Method = FinishMethod.Redirect,
                            Uri = new Uri("https://example.com/finish"),
                            Nonce = "n",
                        },
                    },
                }
            );

            captured.Should().ContainSingle();
        }
    }
}

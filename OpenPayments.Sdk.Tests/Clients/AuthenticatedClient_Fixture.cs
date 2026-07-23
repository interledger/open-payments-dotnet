using System.Net;
using System.Text;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NSec.Cryptography;
using Interledger.OpenPayments.Clients;
using Interledger.OpenPayments.Generated.Auth;
using Interledger.OpenPayments.Generated.Resource;
using Amount = Interledger.OpenPayments.Generated.Resource.Amount;

namespace Interledger.OpenPayments.Tests.Clients;

public class AuthenticatedClientFixture
{
    public string BaseUrl => "https://example.com";
    public Uri ClientUrl => new(BaseUrl);

    public RequestArgs RequestGrantArgs => new() { Url = new Uri("https://example.com/auth") };

    public AuthRequestArgs GrantWithTokenArgs =>
        new() { Url = new Uri("https://example.com/auth"), AccessToken = "1234" };

    public GrantCreateBody RequestGrantBody =>
        new()
        {
            AccessToken = new AccessToken()
            {
                Access =
                [
                    new IncomingAccess()
                    {
                        Actions = [Actions.Create, Actions.Read, Actions.List, Actions.Complete],
                    },
                ],
            },
        };

    public GrantContinueBody ContinueGrantBody => new() { InteractRef = "1231323" };

    public AuthResponse ApprovedGrantResponse =>
        new()
        {
            AccessToken = new AccessTokenResponse()
            {
                Access =
                [
                    new IncomingAccess()
                    {
                        Actions = [Actions.Create, Actions.Read, Actions.List, Actions.Complete],
                    },
                ],
            },
            Continue = new AuthContinue()
            {
                AccessToken = new ContinueAccessToken() { Value = "D8616A2FBC790B1CE132" },
                Uri = new Uri(BaseUrl + "/continue/12345"),
            },
        };

    public RotateTokenResponse TokenResponse = new()
    {
        AccessToken = new AccessTokenResponse()
        {
            Access =
            [
                new IncomingAccess()
                {
                    Actions = [Actions.Create, Actions.Read, Actions.List, Actions.Complete],
                },
            ],
        },
    };

    public IncomingPaymentBody CreateIncomingPaymentBody = new()
    {
        WalletAddress = new Uri("https://example.com/wallet/1234"),
        IncomingAmount = new Amount("1000", "EUR", 2),
    };

    public IncomingPaymentResponse CreateIncomingPaymentResponse = new()
    {
        Id = new Uri("https://example.com/incoming/1234"),
        WalletAddress = new Uri("https://example.com/wallet/1234"),
        IncomingAmount = new Amount("100", "EUR", 2),
        ReceivedAmount = new Amount("0", "EUR", 2),
        Completed = false,
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddDays(1),
        Methods =
        [
            new IlpPaymentMethod()
            {
                Type = IlpPaymentMethodType.Ilp,
                IlpAddress = "example.com/incoming/1234",
                SharedSecret = "secret1234",
            },
        ],
    };

    public IncomingPaymentResponse CreateIncomingPaymentResponseWithMetadata = new()
    {
        Id = new Uri("https://example.com/incoming/1234"),
        WalletAddress = new Uri("https://example.com/wallet/1234"),
        IncomingAmount = new Amount("100", "EUR", 2),
        ReceivedAmount = new Amount("0", "EUR", 2),
        Completed = false,
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddDays(1),
        Methods =
        [
            new IlpPaymentMethod()
            {
                Type = IlpPaymentMethodType.Ilp,
                IlpAddress = "example.com/incoming/1234",
                SharedSecret = "secret1234",
            },
        ],
        Metadata = new JObject { ["description"] = "Free Money" },
    };

    public QuoteBody CreateQuoteBody = new()
    {
        WalletAddress = new Uri("https://example.com/wallet/1234"),
        Receiver = new Uri("https://example.com/incoming/1234"),
        Method = PaymentMethod.Ilp,
    };

    public QuoteBodyWithDebitAmount CreateQuoteBodyWithDebitAmount = new()
    {
        WalletAddress = new Uri("https://example.com/wallet/1234"),
        Receiver = new Uri("https://example.com/incoming/1234"),
        DebitAmount = new Amount("100", "EUR"),
        Method = PaymentMethod.Ilp,
    };

    public QuoteBodyWithReceiveAmount CreateQuoteBodyWithReceiveAmount = new()
    {
        WalletAddress = new Uri("https://example.com/wallet/1234"),
        Receiver = new Uri("https://example.com/incoming/1234"),
        ReceiveAmount = new Amount("100", "EUR"),
        Method = PaymentMethod.Ilp,
    };

    public QuoteResponse CreateQuoteResponse = new()
    {
        Id = new Uri("https://example.com/quote/1234"),
        WalletAddress = new Uri("https://example.com/wallet/1234"),
        ReceiveAmount = new Amount("100", "EUR"),
        DebitAmount = new Amount("200", "EUR"),
        Receiver = new Uri("https://example.com/incoming/1234"),
        ExpiresAt = "",
        CreatedAt = DateTime.UtcNow,
        Method = PaymentMethod.Ilp,
    };

    public OutgoingPaymentBodyFromQuote CreateOutgoingPaymentBodyFromQuote = new()
    {
        WalletAddress = new Uri("https://example.com/wallet/1234"),
        QuoteId = new Uri("https://example.com/quote/1234"),
        Metadata = new JObject { ["description"] = "Free Money" },
    };

    public OutgoingPaymentBodyFromIncomingPayment CreateOutgoingPaymentBodyFromIncomingPayment =
        new()
        {
            WalletAddress = new Uri("https://example.com/wallet/1234"),
            IncomingPayment = new Uri("https://example.com/incoming/1234"),
            DebitAmount = new Amount("100", "EUR"),
            Metadata = new JObject { ["description"] = "Free Money" },
        };

    public OutgoingPaymentWithSpentAmountsResponse CreateOutgoingPaymentResponse = new()
    {
        Id = new Uri("https://example.com/outgoing/1234"),
        WalletAddress = new Uri("https://example.com/wallet/1234"),
        QuoteId = new Uri("https://example.com/quote/1234"),
        ReceiveAmount = new Amount("100", "EUR"),
        DebitAmount = new Amount("200", "EUR"),
        SentAmount = new Amount("0", "EUR"),
        Receiver = new Uri("https://example.com/incoming/1234"),
        Failed = false,
        Metadata = new JObject() { ["description"] = "Free Money" },
        CreatedAt = DateTime.UtcNow,
        GrantSpentReceiveAmount = new Amount("0", "EUR"),
        GrantSpentDebitAmount = new Amount("0", "EUR"),
    };

    public OutgoingPaymentResponse GetOutgoingPaymentResponse = new()
    {
        Id = new Uri("https://example.com/outgoing/1234"),
        WalletAddress = new Uri("https://example.com/wallet/1234"),
        QuoteId = new Uri("https://example.com/quote/1234"),
        ReceiveAmount = new Amount("100", "EUR"),
        DebitAmount = new Amount("200", "EUR"),
        SentAmount = new Amount("0", "EUR"),
        Receiver = new Uri("https://example.com/incoming/1234"),
        Failed = false,
        Metadata = new JObject() { ["description"] = "Free Money" },
        CreatedAt = DateTime.UtcNow,
    };

    public ListOutgoingPaymentQuery ListOutgoingPaymentQuery = new()
    {
        WalletAddress = "https://example.com/wallet/1234",
        Cursor = "1234",
        First = 10,
        Last = 10,
    };

    public ListOutgoingPaymentsResponse ListOutgoingPaymentsResponse = new()
    {
        Pagination = new PageInfo
        {
            StartCursor = "1234",
            EndCursor = "1234",
            HasNextPage = false,
            HasPreviousPage = false,
        },
        Result =
        [
            new OutgoingPaymentResponse()
            {
                Id = new Uri("https://example.com/outgoing/1234"),
                WalletAddress = new Uri("https://example.com/wallet/1234"),
                QuoteId = new Uri("https://example.com/quote/1234"),
                ReceiveAmount = new Amount("100", "EUR"),
                DebitAmount = new Amount("200", "EUR"),
                SentAmount = new Amount("0", "EUR"),
                Receiver = new Uri("https://example.com/incoming/1234"),
                Failed = false,
                Metadata = new JObject() { ["description"] = "Free Money" },
                CreatedAt = DateTime.UtcNow,
            },
        ],
    };

    public Interledger.OpenPayments.Generated.Auth.ErrorResponse GrantErrorResponse =>
        new()
        {
            Error = new Interledger.OpenPayments.Generated.Auth.ErrorItem
            {
                Code = Interledger.OpenPayments.Generated.Auth.ErrorItemCode.InvalidRequest,
                Description = "bad grant",
            },
        };

    public HttpClient CreateHttpClientMock(object responseObject, HttpStatusCode code)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(
                new HttpResponseMessage
                {
                    StatusCode = code,
                    Content = new StringContent(
                        JsonConvert.SerializeObject(responseObject),
                        Encoding.UTF8,
                        "application/json"
                    ),
                }
            );

        return new HttpClient(handler.Object) { BaseAddress = new Uri(BaseUrl) };
    }

    public HttpClient CreateHttpClientMock(
        object? responseObject = null,
        HttpStatusCode? code = null
    )
    {
        var statusCode =
            code ?? (responseObject == null ? HttpStatusCode.NoContent : HttpStatusCode.OK);

        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(
                new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content =
                        responseObject == null
                            ? new StringContent("", Encoding.UTF8)
                            : new StringContent(
                                JsonConvert.SerializeObject(responseObject),
                                Encoding.UTF8,
                                "application/json"
                            ),
                }
            );

        return new HttpClient(handler.Object) { BaseAddress = new Uri(BaseUrl) };
    }
}

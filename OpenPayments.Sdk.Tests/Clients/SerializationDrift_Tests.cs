using System.Net;
using System.Text;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Interledger.OpenPayments.Clients;

namespace Interledger.OpenPayments.Tests.Clients;

public class SerializationDrift_Tests
{
    // A resource-server response missing `receivedAmount`, which the generated contract
    // marks Required.Always. The lenient resolver must tolerate this drift instead of
    // failing the whole call.
    private const string DriftedIncomingPaymentJson = """
        {
          "id": "https://example.com/incoming-payments/1234",
          "walletAddress": "https://example.com/alice",
          "completed": false,
          "createdAt": "2026-01-01T00:00:00Z",
          "methods": []
        }
        """;

    private static HttpClient CreateClientReturning(string json)
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
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                }
            );

        return new HttpClient(handler.Object);
    }

    [Fact]
    public async Task GetIncomingPaymentAsync_ResponseMissingRequiredField_StillDeserializes()
    {
        var client = new AuthenticatedClient(
            CreateClientReturning(DriftedIncomingPaymentJson),
            new Uri("https://client.example")
        );

        var result = await client.GetIncomingPaymentAsync(
            new AuthRequestArgs
            {
                Url = new Uri("https://example.com/incoming-payments/1234"),
                AccessToken = "token",
            }
        );

        result.Should().NotBeNull();
        result.Id.Should().Be(new Uri("https://example.com/incoming-payments/1234"));
    }
}

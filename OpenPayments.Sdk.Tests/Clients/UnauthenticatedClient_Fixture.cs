using System.Net;
using System.Text;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using OpenPayments.Sdk.Generated.Resource;
using OpenPayments.Sdk.Generated.Wallet;

namespace OpenPayments.Sdk.Tests.Clients;

public class UnauthenticatedClientFixture
{
    public string BaseUrl => "https://example.com";
    
    public WalletAddress WalletAddress { get; private set; }

    public PublicIncomingPayment IncomingPayment { get; private set; }

    public JsonWebKeySet WalletAddressKeys { get; private set; }

    public UnauthenticatedClientFixture()
    {
        WalletAddress = new WalletAddress()
        {
            Id = new Uri(BaseUrl),
            PublicName = "Wallet Address",
            AssetScale = 2,
            AssetCode = "EUR",
            AuthServer = new Uri(BaseUrl + "/auth"),
            ResourceServer = new Uri(BaseUrl + "/ilp")
        };

        IncomingPayment = new PublicIncomingPayment
        {
            ReceivedAmount = new Amount()
            {
                AssetCode = "EUR",
                AssetScale = 2,
                Value = "100"
            },
            AuthServer = new Uri(BaseUrl + "/auth")
        };

        WalletAddressKeys = new JsonWebKeySet()
        {
            Keys = {
                new JsonWebKey {
                    Kty = JsonWebKeyKty.OKP,
                    Crv = JsonWebKeyCrv.Ed25519,
                    Kid = "test-kid",
                    Alg = JsonWebKeyAlg.EdDSA,
                    X = "public-key"
                }
            }
        };
    }

    public HttpClient CreateHttpClientMock(object responseObject)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(responseObject), Encoding.UTF8, "application/json")
            });

        return new HttpClient(handler.Object)
        {
            BaseAddress = new Uri(BaseUrl)
        };
    }
}

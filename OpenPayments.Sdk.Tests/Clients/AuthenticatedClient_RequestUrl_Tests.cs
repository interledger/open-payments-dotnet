using FluentAssertions;
using OpenPayments.Sdk.Clients;

namespace OpenPayments.Sdk.Tests.Clients;

[Collection("AuthenticatedClient")]
public class AuthenticatedClient_RequestUrl_Tests(AuthenticatedClientFixture fixture)
{
    private readonly AuthenticatedClientFixture _fixture = fixture;

    private AuthenticatedClient CreateClient(HttpClient http) =>
        new(http, _fixture.PrivateKey, _fixture.KeyId, _fixture.ClientUrl);

    [Fact]
    public async Task RotateTokenAsync_RequestsTheTokenUrlVerbatim()
    {
        var (http, handler) = _fixture.CreateRecordingHttpClient(_fixture.TokenResponse);
        var client = CreateClient(http);

        await client.RotateTokenAsync(
            new AuthRequestArgs
            {
                Url = new Uri("https://auth-a.example/token/abc"),
                AccessToken = "token",
            }
        );

        handler.LastRequestUri.AbsoluteUri.Should().Be("https://auth-a.example/token/abc");
    }
}

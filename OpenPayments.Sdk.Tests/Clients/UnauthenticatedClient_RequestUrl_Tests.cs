using FluentAssertions;
using OpenPayments.Sdk.Clients;

namespace OpenPayments.Sdk.Tests.Clients;

[Collection("UnauthenticatedClient")]
public class UnauthenticatedClient_RequestUrl_Tests(UnauthenticatedClientFixture fixture)
{
    private readonly UnauthenticatedClientFixture _fixture = fixture;

    [Fact]
    public async Task GetWalletAddressKeysAsync_AppendsJwksJsonWithASlash()
    {
        var (http, handler) = _fixture.CreateRecordingHttpClient(_fixture.WalletAddressKeys);
        var client = new UnauthenticatedClient(http);

        await client.GetWalletAddressKeysAsync("https://example.com/alice");

        handler.LastRequestUri.AbsoluteUri.Should().Be("https://example.com/alice/jwks.json");
    }

    [Fact]
    public async Task GetWalletAddressKeysAsync_DoesNotDoubleSlashWhenWalletAddressHasTrailingSlash()
    {
        var (http, handler) = _fixture.CreateRecordingHttpClient(_fixture.WalletAddressKeys);
        var client = new UnauthenticatedClient(http);

        await client.GetWalletAddressKeysAsync("https://example.com/");

        handler.LastRequestUri.AbsoluteUri.Should().Be("https://example.com/jwks.json");
    }
}

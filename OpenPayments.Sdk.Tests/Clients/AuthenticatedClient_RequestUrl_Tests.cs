using System.Net;
using FluentAssertions;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Generated.Resource;

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

    [Fact]
    public async Task CreateIncomingPaymentAsync_AppendsIncomingPaymentsToTheResourceServer()
    {
        var (http, handler) = _fixture.CreateRecordingHttpClient(
            _fixture.CreateIncomingPaymentResponse,
            HttpStatusCode.Created
        );
        var client = CreateClient(http);

        await client.CreateIncomingPaymentAsync(
            new AuthRequestArgs
            {
                Url = new Uri("https://host-a.example"),
                AccessToken = "token",
            },
            _fixture.CreateIncomingPaymentBody
        );

        handler
            .LastRequestUri.AbsoluteUri.Should()
            .Be("https://host-a.example/incoming-payments");
    }

    [Fact]
    public async Task CreateIncomingPaymentAsync_DoesNotDoubleSlashWhenResourceServerHasTrailingSlash()
    {
        var (http, handler) = _fixture.CreateRecordingHttpClient(
            _fixture.CreateIncomingPaymentResponse,
            HttpStatusCode.Created
        );
        var client = CreateClient(http);

        await client.CreateIncomingPaymentAsync(
            new AuthRequestArgs
            {
                Url = new Uri("https://host-a.example/resource/"),
                AccessToken = "token",
            },
            _fixture.CreateIncomingPaymentBody
        );

        handler
            .LastRequestUri.AbsoluteUri.Should()
            .Be("https://host-a.example/resource/incoming-payments");
    }

    [Fact]
    public async Task GetIncomingPaymentAsync_RequestsTheResourceUrlVerbatim()
    {
        var (http, handler) = _fixture.CreateRecordingHttpClient(
            _fixture.CreateIncomingPaymentResponse
        );
        var client = CreateClient(http);

        await client.GetIncomingPaymentAsync(
            new AuthRequestArgs
            {
                Url = new Uri("https://host-a.example/incoming-payments/1"),
                AccessToken = "token",
            }
        );

        handler
            .LastRequestUri.AbsoluteUri.Should()
            .Be("https://host-a.example/incoming-payments/1");
    }

    [Fact]
    public async Task CompleteIncomingPaymentsAsync_AppendsASingleCompleteSegment()
    {
        var (http, handler) = _fixture.CreateRecordingHttpClient(
            _fixture.CreateIncomingPaymentResponse
        );
        var client = CreateClient(http);

        await client.CompleteIncomingPaymentsAsync(
            new AuthRequestArgs
            {
                Url = new Uri("https://host-a.example/incoming-payments/1"),
                AccessToken = "token",
            }
        );

        handler
            .LastRequestUri.AbsoluteUri.Should()
            .Be("https://host-a.example/incoming-payments/1/complete");
    }

    [Fact]
    public async Task ListIncomingPaymentsAsync_AppendsIncomingPaymentsToTheResourceServer()
    {
        var (http, handler) = _fixture.CreateRecordingHttpClient(
            _fixture.ListIncomingPaymentsResponse
        );
        var client = CreateClient(http);

        await client.ListIncomingPaymentsAsync(
            new AuthRequestArgs
            {
                Url = new Uri("https://host-a.example"),
                AccessToken = "token",
            },
            new ListIncomingPaymentQuery { WalletAddress = "https://host-a.example/wallet/1" }
        );

        handler
            .LastRequestUri.GetLeftPart(UriPartial.Path)
            .Should()
            .Be("https://host-a.example/incoming-payments");
    }

    [Fact]
    public async Task CreateQuoteAsync_AppendsQuotesToTheResourceServer()
    {
        var (http, handler) = _fixture.CreateRecordingHttpClient(
            _fixture.CreateQuoteResponse,
            HttpStatusCode.Created
        );
        var client = CreateClient(http);

        await client.CreateQuoteAsync(
            new AuthRequestArgs
            {
                Url = new Uri("https://host-a.example"),
                AccessToken = "token",
            },
            _fixture.CreateQuoteBody
        );

        handler.LastRequestUri.AbsoluteUri.Should().Be("https://host-a.example/quotes");
    }

    [Fact]
    public async Task GetQuoteAsync_RequestsTheResourceUrlVerbatim()
    {
        var (http, handler) = _fixture.CreateRecordingHttpClient(_fixture.CreateQuoteResponse);
        var client = CreateClient(http);

        await client.GetQuoteAsync(
            new AuthRequestArgs
            {
                Url = new Uri("https://host-a.example/quotes/1"),
                AccessToken = "token",
            }
        );

        handler.LastRequestUri.AbsoluteUri.Should().Be("https://host-a.example/quotes/1");
    }

    [Fact]
    public async Task CreateOutgoingPaymentAsync_AppendsOutgoingPaymentsToTheResourceServer()
    {
        var (http, handler) = _fixture.CreateRecordingHttpClient(
            _fixture.CreateOutgoingPaymentResponse,
            HttpStatusCode.Created
        );
        var client = CreateClient(http);

        await client.CreateOutgoingPaymentAsync(
            new AuthRequestArgs
            {
                Url = new Uri("https://host-a.example"),
                AccessToken = "token",
            },
            _fixture.CreateOutgoingPaymentBodyFromQuote
        );

        handler
            .LastRequestUri.AbsoluteUri.Should()
            .Be("https://host-a.example/outgoing-payments");
    }

    [Fact]
    public async Task GetOutgoingPaymentAsync_RequestsTheResourceUrlVerbatim()
    {
        var (http, handler) = _fixture.CreateRecordingHttpClient(
            _fixture.GetOutgoingPaymentResponse
        );
        var client = CreateClient(http);

        await client.GetOutgoingPaymentAsync(
            new AuthRequestArgs
            {
                Url = new Uri("https://host-a.example/outgoing-payments/1"),
                AccessToken = "token",
            }
        );

        handler
            .LastRequestUri.AbsoluteUri.Should()
            .Be("https://host-a.example/outgoing-payments/1");
    }

    [Fact]
    public async Task ListOutgoingPaymentsAsync_AppendsOutgoingPaymentsToTheResourceServer()
    {
        var (http, handler) = _fixture.CreateRecordingHttpClient(
            _fixture.ListOutgoingPaymentsResponse
        );
        var client = CreateClient(http);

        await client.ListOutgoingPaymentsAsync(
            new AuthRequestArgs
            {
                Url = new Uri("https://host-a.example"),
                AccessToken = "token",
            },
            new ListOutgoingPaymentQuery { WalletAddress = "https://host-a.example/wallet/1" }
        );

        handler
            .LastRequestUri.GetLeftPart(UriPartial.Path)
            .Should()
            .Be("https://host-a.example/outgoing-payments");
    }

    [Fact]
    public async Task RequestGrantAsync_RequestsThePathfulAuthServerUrlVerbatim()
    {
        var (http, handler) = _fixture.CreateRecordingHttpClient(_fixture.ApprovedGrantResponse);
        var client = CreateClient(http);

        await client.RequestGrantAsync(
            new RequestArgs { Url = new Uri("https://auth-a.example/gnap") },
            _fixture.RequestGrantBody
        );

        handler.LastRequestUri.AbsoluteUri.Should().Be("https://auth-a.example/gnap");
    }

    [Fact]
    public async Task GetIncomingPaymentAsync_SequentialCallsAcrossHosts_EachReachesItsOwnHost()
    {
        var (http, handler) = _fixture.CreateRecordingHttpClient(
            _fixture.CreateIncomingPaymentResponse
        );
        var client = CreateClient(http);

        await client.GetIncomingPaymentAsync(
            new AuthRequestArgs
            {
                Url = new Uri("https://host-a.example/incoming-payments/1"),
                AccessToken = "token",
            }
        );
        await client.GetIncomingPaymentAsync(
            new AuthRequestArgs
            {
                Url = new Uri("https://host-b.example/incoming-payments/2"),
                AccessToken = "token",
            }
        );

        handler
            .RequestUris.Select(u => u.AbsoluteUri)
            .Should()
            .Equal(
                "https://host-a.example/incoming-payments/1",
                "https://host-b.example/incoming-payments/2"
            );
    }

    [Fact]
    public async Task GetIncomingPaymentAsync_ParallelCallsAcrossHosts_EachReachesItsOwnHost()
    {
        var (http, handler) = _fixture.CreateRecordingHttpClient(
            _fixture.CreateIncomingPaymentResponse
        );
        var client = CreateClient(http);

        var targets = Enumerable
            .Range(0, 200)
            .Select(i => new Uri($"https://host-{i % 2}.example/incoming-payments/{i}"))
            .ToArray();

        await Parallel.ForEachAsync(
            targets,
            async (target, ct) =>
            {
                await client.GetIncomingPaymentAsync(
                    new AuthRequestArgs { Url = target, AccessToken = "token" },
                    ct
                );
            }
        );

        handler
            .RequestUris.Select(u => u.AbsoluteUri)
            .Should()
            .BeEquivalentTo(targets.Select(t => t.AbsoluteUri));
    }

    [Fact]
    public async Task RequestGrantAsync_ParallelCallsAcrossAuthServers_EachReachesItsOwnHost()
    {
        var (http, handler) = _fixture.CreateRecordingHttpClient(_fixture.ApprovedGrantResponse);
        var client = CreateClient(http);

        var targets = Enumerable
            .Range(0, 200)
            .Select(i => new Uri($"https://auth-{i % 2}.example/gnap/{i}"))
            .ToArray();

        await Parallel.ForEachAsync(
            targets,
            async (target, ct) =>
            {
                await client.RequestGrantAsync(
                    new RequestArgs { Url = target },
                    _fixture.RequestGrantBody,
                    ct
                );
            }
        );

        handler
            .RequestUris.Select(u => u.AbsoluteUri)
            .Should()
            .BeEquivalentTo(targets.Select(t => t.AbsoluteUri));
    }
}

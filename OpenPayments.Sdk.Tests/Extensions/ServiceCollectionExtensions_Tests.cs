using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using NSec.Cryptography;
using Interledger.OpenPayments.Clients;
using Interledger.OpenPayments.Extensions;
using Interledger.OpenPayments.Generated.Auth;

namespace Interledger.OpenPayments.Tests.Extensions;

public class ServiceCollectionExtensions_Tests
{
    [Fact]
    public void UseOpenPayments_WithUseUnauthenticatedClient_RegistersServices()
    {
        var services = new ServiceCollection();

        services.UseOpenPayments(options => { options.UseUnauthenticatedClient = true; });
        var provider = services.BuildServiceProvider();

        var client = provider.GetService<IUnauthenticatedClient>();
        Assert.NotNull(client);
        Assert.IsType<UnauthenticatedClient>(client);
    }

    [Fact]
    public void UseOpenPayments_WithoutUseUnauthenticatedClient_DoesNotRegisterClient()
    {
        var services = new ServiceCollection();

        services.UseOpenPayments(_ => { });
        var provider = services.BuildServiceProvider();

        var client = provider.GetService<IUnauthenticatedClient>();
        Assert.Null(client);
    }

    [Fact]
    public void UseOpenPayments_WithUseAuthenticatedClient_RegistersServices()
    {
        var services = new ServiceCollection();

        services.UseOpenPayments(options =>
        {
            options.UseAuthenticatedClient = true;
            options.ClientUrl = new Uri("https://example.com");
            options.KeyId = "1234";
            options.PrivateKey = Key.Create(SignatureAlgorithm.Ed25519);
        });
        var provider = services.BuildServiceProvider();

        var client = provider.GetService<IAuthenticatedClient>();
        Assert.NotNull(client);
        Assert.IsType<AuthenticatedClient>(client);
    }

    [Fact]
    public void UseOpenPayments_AuthenticatedClient_WithoutAnyOptions_ThrowsImmediately()
    {
        var services = new ServiceCollection();

        var act = () => services.UseOpenPayments(options => { options.UseAuthenticatedClient = true; });

        act.Should().Throw<InvalidOperationException>().WithMessage("*KeyId*");
    }

    [Fact]
    public void UseOpenPayments_AuthenticatedClient_MissingPrivateKey_ThrowsImmediately()
    {
        var services = new ServiceCollection();

        var act = () =>
            services.UseOpenPayments(options =>
            {
                options.UseAuthenticatedClient = true;
                options.KeyId = "1234";
                options.ClientUrl = new Uri("https://example.com");
            });

        act.Should().Throw<InvalidOperationException>().WithMessage("*PrivateKey*");
    }

    [Fact]
    public void UseOpenPayments_AuthenticatedClient_MissingClientUrl_ThrowsImmediately()
    {
        var services = new ServiceCollection();

        var act = () =>
            services.UseOpenPayments(options =>
            {
                options.UseAuthenticatedClient = true;
                options.KeyId = "1234";
                options.PrivateKey = Key.Create(SignatureAlgorithm.Ed25519);
            });

        act.Should().Throw<InvalidOperationException>().WithMessage("*ClientUrl*");
    }

    [Fact]
    public void UseOpenPayments_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Action act = () => services.UseOpenPayments(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UseOpenPayments_WithoutUseAuthenticatedClient_DoesNotRegistersService()
    {
        var services = new ServiceCollection();

        services.UseOpenPayments(options => { options.UseAuthenticatedClient = false; });
        var provider = services.BuildServiceProvider();

        var client = provider.GetService<IAuthenticatedClient>();
        Assert.Null(client);
    }

    [Fact]
    public void UseOpenPayments_Always_RegistersHttpClient()
    {
        var services = new ServiceCollection();

        services.UseOpenPayments(_ => { });
        var provider = services.BuildServiceProvider();

        var factory = provider.GetService<IHttpClientFactory>();
        Assert.NotNull(factory);
    }

    [Fact]
    public async Task UseOpenPayments_AuthenticatedClient_SignsOutgoingRequests()
    {
        var services = new ServiceCollection();
        var privateKey = Key.Create(SignatureAlgorithm.Ed25519);

        services.UseOpenPayments(options =>
        {
            options.UseAuthenticatedClient = true;
            options.ClientUrl = new Uri("https://example.com");
            options.KeyId = "1234";
            options.PrivateKey = privateKey;
        });

        HttpRequestMessage? captured = null;
        var testHandler = new Mock<HttpMessageHandler>();
        testHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns<HttpRequestMessage, CancellationToken>(
                (request, _) =>
                {
                    captured = request;
                    return Task.FromResult(
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(
                                "{\"access_token\":{\"access\":[]}}",
                                Encoding.UTF8,
                                "application/json"
                            ),
                        }
                    );
                }
            );

        // Substitute the "authenticated" client's primary (innermost) handler with a test
        // double so this test never touches the network. Calling AddHttpClient("authenticated")
        // again after UseOpenPayments does not replace its registration — it appends this
        // handler configuration on top, the same way a consuming application would override
        // the primary handler for testing.
        services
            .AddHttpClient("authenticated")
            .ConfigurePrimaryHttpMessageHandler(() => testHandler.Object);

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IAuthenticatedClient>();

        await client.RequestGrantAsync(
            new RequestArgs { Url = new Uri("https://example.com/auth") },
            new GrantCreateBody
            {
                AccessToken = new AccessToken
                {
                    Access = [new IncomingAccess { Actions = [Actions.Read] }],
                },
            }
        );

        captured.Should().NotBeNull();
        captured!.Headers.Should().Contain(h => h.Key == "Signature");
        captured.Headers.Should().Contain(h => h.Key == "Signature-Input");
    }
}

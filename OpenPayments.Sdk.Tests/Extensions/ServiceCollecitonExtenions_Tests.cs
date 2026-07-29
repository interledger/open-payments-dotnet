using Microsoft.Extensions.DependencyInjection;
using NSec.Cryptography;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Extensions;

namespace OpenPayments.Sdk.Tests.Extensions;

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
    public void UseOpenPayments_AuthenticatedClient_WithoutOptions_ThrowsAtRegistration()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() =>
            services.UseOpenPayments(options => { options.UseAuthenticatedClient = true; })
        );
    }

    [Fact]
    public void UseOpenPayments_AuthenticatedClient_WithoutPrivateKey_Throws()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.UseOpenPayments(options =>
            {
                options.UseAuthenticatedClient = true;
                options.ClientUrl = new Uri("https://example.com");
                options.KeyId = "1234";
            })
        );

        Assert.Contains("PrivateKey", ex.Message);
    }

    [Fact]
    public void UseOpenPayments_AuthenticatedClient_WithoutKeyId_Throws()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.UseOpenPayments(options =>
            {
                options.UseAuthenticatedClient = true;
                options.ClientUrl = new Uri("https://example.com");
                options.PrivateKey = Key.Create(SignatureAlgorithm.Ed25519);
            })
        );

        Assert.Contains("KeyId", ex.Message);
    }

    [Fact]
    public void UseOpenPayments_AuthenticatedClient_WithoutClientUrl_Throws()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.UseOpenPayments(options =>
            {
                options.UseAuthenticatedClient = true;
                options.KeyId = "1234";
                options.PrivateKey = Key.Create(SignatureAlgorithm.Ed25519);
            })
        );

        Assert.Contains("ClientUrl", ex.Message);
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
}

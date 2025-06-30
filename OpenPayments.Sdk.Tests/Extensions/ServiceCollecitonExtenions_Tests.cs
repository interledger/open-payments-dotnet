using Microsoft.Extensions.DependencyInjection;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Extensions;

namespace OpenPayments.Sdk.Tests.Extensions;

public class ServiceCollectionExtensions_Tests
{
    [Fact]
    public void UseOpenPayments_WithUseUnauthenticatedClient_RegistersServices()
    {
        var services = new ServiceCollection();

        services.UseOpenPayments(options => options.UseUnauthenticatedClient());
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
    public void UseOpenPayments_Always_RegistersHttpClient()
    {
        var services = new ServiceCollection();

        services.UseOpenPayments(_ => { });
        var provider = services.BuildServiceProvider();

        var factory = provider.GetService<IHttpClientFactory>();
        Assert.NotNull(factory);
    }
}
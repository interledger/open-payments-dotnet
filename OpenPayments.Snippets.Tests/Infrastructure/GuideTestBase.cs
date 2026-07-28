using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NSec.Cryptography;

namespace OpenPayments.Snippets.Tests.Infrastructure;

/// <summary>
/// Base for guide end-to-end tests. xunit creates a new instance of the test class per
/// <c>[Fact]</c>, so the fresh <see cref="FakeOpenPaymentsServer"/> and
/// <see cref="IAuthenticatedClient"/> built in this constructor are never shared across tests
/// — there is no <c>ICollectionFixture</c>/<c>IClassFixture</c> involved.
/// </summary>
public abstract class GuideTestBase : IDisposable
{
    private readonly ServiceProvider _provider;

    protected FakeOpenPaymentsServer Server { get; }
    protected IAuthenticatedClient Client { get; }

    protected GuideTestBase()
    {
        Server = new FakeOpenPaymentsServer();

        var services = new ServiceCollection();
        services.UseOpenPayments(options =>
        {
            options.UseAuthenticatedClient = true;
            options.KeyId = "guide-test-key";
            options.PrivateKey = Key.Create(SignatureAlgorithm.Ed25519);
            options.ClientUrl = new Uri("https://client.example.com/test");
        });

        // Appends to (does not replace) the "authenticated" client UseOpenPayments already
        // registered: SigningHttpMessageHandler stays outermost, this routing handler sits
        // just inside it, and the primary handler is swapped for the fake TestServer's.
        services
            .AddHttpClient("authenticated")
            .AddHttpMessageHandler(() => new TestServerRoutingHandler(Server.BaseAddress))
            .ConfigurePrimaryHttpMessageHandler(() => Server.CreateHandler());

        _provider = services.BuildServiceProvider();
        Client = _provider.GetRequiredService<IAuthenticatedClient>();
    }

    public void Dispose()
    {
        _provider.Dispose();
        Server.Dispose();
    }
}

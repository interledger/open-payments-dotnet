using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSec.Cryptography;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Configuration;
using OpenPayments.Sdk.Exceptions;
using OpenPayments.Sdk.Extensions;
using OpenPayments.Sdk.Generated.Auth;
using OpenPayments.Sdk.HttpSignatureUtils;

namespace OpenPayments.Sdk.Tests.Extensions;

public class ServiceCollectionExtensions_Tests : IDisposable
{
    /// <summary>
    /// Temp directories created by <see cref="CreateKeyPemFile"/>, deleted in <see cref="Dispose"/>
    /// so test key material does not accumulate on disk across runs.
    /// </summary>
    private readonly List<string> _tempKeyDirectories = [];

    /// <summary>
    /// Captures the request as the inner handler sees it, so tests can inspect the headers
    /// that actually reach the end of the pipeline for a given named client.
    /// </summary>
    private sealed class SpyHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    /// <summary>
    /// Writes a fresh Ed25519 key to a PEM file in a new temporary directory and returns its path.
    /// Every call gets its own directory so tests never share key material.
    /// </summary>
    private string CreateKeyPemFile()
    {
        var dir = Directory.CreateTempSubdirectory("op-sdk-tests");
        _tempKeyDirectories.Add(dir.FullName);
        using var key = KeyUtils.GenerateKey(
            new GenerateKeyArgs { Dir = dir.FullName, Filename = "key.pem" }
        );
        return Path.Combine(dir.FullName, "key.pem");
    }

    /// <summary>
    /// Returns the PEM text of a fresh Ed25519 key.
    /// </summary>
    private string CreateKeyPem() => File.ReadAllText(CreateKeyPemFile());

    /// <summary>
    /// Deletes every temp directory created by <see cref="CreateKeyPemFile"/> during this test
    /// instance's run.
    /// </summary>
    public void Dispose()
    {
        foreach (var dir in _tempKeyDirectories)
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

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

    [Fact]
    public async Task UseOpenPayments_SignedNamedClient_AddsSignatureHeaders()
    {
        var services = new ServiceCollection();
        services.UseOpenPayments(options =>
        {
            options.UseAuthenticatedClient = true;
            options.ClientUrl = new Uri("https://example.com");
            options.KeyId = "1234";
            options.PrivateKey = Key.Create(SignatureAlgorithm.Ed25519);
        });

        var spy = new SpyHandler();
        services
            .AddHttpClient(OpenPaymentsHttpClients.Signed)
            .ConfigurePrimaryHttpMessageHandler(() => spy);

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(OpenPaymentsHttpClients.Signed);

        await client.GetAsync("https://example.com/resource");

        Assert.NotNull(spy.Request);
        Assert.True(spy.Request!.Headers.Contains("Signature"));
        Assert.True(spy.Request.Headers.Contains("Signature-Input"));
    }

    [Fact]
    public async Task UseOpenPayments_UnsignedNamedClient_DoesNotAddSignatureHeaders()
    {
        var services = new ServiceCollection();
        services.UseOpenPayments(options =>
        {
            options.UseAuthenticatedClient = true;
            options.ClientUrl = new Uri("https://example.com");
            options.KeyId = "1234";
            options.PrivateKey = Key.Create(SignatureAlgorithm.Ed25519);
        });

        var spy = new SpyHandler();
        services
            .AddHttpClient(OpenPaymentsHttpClients.Unsigned)
            .ConfigurePrimaryHttpMessageHandler(() => spy);

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(OpenPaymentsHttpClients.Unsigned);

        await client.GetAsync("https://example.com/wallet-address");

        Assert.NotNull(spy.Request);
        Assert.False(spy.Request!.Headers.Contains("Signature"));
        Assert.False(spy.Request.Headers.Contains("Signature-Input"));
    }

    [Fact]
    public void UseOpenPayments_WithPrivateKeyPem_RegistersAuthenticatedClient()
    {
        var services = new ServiceCollection();

        services.UseOpenPayments(options =>
        {
            options.UseAuthenticatedClient = true;
            options.ClientUrl = new Uri("https://example.com");
            options.KeyId = "1234";
            options.PrivateKeyPem = CreateKeyPem();
        });
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IAuthenticatedClient>());
    }

    [Fact]
    public void UseOpenPayments_WithPrivateKeyPath_RegistersAuthenticatedClient()
    {
        var services = new ServiceCollection();

        services.UseOpenPayments(options =>
        {
            options.UseAuthenticatedClient = true;
            options.ClientUrl = new Uri("https://example.com");
            options.KeyId = "1234";
            options.PrivateKeyPath = CreateKeyPemFile();
        });
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IAuthenticatedClient>());
    }

    [Fact]
    public void UseOpenPayments_WithTwoKeySources_ThrowsNamingBoth()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.UseOpenPayments(options =>
            {
                options.UseAuthenticatedClient = true;
                options.ClientUrl = new Uri("https://example.com");
                options.KeyId = "1234";
                options.PrivateKeyPem = CreateKeyPem();
                options.PrivateKeyPath = CreateKeyPemFile();
            })
        );

        Assert.Contains("PrivateKeyPem", ex.Message);
        Assert.Contains("PrivateKeyPath", ex.Message);
    }

    [Fact]
    public void UseOpenPayments_WithNoKeySource_ThrowsNamingAllThree()
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

        Assert.Contains("PrivateKey,", ex.Message);
        Assert.Contains("PrivateKeyPem", ex.Message);
        Assert.Contains("PrivateKeyPath", ex.Message);
    }

    [Fact]
    public void UseOpenPayments_UnauthenticatedClient_IsRegisteredTransient()
    {
        var services = new ServiceCollection();

        services.UseOpenPayments(options => options.UseUnauthenticatedClient = true);

        var descriptor = Assert.Single(
            services,
            d => d.ServiceType == typeof(IUnauthenticatedClient)
        );
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void UseOpenPayments_AuthenticatedClient_IsRegisteredTransient()
    {
        var services = new ServiceCollection();

        services.UseOpenPayments(options =>
        {
            options.UseAuthenticatedClient = true;
            options.ClientUrl = new Uri("https://example.com");
            options.KeyId = "1234";
            options.PrivateKey = Key.Create(SignatureAlgorithm.Ed25519);
        });

        var descriptor = Assert.Single(
            services,
            d => d.ServiceType == typeof(IAuthenticatedClient)
        );
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void UseOpenPayments_ResolvingAuthenticatedClientTwice_ReturnsDistinctInstances()
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

        var first = provider.GetRequiredService<IAuthenticatedClient>();
        var second = provider.GetRequiredService<IAuthenticatedClient>();

        Assert.NotSame(first, second);
    }

    [Fact]
    public void UseOpenPayments_ResolvingUnauthenticatedClientTwice_ReturnsDistinctInstances()
    {
        var services = new ServiceCollection();

        services.UseOpenPayments(options => options.UseUnauthenticatedClient = true);
        var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<IUnauthenticatedClient>();
        var second = provider.GetRequiredService<IUnauthenticatedClient>();

        Assert.NotSame(first, second);
    }

    [Fact]
    public void UseOpenPayments_WithNullConfigureDelegate_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.UseOpenPayments((Action<OpenPaymentsOptions>)null!)
        );
    }

    [Fact]
    public void UseOpenPayments_WithNullConfigurationSection_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.UseOpenPayments((IConfigurationSection)null!)
        );
    }

    [Fact]
    public void UseOpenPayments_BoundFromConfigurationWithPem_RegistersAuthenticatedClient()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["OpenPayments:UseAuthenticatedClient"] = "true",
                    ["OpenPayments:KeyId"] = "1234",
                    ["OpenPayments:ClientUrl"] = "https://example.com",
                    ["OpenPayments:PrivateKeyPem"] = CreateKeyPem(),
                }
            )
            .Build();
        var services = new ServiceCollection();

        services.UseOpenPayments(configuration.GetSection("OpenPayments"));
        var provider = services.BuildServiceProvider();

        var client = provider.GetService<IAuthenticatedClient>();
        Assert.NotNull(client);
        Assert.IsType<AuthenticatedClient>(client);
    }

    [Fact]
    public void UseOpenPayments_BoundFromConfigurationWithKeyPath_RegistersAuthenticatedClient()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["OpenPayments:UseAuthenticatedClient"] = "true",
                    ["OpenPayments:KeyId"] = "1234",
                    ["OpenPayments:ClientUrl"] = "https://example.com",
                    ["OpenPayments:PrivateKeyPath"] = CreateKeyPemFile(),
                }
            )
            .Build();
        var services = new ServiceCollection();

        services.UseOpenPayments(configuration.GetSection("OpenPayments"));
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IAuthenticatedClient>());
    }

    [Fact]
    public void UseOpenPayments_BoundFromConfigurationWithUnauthenticatedOnly_RegistersClient()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["OpenPayments:UseUnauthenticatedClient"] = "true",
                }
            )
            .Build();
        var services = new ServiceCollection();

        services.UseOpenPayments(configuration.GetSection("OpenPayments"));
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IUnauthenticatedClient>());
    }

    [Fact]
    public void UseOpenPayments_BoundFromConfigurationWithoutKeyId_ThrowsAtRegistration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["OpenPayments:UseAuthenticatedClient"] = "true",
                    ["OpenPayments:ClientUrl"] = "https://example.com",
                    ["OpenPayments:PrivateKeyPem"] = CreateKeyPem(),
                }
            )
            .Build();
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.UseOpenPayments(configuration.GetSection("OpenPayments"))
        );

        Assert.Contains("KeyId", ex.Message);
    }

    [Fact]
    public void UseOpenPayments_BoundFromConfigurationWithRelativeClientUrl_ThrowsAtRegistration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["OpenPayments:UseAuthenticatedClient"] = "true",
                    ["OpenPayments:KeyId"] = "1234",
                    // Missing the scheme. ConfigurationBinder's UriTypeConverter parses this as a
                    // valid *relative* Uri rather than failing to bind, so the guard in
                    // AddOpenPaymentsCore has to reject it explicitly.
                    ["OpenPayments:ClientUrl"] = "wallet.example",
                    ["OpenPayments:PrivateKeyPem"] = CreateKeyPem(),
                }
            )
            .Build();
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.UseOpenPayments(configuration.GetSection("OpenPayments"))
        );

        Assert.Contains("ClientUrl", ex.Message);
    }

    [Fact]
    public void UseOpenPayments_WithRelativeClientUrl_Throws()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.UseOpenPayments(options =>
            {
                options.UseAuthenticatedClient = true;
                options.ClientUrl = new Uri("wallet.example", UriKind.Relative);
                options.KeyId = "1234";
                options.PrivateKey = Key.Create(SignatureAlgorithm.Ed25519);
            })
        );

        Assert.Contains("ClientUrl", ex.Message);
    }

    [Fact]
    public void UseOpenPayments_MissingConfigurationSection_ThrowsAtRegistration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["SomeOtherSection:Foo"] = "bar" }
            )
            .Build();
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.UseOpenPayments(configuration.GetSection("SomeMissingSection"))
        );

        Assert.Contains("SomeMissingSection", ex.Message);
    }

    [Fact]
    public async Task UseOpenPayments_ResolvedAuthenticatedClient_SignsOnlySignedPipeline()
    {
        var services = new ServiceCollection();
        services.UseOpenPayments(options =>
        {
            options.UseAuthenticatedClient = true;
            options.ClientUrl = new Uri("https://example.com");
            options.KeyId = "1234";
            options.PrivateKey = Key.Create(SignatureAlgorithm.Ed25519);
        });

        // Registered after UseOpenPayments, same pattern as the named-client signature tests:
        // this swaps only the primary (innermost) handler, so the signing handler that
        // UseOpenPayments already wired onto the signed pipeline is still in the chain.
        var signedSpy = new SpyHandler();
        services
            .AddHttpClient(OpenPaymentsHttpClients.Signed)
            .ConfigurePrimaryHttpMessageHandler(() => signedSpy);

        var unsignedSpy = new SpyHandler();
        services
            .AddHttpClient(OpenPaymentsHttpClients.Unsigned)
            .ConfigurePrimaryHttpMessageHandler(() => unsignedSpy);

        var provider = services.BuildServiceProvider();

        // Resolved from the container, not built via IHttpClientFactory.CreateClient directly, so
        // this exercises the same instance a consuming application would inject.
        var client = provider.GetRequiredService<IAuthenticatedClient>();

        // Drives the unsigned pipeline: GetWalletAddressAsync is inherited from
        // UnauthenticatedClient, which AuthenticatedClient wraps around the unsigned HttpClient.
        // The spy returns an empty 200 response, so deserializing it into a WalletAddress fails;
        // that's expected and irrelevant here, only the request the spy captured matters.
        await Assert.ThrowsAsync<OpenPaymentsApiException>(
            () => client.GetWalletAddressAsync("https://example.com/alice")
        );

        // Drives the signed pipeline via the auth-server client, which is built over the signed
        // HttpClient. Same story: the spy's empty response fails to deserialize as AuthResponse.
        await Assert.ThrowsAsync<OpenPaymentsApiException>(
            () =>
                client.RequestGrantAsync(
                    new RequestArgs { Url = new Uri("https://example.com/grant") },
                    new GrantCreateBody()
                )
        );

        Assert.NotNull(unsignedSpy.Request);
        Assert.False(unsignedSpy.Request!.Headers.Contains("Signature"));
        Assert.False(unsignedSpy.Request.Headers.Contains("Signature-Input"));

        Assert.NotNull(signedSpy.Request);
        Assert.True(signedSpy.Request!.Headers.Contains("Signature"));
        Assert.True(signedSpy.Request.Headers.Contains("Signature-Input"));
    }
}

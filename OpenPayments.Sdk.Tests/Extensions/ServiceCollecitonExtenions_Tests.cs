using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSec.Cryptography;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Configuration;
using OpenPayments.Sdk.Extensions;
using OpenPayments.Sdk.HttpSignatureUtils;

namespace OpenPayments.Sdk.Tests.Extensions;

public class ServiceCollectionExtensions_Tests
{
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
    private static string CreateKeyPemFile()
    {
        var dir = Directory.CreateTempSubdirectory("op-sdk-tests");
        using var key = KeyUtils.GenerateKey(
            new GenerateKeyArgs { Dir = dir.FullName, Filename = "key.pem" }
        );
        return Path.Combine(dir.FullName, "key.pem");
    }

    /// <summary>
    /// Returns the PEM text of a fresh Ed25519 key.
    /// </summary>
    private static string CreateKeyPem() => File.ReadAllText(CreateKeyPemFile());

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
            services.Where(d => d.ServiceType == typeof(IUnauthenticatedClient))
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
            services.Where(d => d.ServiceType == typeof(IAuthenticatedClient))
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
            services.UseOpenPayments((IConfiguration)null!)
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
}

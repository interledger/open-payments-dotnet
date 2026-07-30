using Microsoft.Extensions.DependencyInjection;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Configuration;
using OpenPayments.Sdk.HttpSignatureUtils;

namespace OpenPayments.Sdk.Extensions;

/// <summary>
/// Provides extension methods for registering OpenPayments services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers OpenPayments services using the specified configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">
    /// A delegate to configure the <see cref="OpenPaymentsOptions"/> instance,
    /// allowing selection between authenticated or unauthenticated client.
    /// </param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection UseOpenPayments(
        this IServiceCollection services,
        Action<OpenPaymentsOptions> configure
    )
    {
        var options = new OpenPaymentsOptions();
        configure(options);

        return AddOpenPaymentsCore(services, options);
    }

    /// <summary>
    /// Validates the options and registers the clients. Both public overloads funnel through here
    /// so a misconfiguration fails at registration whichever way the options were supplied.
    /// </summary>
    private static IServiceCollection AddOpenPaymentsCore(
        IServiceCollection services,
        OpenPaymentsOptions options
    )
    {
        services
            .AddHttpClient(OpenPaymentsHttpClients.Unsigned)
            .ConfigurePrimaryHttpMessageHandler(CreatePrimaryHandler);

        if (options.UseUnauthenticatedClient)
        {
            services.AddHttpClient<IUnauthenticatedClient, UnauthenticatedClient>(
                OpenPaymentsHttpClients.Unsigned
            );
        }

        if (options.UseAuthenticatedClient)
        {
            // Validated here, not in the factory lambda: the signing handler registration below
            // needs a real key at registration time, so a misconfigured signed pipeline can
            // never be built.
            if (string.IsNullOrWhiteSpace(options.KeyId))
                throw new InvalidOperationException("OpenPaymentsOptions.KeyId must be provided.");
            if (options.ClientUrl is null)
                throw new InvalidOperationException(
                    "OpenPaymentsOptions.ClientUrl must be provided."
                );

            var privateKey = SigningKeyResolver.Resolve(options);

            // Captured into locals so the closures below do not hold the mutable options object.
            var keyId = options.KeyId;
            var clientUrl = options.ClientUrl;

            // A typed client supplies exactly one HttpClient, but AuthenticatedClient needs two.
            // The factory overload gives us the signed pipeline as the typed client and lets us
            // pull the unsigned one from the factory in the same transient resolution.
            services
                .AddHttpClient<IAuthenticatedClient, AuthenticatedClient>(
                    OpenPaymentsHttpClients.Signed,
                    (signed, sp) =>
                        new AuthenticatedClient(
                            signed,
                            sp.GetRequiredService<IHttpClientFactory>()
                                .CreateClient(OpenPaymentsHttpClients.Unsigned),
                            clientUrl
                        )
                )
                .ConfigurePrimaryHttpMessageHandler(CreatePrimaryHandler)
                .AddHttpMessageHandler(() => new SigningHttpMessageHandler(privateKey, keyId));
        }

        return services;
    }

    /// <summary>
    /// Primary handler for both pipelines. Typed clients are transient, so a fresh resolution
    /// always gets a rotated handler chain — but a client injected into a singleton consumer is
    /// still held for the application lifetime. Recycling pooled connections every two minutes
    /// means such a client still picks up DNS changes.
    /// </summary>
    private static HttpMessageHandler CreatePrimaryHandler() =>
        new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(2) };
}

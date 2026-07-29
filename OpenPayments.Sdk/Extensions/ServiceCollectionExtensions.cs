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

        services.AddHttpClient(OpenPaymentsHttpClients.Unsigned);

        if (options.UseUnauthenticatedClient)
        {
            services.AddSingleton<UnauthenticatedClient>(sp => new UnauthenticatedClient(
                sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient(OpenPaymentsHttpClients.Unsigned)
            ));
            services.AddSingleton<IUnauthenticatedClient>(sp =>
                sp.GetRequiredService<UnauthenticatedClient>()
            );
        }

        if (options.UseAuthenticatedClient)
        {
            // Validated here, not in the factory lambda: the signing handler registration below
            // needs a real key at registration time, so a misconfigured signed pipeline can
            // never be built.
            if (string.IsNullOrWhiteSpace(options.KeyId))
                throw new InvalidOperationException("OpenPaymentsOptions.KeyId must be provided.");
            if (options.PrivateKey is null)
                throw new InvalidOperationException(
                    "OpenPaymentsOptions.PrivateKey must be provided."
                );
            if (options.ClientUrl is null)
                throw new InvalidOperationException(
                    "OpenPaymentsOptions.ClientUrl must be provided."
                );

            // Captured into locals so the closures below do not hold the mutable options object.
            var privateKey = options.PrivateKey;
            var keyId = options.KeyId;
            var clientUrl = options.ClientUrl;

            services
                .AddHttpClient(OpenPaymentsHttpClients.Signed)
                .AddHttpMessageHandler(() => new SigningHttpMessageHandler(privateKey, keyId));

            services.AddSingleton<AuthenticatedClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                return new AuthenticatedClient(
                    factory.CreateClient(OpenPaymentsHttpClients.Signed),
                    factory.CreateClient(OpenPaymentsHttpClients.Unsigned),
                    clientUrl
                );
            });
            services.AddSingleton<IAuthenticatedClient>(sp =>
                sp.GetRequiredService<AuthenticatedClient>()
            );
        }

        return services;
    }
}

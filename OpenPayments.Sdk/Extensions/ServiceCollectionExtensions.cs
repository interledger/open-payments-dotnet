using Microsoft.Extensions.DependencyInjection;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Configuration;

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

        services.AddHttpClient();

        if (options.UseUnauthenticatedClient)
        {
            services.AddSingleton<UnauthenticatedClient>();
            services.AddSingleton<IUnauthenticatedClient>(sp =>
                sp.GetRequiredService<UnauthenticatedClient>()
            );
        }

        if (options.UseAuthenticatedClient)
        {
            services.AddSingleton<AuthenticatedClient>(sp =>
            {
                if (string.IsNullOrWhiteSpace(options.KeyId))
                    throw new InvalidOperationException(
                        "OpenPaymentsOptions.KeyId must be provided."
                    );
                if (options.PrivateKey is null)
                    throw new InvalidOperationException(
                        "OpenPaymentsOptions.PrivateKey must be provided."
                    );
                if (options.ClientUrl is null)
                    throw new InvalidOperationException(
                        "OpenPaymentsOptions.ClientUrl must be provided."
                    );

                var http = sp.GetRequiredService<IHttpClientFactory>()
                    .CreateClient("authenticated");
                return new AuthenticatedClient(
                    http,
                    options.PrivateKey,
                    options.KeyId,
                    options.ClientUrl
                );
            });
            services.AddSingleton<IAuthenticatedClient>(sp =>
                sp.GetRequiredService<AuthenticatedClient>()
            );
        }

        return services;
    }
}

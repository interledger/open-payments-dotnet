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
    public static IServiceCollection UseOpenPayments(this IServiceCollection services, Action<OpenPaymentsOptions> configure)
    {
        var options = new OpenPaymentsOptions();
        configure(options);

        services.AddHttpClient();
        if (options.UseUnauthenticatedClient)
        {
            services.AddSingleton<UnauthenticatedClient>();
            services.AddSingleton<IUnauthenticatedClient>(sp =>
                sp.GetRequiredService<UnauthenticatedClient>());
        }

        return services;
    }

    /// <summary>
    /// Configures the UnauthenticatedClient to be used with OpenPayments.
    /// </summary>
    /// <param name="options">The OpenPayments options to modify.</param>
    public static void UseUnauthenticatedClient(this OpenPaymentsOptions options)
    {
        options.UseUnauthenticatedClient = true;
    }
}   
    
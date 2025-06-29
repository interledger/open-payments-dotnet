using Microsoft.Extensions.DependencyInjection;
using OpenPayments.Sdk.Configuration;

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

        options.RegisterClient?.Invoke(services);

        return services;
    }
}
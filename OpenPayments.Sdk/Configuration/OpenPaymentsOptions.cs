using Microsoft.Extensions.DependencyInjection;
using OpenPayments.Sdk.Clients;

namespace OpenPayments.Sdk.Configuration;

/// <summary>
/// Configuration options for registering OpenPayments clients into the dependency injection container.
/// </summary>
/// <remarks>
/// Used with <see cref="ServiceCollectionExtensions.UseOpenPayments"/> to select between
/// authenticated and unauthenticated client setups./// </remarks>

public class OpenPaymentsOptions
{
    internal Action<IServiceCollection>? RegisterClient { get; private set; }

    /// <summary>
    /// Configures the OpenPayments SDK to use the unauthenticated client implementation.
    /// </summary>
    /// <remarks>
    /// This method registers the <see cref="UnauthenticatedClient"/> as a typed <see cref="HttpClient"/> service.
    /// </remarks>
    public void UseUnauthenticatedClient()
    {
        RegisterClient = services =>
        {
            services.AddHttpClient<UnauthenticatedClient>();
        };
    }
}
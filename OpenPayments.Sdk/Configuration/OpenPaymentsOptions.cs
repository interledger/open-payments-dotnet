using NSec.Cryptography;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Extensions;

namespace OpenPayments.Sdk.Configuration;

/// <summary>
/// Configuration options for registering OpenPayments clients into the dependency injection container.
/// </summary>
/// <remarks>
/// Used with <see cref="ServiceCollectionExtensions.UseOpenPayments"/> to select between
/// authenticated and unauthenticated client setups./// </remarks>
public class OpenPaymentsOptions
{
    /// <summary>
    /// Indicates whether the <see cref="UnauthenticatedClient"/> should be registered.
    /// </summary>
    public bool UseUnauthenticatedClient { get; set; }
    
    public string? KeyId { get; set; }
    
    public Key? PrivateKey { get; set; }
    
    public Uri? ClientUrl { get; set; }
}
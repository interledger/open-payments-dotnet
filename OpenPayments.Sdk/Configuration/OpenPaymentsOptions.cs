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

    /// <summary>
    /// Indicates whether the authenticated client (<c>IAuthenticatedClient</c>) should be registered.
    /// </summary>
    public bool UseAuthenticatedClient { get; set; }

    /// <summary>
    /// Key identifier (<c>kid</c>) associated with <see cref="PrivateKey"/>, sent alongside signed
    /// requests. Required when <see cref="UseAuthenticatedClient"/> is <c>true</c>.
    /// </summary>
    public string? KeyId { get; set; }

    /// <summary>
    /// Private key used to sign requests made by the authenticated client. Required when
    /// <see cref="UseAuthenticatedClient"/> is <c>true</c>.
    /// </summary>
    public Key? PrivateKey { get; set; }

    /// <summary>
    /// The client's own wallet address, used to identify the client making grant requests. Required when
    /// <see cref="UseAuthenticatedClient"/> is <c>true</c>.
    /// </summary>
    public Uri? ClientUrl { get; set; }
}

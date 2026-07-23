using NSec.Cryptography;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Extensions;

namespace OpenPayments.Sdk.Configuration;

/// <summary>
/// Configuration options for registering OpenPayments clients into the dependency injection container.
/// </summary>
/// <remarks>
/// Used with <see cref="ServiceCollectionExtensions.UseOpenPayments"/> to select between
/// authenticated and unauthenticated client setups.
/// </remarks>
public class OpenPaymentsOptions
{
    /// <summary>
    /// Indicates whether the <see cref="UnauthenticatedClient"/> should be registered.
    /// </summary>
    public bool UseUnauthenticatedClient { get; set; }

    /// <summary>
    /// Indicates whether the <see cref="Clients.AuthenticatedClient"/> should be registered.
    /// When <c>true</c>, <see cref="KeyId"/>, <see cref="PrivateKey"/>, and <see cref="ClientUrl"/>
    /// must all be provided, or <see cref="ServiceCollectionExtensions.UseOpenPayments"/> throws
    /// an <see cref="InvalidOperationException"/> immediately.
    /// </summary>
    public bool UseAuthenticatedClient { get; set; }

    /// <summary>
    /// The key ID used to sign requests when <see cref="UseAuthenticatedClient"/> is <c>true</c>.
    /// Required in that case.
    /// </summary>
    public string? KeyId { get; set; }

    /// <summary>
    /// The private key used to sign requests when <see cref="UseAuthenticatedClient"/> is <c>true</c>.
    /// Required in that case.
    /// </summary>
    public Key? PrivateKey { get; set; }

    /// <summary>
    /// The client wallet address URL (e.g. <c>https://wallet.example</c>) used when
    /// <see cref="UseAuthenticatedClient"/> is <c>true</c>. Required in that case.
    /// </summary>
    public Uri? ClientUrl { get; set; }
}

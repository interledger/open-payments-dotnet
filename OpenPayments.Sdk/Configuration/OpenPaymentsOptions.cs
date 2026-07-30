using Microsoft.Extensions.DependencyInjection;
using NSec.Cryptography;
using OpenPayments.Sdk.Clients;
using OpenPayments.Sdk.Extensions;

namespace OpenPayments.Sdk.Configuration;

/// <summary>
/// Configuration options for registering OpenPayments clients into the dependency injection container.
/// </summary>
/// <remarks>
/// Used with
/// <see cref="ServiceCollectionExtensions.UseOpenPayments(IServiceCollection, Action{OpenPaymentsOptions})"/>
/// to select between authenticated and unauthenticated client setups.
/// </remarks>
public class OpenPaymentsOptions
{
    /// <summary>
    /// Indicates whether the <see cref="UnauthenticatedClient"/> should be registered.
    /// </summary>
    public bool UseUnauthenticatedClient { get; set; }

    /// <summary>
    /// Indicates whether the <see cref="AuthenticatedClient"/> should be registered. When true,
    /// <see cref="KeyId"/> and <see cref="ClientUrl"/> are required, and exactly one of
    /// <see cref="PrivateKey"/>, <see cref="PrivateKeyPem"/> and <see cref="PrivateKeyPath"/>
    /// must be set.
    /// </summary>
    public bool UseAuthenticatedClient { get; set; }

    /// <summary>
    /// Key ID advertised in the <c>Signature-Input</c> header of every signed request.
    /// Required when <see cref="UseAuthenticatedClient"/> is true.
    /// </summary>
    public string? KeyId { get; set; }

    /// <summary>
    /// Ed25519 private key used to sign requests, supplied in code. The caller owns this key: the
    /// SDK never disposes it. This property cannot be set from configuration, because
    /// <see cref="Key"/> is an opaque handle rather than a value — use <see cref="PrivateKeyPem"/>
    /// or <see cref="PrivateKeyPath"/> instead.
    /// </summary>
    public Key? PrivateKey { get; set; }

    /// <summary>
    /// PEM-encoded PKCS#8 Ed25519 private key text. The SDK loads the key at registration time and
    /// holds it for the lifetime of the registration; it is not disposed.
    /// </summary>
    public string? PrivateKeyPem { get; set; }

    /// <summary>
    /// Path to a file holding the Ed25519 private key, either PEM-encoded PKCS#8 or a raw 32- or
    /// 64-byte key. The SDK loads the key at registration time and holds it for the lifetime of
    /// the registration; it is not disposed.
    /// </summary>
    public string? PrivateKeyPath { get; set; }

    /// <summary>
    /// Client wallet address URL, for example <c>https://wallet.example</c>. Sent as the client
    /// identifier on grant requests. Required when <see cref="UseAuthenticatedClient"/> is true.
    /// </summary>
    public Uri? ClientUrl { get; set; }
}

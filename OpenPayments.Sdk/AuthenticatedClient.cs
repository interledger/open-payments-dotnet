using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sodium;
using OpenPayments.Sdk.Generated;
using OpenPayments.Sdk.HttpSignatureUtils;
using OpenPayments.Sdk.Generated.Wallet;
using OpenPayments.Sdk.Generated.Resource;
using BodyResource = OpenPayments.Sdk.Generated.Resource.Body;
using Sodium.Exceptions;

namespace OpenPayments.Sdk;

/// <summary>
/// An Open Payments client capable of performing authenticated (GNAP) requests with HTTP Message Signatures.
/// This is an <b>opinionated &amp; simplified</b> subset that covers typical flows: requesting grants, creating incoming/outgoing payments, quotes etc.
/// </summary>
public sealed class AuthenticatedClient
{
    private readonly HttpClient _http;
    private readonly byte[] _privateKey;
    private readonly string _keyId;
    private readonly string _walletAddressUrl;

    public AuthenticatedClient(string walletAddressUrl, string keyId, ReadOnlySpan<byte> ed25519PrivateKey)
        : this(new HttpClient(), walletAddressUrl, keyId, ed25519PrivateKey)
    {
    }

    /// <summary>
    /// Create the instance of the Authenticated Client.
    /// </summary>
    /// <param name="http"></param>
    /// <param name="walletAddressUrl">The valid wallet address with which the client making requests will identify itself. A JSON Web Key Set document that includes the public key that the client instance will use to protect requests MUST be available at the {walletAddressUrl}/jwks.json url. This will be used as the client field during Grant Creation.</param>
    /// <param name="ed25519PrivateKey">The private EdDSA-Ed25519 key (or the relative or absolute path to the key) bound to the wallet address, and used to sign the authenticated requests with. As mentioned above, a public JWK document signed with this key MUST be available at the {walletAddressUrl}/jwks.json url.</param>
    /// <param name="keyId">The key identifier of the given private key and the corresponding public JWK document.</param>
    /// <exception cref="ArgumentException">Throws if walletAddressUrl is null or whitespace, or the length of the private key is not 32 or 64 bytes in length.</exception>
    public AuthenticatedClient(HttpClient http, string walletAddressUrl, string keyId, ReadOnlySpan<byte> ed25519PrivateKey)
    {
        if (string.IsNullOrWhiteSpace(walletAddressUrl))
        {
            throw new ArgumentException("walletAddressUrl is required", nameof(walletAddressUrl));
        }

        if (string.IsNullOrWhiteSpace(keyId))
        {
            throw new ArgumentException("keyId is required", nameof(keyId));
        }

        // if length is 32 bytes, then it was just the seed passed
        // if length is 64 then both the seed is passed and the corresponding public key
        if (ed25519PrivateKey.Length != 32 && ed25519PrivateKey.Length != 64)
        {
            throw new ArgumentException("Ed25519 private key must be either 32 or 64 bytes in length", nameof(ed25519PrivateKey));
        }

        _http = http;
        _walletAddressUrl = walletAddressUrl.TrimEnd('/');
        _keyId = keyId;
        _privateKey = ed25519PrivateKey[..32].ToArray();
    }
} 
using System.Diagnostics;
using System.Security.Cryptography;
using NSec.Cryptography;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.OpenSsl;

namespace OpenPayments.Sdk.HttpSignatureUtils;

/// <summary>
/// Represents the arguments for generating a key, including optional directory and filename information.
/// </summary>
public sealed class GenerateKeyArgs
{
    /// <summary>
    /// Gets or sets the directory path where the key file should be saved.
    /// If the specified directory does not exist, it will be created automatically.
    /// </summary>
    public string? Dir { get; set; }

    /// <summary>
    /// Gets or sets the filename to be used when saving the key file.
    /// If not specified, a default filename will be generated using the current timestamp.
    /// </summary>
    public string? Filename { get; set; }
}

/// <summary>
/// Utility helpers for Ed25519 key generation, persistence, and JWK conversion.
/// </summary>
public static class KeyUtils
{
    /// <summary>
    /// Loads an Ed25519 private key from a Base64-encoded string.
    /// </summary>
    /// <param name="base64Key">The Base64-encoded Ed25519 private key. Must be either 32 bytes (seed) or 64 bytes (private key + public key) after decoding.</param>
    /// <returns>
    /// A <see cref="NSec.Cryptography.Key"/> instance representing the Ed25519 private key, created from the first 32 bytes (seed) of the decoded input.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the decoded key is not exactly 32 or 64 bytes long.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown if the input string is not a valid Base64 format.
    /// </exception>
    public static Key LoadBase64Key(string base64Key)
    {
        var keyBytes = Convert.FromBase64String(base64Key);

        if (keyBytes.Length != 32 && keyBytes.Length != 64)
        {
            throw new ArgumentException(
                "Ed25519 private key must be 32 or 64 bytes after Base64 decode."
            );
        }

        var algorithm = SignatureAlgorithm.Ed25519;
        var seed = keyBytes.AsSpan(0, 32);
        return Key.Import(algorithm, seed, KeyBlobFormat.RawPrivateKey);
    }

    /// <summary>
    /// Loads an Ed25519 private key from a PEM-encoded PKCS#8 string.
    /// </summary>
    /// <param name="pem"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Key LoadPem(string pem)
    {
        // Read PEM -> DER object
        using var sr = new StringReader(pem);
        var pemObj = new PemReader(sr).ReadPemObject();
        if (pemObj == null)
            throw new ArgumentException("Invalid PEM");

        // Parse PKCS#8 PrivateKeyInfo
        var pkInfo = PrivateKeyInfo.GetInstance(Asn1Object.FromByteArray(pemObj.Content));

        // Ensure Ed25519 OID (1.3.101.112)
        var oid = pkInfo.PrivateKeyAlgorithm.Algorithm.Id;
        if (oid != "1.3.101.112")
            throw new ArgumentException(
                $"Unexpected algorithm OID: {oid}. Expected Ed25519 (1.3.101.112)."
            );

        // Extract the inner OCTET STRING (seed). For Ed25519 in PKCS#8, this is 32 bytes.
        var privateKeyOctets = Asn1OctetString.GetInstance(pkInfo.ParsePrivateKey());
        var privateKeyBytes = privateKeyOctets.GetOctets();

        // Some toolchains wrap the seed in another OCTET STRING layer:
        // If lenght is not 32, try one more unwrap.
        if (privateKeyBytes.Length != 32)
        {
            try
            {
                var inner = Asn1OctetString.GetInstance(Asn1Object.FromByteArray(privateKeyBytes));
                privateKeyBytes = inner.GetOctets();
            }
            catch
            {
                /* ignore */
            }
        }

        if (privateKeyBytes.Length != 32)
            throw new ArgumentException(
                $"Ed25519 seed must be 32 bytes, got {privateKeyBytes.Length}."
            );

        // Import into NSec as a raw private key (seed)
        var algorithm = SignatureAlgorithm.Ed25519;
        var seed = privateKeyBytes.AsSpan(0, 32);
        return Key.Import(algorithm, seed, KeyBlobFormat.RawPrivateKey);
    }

    /// <summary>
    /// Generates a JSON Web Key (JWK) representing an Ed25519 public key.
    /// </summary>
    /// <param name="keyId">
    /// A unique identifier for the key (the <c>kid</c> field in the resulting JWK). Must not be null or whitespace.
    /// </param>
    /// <param name="privateKey">
    /// An optional Ed25519 private key. If not provided, a new key will be generated. The public key will be derived from this private key.
    /// </param>
    /// <returns>
    /// A <see cref="Jwk"/> instance containing the public components of the Ed25519 key in JWK format,
    /// including the key type (<c>kty</c>), curve (<c>crv</c>), algorithm (<c>alg</c>), key ID (<c>kid</c>), and public key (<c>x</c>).
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="keyId"/> is null, empty, or contains only whitespace.
    /// </exception>
    public static Jwk GenerateJwk(string keyId, Key? privateKey = null)
    {
        if (string.IsNullOrWhiteSpace(keyId))
        {
            throw new ArgumentException("keyId cannot be empty.", nameof(keyId));
        }

        var algorithm = SignatureAlgorithm.Ed25519;

        privateKey ??= new Key(
            algorithm,
            new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport }
        );

        var publicKeyBytes = privateKey.PublicKey.Export(KeyBlobFormat.RawPublicKey);

        return new Jwk { Kid = keyId, X = Convert.ToBase64String(publicKeyBytes) };
    }

    /// <summary>
    /// Loads an Ed25519 private key from a file.
    /// </summary>
    /// <param name="keyFilePath">The file path of the private key (32- or 64-byte raw Ed25519 key).</param>
    /// <returns>The loaded <see cref="Key"/> instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file cannot be read.</exception>
    /// <exception cref="ArgumentException">Thrown if the key is not a valid 32 or 64-byte Ed25519 key.</exception>
    public static Key LoadKey(string keyFilePath)
    {
        byte[] keyBytes;

        try
        {
            keyBytes = File.ReadAllBytes(keyFilePath);
        }
        catch (Exception)
        {
            throw new FileNotFoundException($"Could not load file: {keyFilePath}");
        }

        if (keyBytes.Length != 32 && keyBytes.Length != 64)
        {
            throw new ArgumentException(
                "File was loaded, but key was not a valid Ed25519 private key (must be 32 or 64 bytes)."
            );
        }

        var seed = keyBytes.AsSpan(0, 32).ToArray();

        try
        {
            return Key.Import(SignatureAlgorithm.Ed25519, seed, KeyBlobFormat.RawPrivateKey);
        }
        catch (Exception)
        {
            throw new ArgumentException("Private key was invalid or improperly formatted.");
        }
    }

    /// <summary>
    /// Generates an EdDSA Ed25519 private key and optionally saves it as a PEM-encoded PKCS#8 file.
    /// </summary>
    /// <param name="args">Optional arguments to specify directory and filename for saving the key.</param>
    /// <returns>The generated <see cref="Ed25519"/> <see cref="AsymmetricAlgorithm"/> as a <see cref="AsymmetricAlgorithm"/>.</returns>
    public static Key GenerateKey(GenerateKeyArgs? args = null)
    {
        var key = new Key(
            SignatureAlgorithm.Ed25519,
            new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport }
        );

        if (args?.Dir is not null)
        {
            Directory.CreateDirectory(args.Dir);
            var fileName =
                args.Filename
                ?? $"private-key-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.pem";
            var path = Path.Combine(args.Dir, fileName);

            key.ToPem(path);
        }

        return key;
    }

    /// <summary>
    /// Loads an Ed25519 private key from the specified file path.
    /// If the file is missing or invalid, a new key is generated and optionally saved.
    /// </summary>
    /// <param name="keyFilePath">Optional path to an existing Ed25519 private key file (raw 32- or 64-byte).</param>
    /// <param name="generateKeyArgs">Optional parameters for saving the generated key to a directory and file name.</param>
    /// <returns>The loaded or newly generated <see cref="Key"/>.</returns>
    public static Key LoadOrGenerateKey(
        string? keyFilePath = null,
        GenerateKeyArgs? generateKeyArgs = null
    )
    {
        if (!string.IsNullOrWhiteSpace(keyFilePath))
        {
            try
            {
                return LoadKey(keyFilePath);
            }
            catch (Exception)
            {
                // Could not load key, fallback to generate new one
            }
        }

        return GenerateKey(generateKeyArgs);
    }
}

using NSec.Cryptography;
using OpenPayments.Sdk.HttpSignatureUtils;

namespace OpenPayments.Sdk.Configuration;

/// <summary>
/// Turns the three mutually exclusive key sources on <see cref="OpenPaymentsOptions"/> into a
/// single <see cref="Key"/>.
/// </summary>
internal static class SigningKeyResolver
{
    /// <summary>
    /// Resolves the signing key from whichever key source is configured.
    /// </summary>
    /// <param name="options">Options holding the key sources.</param>
    /// <returns>The signing key.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no key source is set, when more than one is set, or when
    /// <see cref="OpenPaymentsOptions.PrivateKeyPath"/> points at a file that does not exist.
    /// </exception>
    internal static Key Resolve(OpenPaymentsOptions options)
    {
        var sources = new List<string>();
        if (options.PrivateKey is not null)
            sources.Add(nameof(OpenPaymentsOptions.PrivateKey));
        if (!string.IsNullOrWhiteSpace(options.PrivateKeyPem))
            sources.Add(nameof(OpenPaymentsOptions.PrivateKeyPem));
        if (!string.IsNullOrWhiteSpace(options.PrivateKeyPath))
            sources.Add(nameof(OpenPaymentsOptions.PrivateKeyPath));

        if (sources.Count == 0)
            throw new InvalidOperationException(
                "Exactly one signing key source must be provided on OpenPaymentsOptions: "
                    + "PrivateKey, PrivateKeyPem or PrivateKeyPath."
            );

        if (sources.Count > 1)
            throw new InvalidOperationException(
                "Only one signing key source may be set on OpenPaymentsOptions, but these were "
                    + $"all provided: {string.Join(", ", sources)}."
            );

        if (options.PrivateKey is not null)
            return options.PrivateKey;

        if (!string.IsNullOrWhiteSpace(options.PrivateKeyPem))
            return KeyUtils.LoadPem(options.PrivateKeyPem);

        return LoadFromFile(options.PrivateKeyPath!);
    }

    /// <summary>
    /// Loads a key file that may hold PEM text or raw key bytes. KeyUtils.GenerateKey writes PEM,
    /// so PEM is the common case; KeyUtils.LoadKey covers the raw form.
    /// </summary>
    private static Key LoadFromFile(string path)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException(
                $"OpenPaymentsOptions.PrivateKeyPath points at a file that does not exist: {path}"
            );

        var text = File.ReadAllText(path);
        return text.Contains("-----BEGIN", StringComparison.Ordinal)
            ? KeyUtils.LoadPem(text)
            : KeyUtils.LoadKey(path);
    }
}

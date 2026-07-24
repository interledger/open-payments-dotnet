using System.Security.Cryptography;
using NSec.Cryptography;

namespace Interledger.OpenPayments.HttpSignatureUtils;

internal static class KeyExtensions
{
    public static void ToPem(this Key key, string filePath)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required.", nameof(filePath));

        var seed = key.Export(KeyBlobFormat.RawPrivateKey);
        var pkcs8 = Ed25519Pkcs8.Encode(seed);

        File.WriteAllText(filePath, new string(PemEncoding.Write("PRIVATE KEY", pkcs8)) + "\n");
    }
}

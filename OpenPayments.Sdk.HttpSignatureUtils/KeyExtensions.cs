using NSec.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Utilities.IO.Pem;

internal static class KeyExtensions
{
    public static void ToPem(this Key key, string filePath)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));

        byte[] seed = key.Export(KeyBlobFormat.RawPrivateKey);
        var bcKey = new Ed25519PrivateKeyParameters(seed, 0);

        var privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(bcKey);
        var pkcs8Bytes = privateKeyInfo.ToAsn1Object().GetEncoded();

        var pemObject = new PemObject("PRIVATE KEY", pkcs8Bytes);

        using var sw = new StreamWriter(filePath);
        var pemWriter = new Org.BouncyCastle.Utilities.IO.Pem.PemWriter(sw);
        pemWriter.WriteObject(pemObject);
        sw.Flush();
    }
}
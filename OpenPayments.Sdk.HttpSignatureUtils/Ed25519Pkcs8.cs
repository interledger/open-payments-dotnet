namespace Interledger.OpenPayments.HttpSignatureUtils;

/// <summary>
/// Minimal PKCS#8 (RFC 5208 / RFC 8410) encoding and decoding for Ed25519 private keys,
/// replacing the previous Portable.BouncyCastle dependency. An Ed25519 PKCS#8 blob is the
/// fixed prefix below followed by the 32-byte seed.
/// </summary>
internal static class Ed25519Pkcs8
{
    // SEQUENCE(46) { INTEGER 0, SEQUENCE(5) { OID 1.3.101.112 }, OCTET STRING(34) { OCTET STRING(32) seed } }
    private static readonly byte[] Prefix =
    [
        0x30, 0x2e,                                     // SEQUENCE, 46 bytes
        0x02, 0x01, 0x00,                               // INTEGER 0 (version)
        0x30, 0x05, 0x06, 0x03, 0x2b, 0x65, 0x70,       // AlgorithmIdentifier { OID 1.3.101.112 (Ed25519) }
        0x04, 0x22,                                     // OCTET STRING, 34 bytes (privateKey)
        0x04, 0x20,                                     // inner OCTET STRING, 32 bytes (CurvePrivateKey seed)
    ];

    private static readonly byte[] Ed25519Oid = [0x06, 0x03, 0x2b, 0x65, 0x70];

    public static byte[] Encode(ReadOnlySpan<byte> seed)
    {
        if (seed.Length != 32)
            throw new ArgumentException("Ed25519 seed must be 32 bytes.", nameof(seed));

        var der = new byte[Prefix.Length + seed.Length];
        Prefix.CopyTo(der, 0);
        seed.CopyTo(der.AsSpan(Prefix.Length));
        return der;
    }

    public static byte[] DecodeSeed(ReadOnlySpan<byte> der)
    {
        // Fast path: the canonical RFC 8410 layout.
        if (der.Length == Prefix.Length + 32 && der.StartsWith(Prefix))
            return der[Prefix.Length..].ToArray();

        // Tolerant path: verify structure field by field so the error names the real problem
        // (mirrors the OID check and single/double OCTET STRING handling of the old
        // BouncyCastle-based implementation).
        if (der.Length < 16 || der[0] != 0x30 || der[2] != 0x02 || der[3] != 0x01 || der[4] != 0x00)
            throw new ArgumentException("Not a PKCS#8 private key.");

        if (der[5] != 0x30 || !der[7..12].SequenceEqual(Ed25519Oid))
            throw new ArgumentException(
                "Unexpected key algorithm. Expected Ed25519 (OID 1.3.101.112)."
            );

        if (der[12] != 0x04)
            throw new ArgumentException("Malformed PKCS#8 private key: missing OCTET STRING.");

        int length = der[13];
        if (14 + length > der.Length)
            throw new ArgumentException("Malformed PKCS#8 private key: truncated OCTET STRING.");

        var content = der.Slice(14, length);

        // Standard double-wrap: OCTET STRING(34) containing OCTET STRING(32).
        if (length == 34 && content[0] == 0x04 && content[1] == 0x20)
            return content[2..].ToArray();

        // Some toolchains emit the seed directly.
        if (length == 32)
            return content.ToArray();

        throw new ArgumentException($"Ed25519 seed must be 32 bytes, got {length}.");
    }
}

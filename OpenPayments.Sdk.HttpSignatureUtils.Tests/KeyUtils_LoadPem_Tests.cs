using System;
using System.IO;
using System.Security.Cryptography;
using AwesomeAssertions;
using NSec.Cryptography;
using Xunit;

namespace OpenPayments.Sdk.HttpSignatureUtils.Tests;

public class KeyUtils_LoadPem_Tests
{
    // Generated with: openssl genpkey -algorithm ed25519
    private const string FixturePem = """
        -----BEGIN PRIVATE KEY-----
        MC4CAQAwBQYDK2VwBCIEILGNquZIIajfyOBSv5HwSbBWCNHRPRud6bogzSznTuLH
        -----END PRIVATE KEY-----
        """;

    // openssl pkey -pubout -outform DER | tail -c 32 | base64 of the fixture key
    private const string FixturePublicKeyBase64 = "OLhFXh/6GCJhiBVPDLj4CIc+dKTZLn+31PiRe9Oq/3E=";

    [Fact]
    public void LoadPem_OpenSslGeneratedPem_ImportsExpectedKey()
    {
        var key = KeyUtils.LoadPem(FixturePem);

        Convert.ToBase64String(key.PublicKey.Export(KeyBlobFormat.RawPublicKey))
            .Should()
            .Be(FixturePublicKeyBase64);
    }

    [Fact]
    public void LoadPem_RoundTripsWithGenerateKey()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            var generated = KeyUtils.GenerateKey(
                new GenerateKeyArgs { Dir = dir, Filename = "roundtrip.pem" }
            );

            var loaded = KeyUtils.LoadPem(File.ReadAllText(Path.Combine(dir, "roundtrip.pem")));

            loaded
                .PublicKey.Export(KeyBlobFormat.RawPublicKey)
                .Should()
                .Equal(generated.PublicKey.Export(KeyBlobFormat.RawPublicKey));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void LoadPem_NotPem_Throws()
    {
        var act = () => KeyUtils.LoadPem("definitely not a pem");

        act.Should().Throw<ArgumentException>().WithMessage("*Invalid PEM*");
    }

    [Fact]
    public void LoadPem_WrongOid_ThrowsMentioningEd25519()
    {
        // Corrupt the Ed25519 OID's last byte (0x70 -> 0x71) in an otherwise valid blob.
        var der = Convert.FromBase64String(
            "MC4CAQAwBQYDK2VwBCIEILGNquZIIajfyOBSv5HwSbBWCNHRPRud6bogzSznTuLH"
        );
        der[11] = 0x71;
        var pem = new string(PemEncoding.Write("PRIVATE KEY", der));

        var act = () => KeyUtils.LoadPem(pem);

        act.Should().Throw<ArgumentException>().WithMessage("*Ed25519*");
    }
}

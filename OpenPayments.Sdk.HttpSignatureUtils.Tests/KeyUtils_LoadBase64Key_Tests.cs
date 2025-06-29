using System;
using NSec.Cryptography;
using Xunit;

namespace OpenPayments.Sdk.HttpSignatureUtils.Tests;

public class KeyUtils_LoadBase64Key_Tests
{
    [Fact]
    public void LoadBase64Key_Valid32ByteKey_ReturnsKey()
    {
        byte[] rawKey = new byte[32];
        new Random().NextBytes(rawKey);
        string base64 = Convert.ToBase64String(rawKey);

        Key key = KeyUtils.LoadBase64Key(base64);
        Assert.NotNull(key);
        Assert.Equal(SignatureAlgorithm.Ed25519, key.Algorithm);
    }

    [Fact]
    public void LoadBase64Key_Valid64ByteKey_UseFirst32BytesAsSeed()
    {
        byte[] fullKey = new byte[64];
        new Random().NextBytes(fullKey);
        string base64 = Convert.ToBase64String(fullKey);

        Key key = KeyUtils.LoadBase64Key(base64);

        Assert.NotNull(key);
        Assert.Equal(SignatureAlgorithm.Ed25519, key.Algorithm);
    }

    [Fact]
    public void LoadBase64Key_InvalidBase64_ThrowsFormatException()
    {
        var ex = Assert.Throws<FormatException>(() =>
            KeyUtils.LoadBase64Key("not-a-valid-base64"));

        Assert.IsType<FormatException>(ex);
    }

    [Fact]
    public void LoadBase64Key_InvalidLength_ThrowsArgumentException()
    {
        byte[] invalidBytes = new byte[10];
        string base64 = Convert.ToBase64String(invalidBytes);

        var ex = Assert.Throws<ArgumentException>(() =>
            KeyUtils.LoadBase64Key(base64)
        );

        Assert.Equal("Ed25519 private key must be 32 or 64 bytes after Base64 decode.", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void LoadBase64Key_EmptyOrNull_ThrowsException(string input)
    {
        if (input == null)
        {
            Assert.Throws<ArgumentNullException>(() => KeyUtils.LoadBase64Key(input));
        }
        else
        {
            Assert.Throws<ArgumentException>(() => KeyUtils.LoadBase64Key(input));
        }
    }
}
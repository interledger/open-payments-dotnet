using System;
using System.IO;
using NSec.Cryptography;
using Xunit;

namespace OpenPayments.Sdk.HttpSignatureUtils.Tests;

public class KeyUtils_LoadKey_Tests
{
    private string createTemporaryKeyFile(byte[] keyBytes)
    {
        string path = Path.GetTempFileName();
        File.WriteAllBytes(path, keyBytes);
        return path;
    }

    [Fact]
    public void LoadKey_Valid32ByteKey_Success()
    {
        byte[] seed = new byte[32];
        new Random().NextBytes(seed);

        string path = createTemporaryKeyFile(seed);

        Key key = KeyUtils.LoadKey(path);

        Assert.NotNull(key);
        Assert.Equal(SignatureAlgorithm.Ed25519, key.Algorithm);

        File.Delete(path);
    }

    [Fact]
    public void LoadKey_Valid64ByteKey_Success()
    {
        byte[] fullKey = new byte[64];
        new Random().NextBytes(fullKey);

        string path = createTemporaryKeyFile(fullKey);

        Key key = KeyUtils.LoadKey(path);

        Assert.NotNull(key);
        Assert.Equal(SignatureAlgorithm.Ed25519, key.Algorithm);

        File.Delete(path);
    }

    [Fact]
    public void LoadKey_FileNotFound_Throws()
    {
        var ex = Assert.Throws<FileNotFoundException>(() =>
            KeyUtils.LoadKey("non-existent-file.key"));

        Assert.Contains("Could not load file", ex.Message);
    }

    [Fact]
    public void LoadKey_InvalidLength_Throws()
    {
        byte[] invalid = new byte[10];
        string path = createTemporaryKeyFile(invalid);

        var ex = Assert.Throws<ArgumentException>(() =>
            KeyUtils.LoadKey(path));

        Assert.Contains("not a valid Ed25519 private key", ex.Message);

        File.Delete(path);
    }

    [Fact]
    public void LoadKey_InvalidSeed_Throws()
    {
        byte[] seed = new byte[32];
        string path = createTemporaryKeyFile(seed);

        var key = KeyUtils.LoadKey(path);
        Assert.NotNull(key);

        File.Delete(path);
    }
}
using System;
using System.IO;
using NSec.Cryptography;
using Xunit;

namespace OpenPayments.Sdk.HttpSignatureUtils.Tests;

public class KeyUtils_LoadOrGenerateKey_Tests
{
    private static string CreateTempKeyFile(byte[] keyBytes)
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".key");
        File.WriteAllBytes(path, keyBytes);
        return path;
    }

    [Fact]
    public void LoadOrGenerateKey_LoadsValidKeyFile()
    {
        var originalKey = KeyUtils.GenerateKey();
        byte[] seed = originalKey.Export(KeyBlobFormat.RawPrivateKey);
        string path = CreateTempKeyFile(seed);

        var loadedKey = KeyUtils.LoadOrGenerateKey(path);

        Assert.NotNull(loadedKey);
        Assert.Equal(SignatureAlgorithm.Ed25519, loadedKey.Algorithm);

        File.Delete(path);
    }

    [Fact]
    public void LoadOrGenerateKey_FileDoesNotExist_GeneratesNewKey()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".key");

        var key = KeyUtils.LoadOrGenerateKey(path, new GenerateKeyArgs
        {
            Dir = Path.GetDirectoryName(path),
            Filename = Path.GetFileName(path)
        });

        Assert.NotNull(key);
        Assert.True(File.Exists(path), "Expected fallback key to be saved to disk.");

        File.Delete(path);
    }

    [Fact]
    public void LoadOrGenerateKey_FileInvalid_FallsBackToGenerate()
    {
        string path = CreateTempKeyFile(new byte[] { 1, 2, 3, 4 }); // Invalid key

        var key = KeyUtils.LoadOrGenerateKey(path, new GenerateKeyArgs
        {
            Dir = Path.GetDirectoryName(path),
            Filename = Path.GetFileName(path)
        });

        Assert.NotNull(key);
        Assert.True(File.Exists(path), "Expected fallback key to be saved to disk.");

        File.Delete(path);
    }

    [Fact]
    public void LoadOrGenerateKey_NoFilePath_GeneratesKey()
    {
        var key = KeyUtils.LoadOrGenerateKey(null, new GenerateKeyArgs
        {
            Dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
        });

        Assert.NotNull(key);
    }
}
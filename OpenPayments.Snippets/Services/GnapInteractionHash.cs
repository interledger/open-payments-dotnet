using System.Security.Cryptography;
using System.Text;

namespace OpenPayments.Snippets.Services;

public static class GnapInteractionHash
{
    public static string Compute(
        string clientNonce,
        string asNonce,
        string interactRef,
        Uri grantRequestUri
    )
    {
        var data = $"{clientNonce}\n{asNonce}\n{interactRef}\n{grantRequestUri}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

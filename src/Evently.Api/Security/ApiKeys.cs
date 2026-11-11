using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Evently.Api;

public static class ApiKeys
{
    public const string Scheme = "ApiKey";
    public const string Prefix = "evt_";

    public static string NewSecret() => Prefix + WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    // Secrets are random 256-bit values, so a plain SHA-256 is enough; only the hash is stored.
    public static string Hash(string secret) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
}

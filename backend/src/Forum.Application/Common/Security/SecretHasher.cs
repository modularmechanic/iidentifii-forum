using Forum.Application.Common.Options;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Forum.Application.Common.Security;

/// <summary>
/// Hashes the secrets sent by email, so the stored value cannot be used to sign in. A keyed hash
/// is enough here, unlike a password: these secrets are long, random and short-lived, so there is
/// nothing to guess offline.
/// </summary>
public sealed class SecretHasher(IOptions<TokenOptions> options)
{
    private readonly byte[] _pepper = Encoding.UTF8.GetBytes(options.Value.Pepper);

    public string Hash(string secret)
        => Convert.ToHexString(HMACSHA256.HashData(_pepper, Encoding.UTF8.GetBytes(secret)));

    /// <summary>Compares in constant time, so a wrong answer takes as long as a right one.</summary>
    public bool Matches(string secret, string expectedHash)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Hash(secret)),
            Encoding.UTF8.GetBytes(expectedHash));
}

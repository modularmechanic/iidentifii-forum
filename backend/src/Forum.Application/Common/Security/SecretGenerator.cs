using System.Security.Cryptography;

namespace Forum.Application.Common.Security;

/// <summary>Produces the secrets sent by email.</summary>
public static class SecretGenerator
{
    private const int LinkTokenBytes = 32;

    /// <summary>
    /// A token for a link: 256 bits from the system generator, in a form safe to put in a URL.
    /// </summary>
    public static string NewLinkToken()
        => Base64UrlEncode(RandomNumberGenerator.GetBytes(LinkTokenBytes));

    /// <summary>
    /// A six-digit code for a person to type. Short enough to read from an email, which is why the
    /// number of attempts is limited rather than relying on the code's own strength.
    /// </summary>
    public static string NewCode()
        => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    private static string Base64UrlEncode(byte[] value)
        => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

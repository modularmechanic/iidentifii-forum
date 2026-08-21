using System.Security.Cryptography;

namespace Forum.Api.Configuration;

/// <summary>
/// Supplies the signing key and the token pepper in Development when nothing else has.
///
/// Both are real secrets: the signing key mints sessions, and the pepper protects the one-time
/// tokens behind confirmation and password reset. A value committed to the repository is a value
/// everybody has, so these are generated per run instead. Nothing carries over a restart — an
/// existing session stops being recognised, and an unspent link stops working — which is the
/// right trade in Development, and is why this applies nowhere else.
///
/// Set <c>Jwt:SigningKey</c> and <c>Tokens:Pepper</c> yourself, through user secrets or the
/// environment, to keep them steady between runs.
/// </summary>
public static class DevelopmentSecrets
{
    public static void AddDevelopmentSecrets(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment())
        {
            return;
        }

        var generated = new Dictionary<string, string?>();

        Fill(builder.Configuration, generated, "Jwt:SigningKey");
        Fill(builder.Configuration, generated, "Tokens:Pepper");

        if (generated.Count > 0)
        {
            builder.Configuration.AddInMemoryCollection(generated);
        }
    }

    private static void Fill(
        IConfiguration configuration,
        Dictionary<string, string?> generated,
        string key)
    {
        if (string.IsNullOrWhiteSpace(configuration[key]))
        {
            generated[key] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        }
    }
}

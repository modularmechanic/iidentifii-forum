using System.Text.Json;
using System.Text.Json.Serialization;

namespace Forum.Tests;

/// <summary>
/// Matches how the API writes payloads: camel case, with enums as names rather than numbers.
/// Without this, a test client reading an enum would fail on a value the API is right to send.
/// </summary>
public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}

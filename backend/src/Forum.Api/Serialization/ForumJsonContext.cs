using System.Text.Json;
using System.Text.Json.Serialization;
using Forum.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Serialization;

/// <summary>
/// Serialiser metadata for every payload the API accepts or returns, produced by the
/// System.Text.Json source generator. Registering this resolver keeps request and response
/// handling free of runtime reflection, which is why <c>JsonSerializerIsReflectionEnabledByDefault</c>
/// can stay off.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(ValidationProblemDetails))]
[JsonSerializable(typeof(IDictionary<string, object?>))]
[JsonSerializable(typeof(JsonElement))]
public sealed partial class ForumJsonContext : JsonSerializerContext;

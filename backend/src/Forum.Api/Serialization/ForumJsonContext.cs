using System.Text.Json;
using System.Text.Json.Serialization;
using Forum.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Forum.Api.Serialization;

/// <summary>
/// Compile-time serialiser metadata for every payload the API returns. Registering this
/// resolver keeps request and response handling free of runtime reflection.
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

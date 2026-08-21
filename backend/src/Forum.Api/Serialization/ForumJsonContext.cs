using Forum.Api.Contracts;
using Forum.Application.Common.Models;
using Forum.Application.Dtos;
using Forum.Application.Queries;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Text.Json;

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
[JsonSerializable(typeof(RegisterRequest))]
[JsonSerializable(typeof(VerifyEmailRequest))]
[JsonSerializable(typeof(ResendVerificationRequest))]
[JsonSerializable(typeof(AcknowledgementResponse))]
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(LoginChallengeResponse))]
[JsonSerializable(typeof(VerifyTwoFactorRequest))]
[JsonSerializable(typeof(AuthenticatedResponse))]
[JsonSerializable(typeof(ForgotPasswordRequest))]
[JsonSerializable(typeof(ResetPasswordRequest))]
[JsonSerializable(typeof(PostDto))]
[JsonSerializable(typeof(PostSort))]
[JsonSerializable(typeof(SortOrder))]
[JsonSerializable(typeof(PagedResult<PostDto>))]
[JsonSerializable(typeof(CommentDto))]
[JsonSerializable(typeof(PagedResult<CommentDto>))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(ValidationProblemDetails))]
[JsonSerializable(typeof(IDictionary<string, object?>))]
[JsonSerializable(typeof(Dictionary<string, string[]>))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(JsonElement))]
public sealed partial class ForumJsonContext : JsonSerializerContext;

using Forum.Domain.Users;

namespace Forum.Application.Dtos;

/// <summary>A member, as the API describes them.</summary>
public sealed record UserDto(Guid Id, string Username, string Email, UserRole Role);

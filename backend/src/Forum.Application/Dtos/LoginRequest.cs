using System.ComponentModel.DataAnnotations;

namespace Forum.Application.Dtos;

/// <summary>The first step of signing in: who you are and what you know.</summary>
public sealed record LoginRequest
{
    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

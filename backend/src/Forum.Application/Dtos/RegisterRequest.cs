using Forum.Domain.Users;
using System.ComponentModel.DataAnnotations;

namespace Forum.Application.Dtos;

/// <summary>What somebody supplies to create an account.</summary>
public sealed record RegisterRequest
{
    [Required]
    [StringLength(User.UsernameMaxLength, MinimumLength = User.UsernameMinLength)]
    [RegularExpression(
        "^[A-Za-z0-9_]+$",
        ErrorMessage = "A username may contain letters, numbers and underscores.")]
    public string Username { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(User.EmailMaxLength)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "A password must be at least 8 characters.")]
    public string Password { get; init; } = string.Empty;
}

using Forum.Domain.Users;
using System.ComponentModel.DataAnnotations;

namespace Forum.Application.Dtos;

/// <summary>What somebody supplies to ask for another verification email.</summary>
public sealed record ResendVerificationRequest
{
    [Required]
    [EmailAddress]
    [StringLength(User.EmailMaxLength)]
    public string Email { get; init; } = string.Empty;
}

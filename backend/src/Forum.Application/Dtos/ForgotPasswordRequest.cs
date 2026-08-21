using System.ComponentModel.DataAnnotations;
using Forum.Domain.Users;

namespace Forum.Application.Dtos;

/// <summary>Asks for a link to set a new password.</summary>
public sealed record ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    [StringLength(User.EmailMaxLength)]
    public string Email { get; init; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;

namespace Forum.Application.Dtos;

/// <summary>Sets a new password, using the link that was emailed.</summary>
public sealed record ResetPasswordRequest
{
    [Required]
    public string Token { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "A password must be at least 8 characters.")]
    public string NewPassword { get; init; } = string.Empty;
}

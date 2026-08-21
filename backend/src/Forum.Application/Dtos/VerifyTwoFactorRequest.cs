using System.ComponentModel.DataAnnotations;

namespace Forum.Application.Dtos;

/// <summary>The second step of signing in: the code from the email.</summary>
public sealed record VerifyTwoFactorRequest
{
    [Required]
    public Guid ChallengeId { get; init; }

    [Required]
    [RegularExpression("^[0-9]{6}$", ErrorMessage = "A sign-in code is six digits.")]
    public string Code { get; init; } = string.Empty;
}

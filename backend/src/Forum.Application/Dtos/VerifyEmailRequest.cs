using System.ComponentModel.DataAnnotations;

namespace Forum.Application.Dtos;

/// <summary>What somebody supplies to confirm an address.</summary>
public sealed record VerifyEmailRequest
{
    [Required]
    public string Token { get; init; } = string.Empty;
}

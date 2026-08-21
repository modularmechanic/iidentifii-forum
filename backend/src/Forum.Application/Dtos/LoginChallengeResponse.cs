namespace Forum.Application.Dtos;

/// <summary>
/// What the password step returns: a code is on its way, and this identifies the attempt it
/// belongs to. The address is masked, so it confirms which inbox to check without publishing it.
/// </summary>
public sealed record LoginChallengeResponse(Guid ChallengeId, string MaskedEmail, DateTimeOffset ExpiresAt);

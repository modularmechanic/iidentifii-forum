namespace Forum.Application.Dtos;

/// <summary>Said in reply to anything that must not reveal whether an account exists.</summary>
public sealed record AcknowledgementResponse(string Message);

namespace Forum.Domain.Users;

/// <summary>What a one-time token lets its holder do.</summary>
public enum TokenPurpose
{
    /// <summary>Confirms the address given at registration.</summary>
    EmailVerification,

    /// <summary>Completes a sign-in that has already passed the password check.</summary>
    TwoFactor,

    /// <summary>Authorises setting a new password without knowing the old one.</summary>
    PasswordReset,
}

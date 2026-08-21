using Forum.Application.Common.Interfaces;

namespace Forum.Application.Emails;

/// <summary>
/// The messages the forum sends about an account. Kept together so their wording can be read and
/// changed in one place rather than found among the code that sends them.
/// </summary>
public static class AuthEmails
{
    public static EmailMessage Verification(string emailAddress, string username, string link)
        => new(
            emailAddress,
            "Confirm your email address",
            $"""
             Hello {username},

             Confirm your email address to finish setting up your forum account:

             {link}

             The link works once and expires in an hour. If you did not create an account, you can
             ignore this message.
             """);

    public static EmailMessage SignInCode(string emailAddress, string username, string code)
        => new(
            emailAddress,
            "Your sign-in code",
            $"""
             Hello {username},

             Your sign-in code is {code}

             It expires in ten minutes. If you did not try to sign in, change your password.
             """);

    public static EmailMessage PasswordReset(string emailAddress, string username, string link)
        => new(
            emailAddress,
            "Reset your password",
            $"""
             Hello {username},

             Use this link to choose a new password:

             {link}

             The link works once and expires in an hour. If you did not ask to reset your password,
             you can ignore this message; your current password still works.
             """);
}

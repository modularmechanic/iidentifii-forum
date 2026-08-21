using FluentAssertions;
using Forum.Domain.Common;
using Forum.Domain.Users;
using Xunit;

namespace Forum.Tests.Domain;

public sealed class UserTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan TenMinutes = TimeSpan.FromMinutes(10);

    private static UserToken NewToken()
        => UserToken.Issue(Guid.CreateVersion7(), TokenPurpose.TwoFactor, "hash", TenMinutes, Now);

    [Fact]
    public void A_fresh_token_is_usable()
    {
        NewToken().IsUsable(Now).Should().BeTrue();
    }

    [Fact]
    public void A_token_stops_being_usable_once_it_expires()
    {
        var token = NewToken();

        token.IsUsable(Now.Add(TenMinutes)).Should().BeFalse();
    }

    [Fact]
    public void A_consumed_token_cannot_be_used_again()
    {
        var token = NewToken();

        token.Consume(Now);

        token.IsUsable(Now).Should().BeFalse();
    }

    [Fact]
    public void Enough_wrong_guesses_spend_the_token()
    {
        var token = NewToken();

        for (var attempt = 0; attempt < UserToken.MaxFailedAttempts; attempt++)
        {
            token.RecordFailedAttempt(Now);
        }

        token.IsUsable(Now).Should().BeFalse();
        token.ConsumedAt.Should().Be(Now);
    }

    [Fact]
    public void A_wrong_guess_short_of_the_limit_leaves_the_token_usable()
    {
        var token = NewToken();

        token.RecordFailedAttempt(Now);

        token.IsUsable(Now).Should().BeTrue();
    }

    [Fact]
    public void A_token_that_expires_immediately_is_refused()
    {
        var act = () => UserToken.Issue(Guid.CreateVersion7(), TokenPurpose.TwoFactor, "hash", TimeSpan.Zero, Now);

        act.Should().Throw<DomainException>()
            .Which.Error.Should().Be(DomainError.RuleViolation);
    }
}

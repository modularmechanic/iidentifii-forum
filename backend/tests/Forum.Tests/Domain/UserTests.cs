using FluentAssertions;
using Forum.Domain.Common;
using Forum.Domain.Users;
using Xunit;

namespace Forum.Tests.Domain;

public sealed class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Registering_trims_the_username_and_address_and_starts_unverified()
    {
        var user = User.Register("  alice  ", "  alice@forum.local  ", "hash", Now);

        user.Username.Should().Be("alice");
        user.Email.Should().Be("alice@forum.local");
        user.Role.Should().Be(UserRole.Member);
        user.IsEmailVerified.Should().BeFalse();
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("")]
    public void A_username_shorter_than_the_minimum_is_refused(string username)
    {
        var act = () => User.Register(username, "alice@forum.local", "hash", Now);

        act.Should().Throw<DomainException>()
            .Which.Error.Should().Be(DomainError.RuleViolation);
    }

    [Fact]
    public void An_address_is_required()
    {
        var act = () => User.Register("alice", "  ", "hash", Now);

        act.Should().Throw<DomainException>()
            .Which.Error.Should().Be(DomainError.RuleViolation);
    }

    [Fact]
    public void Verifying_twice_keeps_the_first_moment()
    {
        var user = User.Register("alice", "alice@forum.local", "hash", Now);

        user.VerifyEmail(Now);
        user.VerifyEmail(Now.AddDays(1));

        user.EmailVerifiedAt.Should().Be(Now);
    }

    [Fact]
    public void Changing_to_an_empty_password_is_refused()
    {
        var user = User.Register("alice", "alice@forum.local", "hash", Now);

        var act = () => user.ChangePassword("  ");

        act.Should().Throw<DomainException>();
    }
}

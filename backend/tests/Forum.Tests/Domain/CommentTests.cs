using FluentAssertions;
using Forum.Domain.Comments;
using Forum.Domain.Common;
using Xunit;

namespace Forum.Tests.Domain;

public sealed class CommentTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Author = Guid.CreateVersion7();
    private static readonly Guid Stranger = Guid.CreateVersion7();

    private static Comment NewComment()
        => Comment.Create(Guid.CreateVersion7(), Author, "At least once.", Now);

    [Fact]
    public void Editing_someone_elses_reply_is_refused()
    {
        var comment = NewComment();

        var act = () => comment.Update(Stranger, "Hijacked.", Now);

        act.Should().Throw<DomainException>()
            .Which.Error.Should().Be(DomainError.Forbidden);
    }

    [Fact]
    public void Editing_your_own_reply_records_when_it_changed()
    {
        var comment = NewComment();
        var later = Now.AddMinutes(5);

        comment.Update(Author, "  At least once, with a day of retries.  ", later);

        comment.Body.Should().Be("At least once, with a day of retries.");
        comment.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void An_empty_reply_is_refused()
    {
        var act = () => Comment.Create(Guid.CreateVersion7(), Author, "   ", Now);

        act.Should().Throw<DomainException>()
            .Which.Error.Should().Be(DomainError.RuleViolation);
    }

    [Fact]
    public void A_reply_longer_than_the_limit_is_refused()
    {
        var act = () => Comment.Create(
            Guid.CreateVersion7(),
            Author,
            new string('x', Comment.BodyMaxLength + 1),
            Now);

        act.Should().Throw<DomainException>()
            .Which.Error.Should().Be(DomainError.RuleViolation);
    }
}

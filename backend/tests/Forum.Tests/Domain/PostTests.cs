using FluentAssertions;
using Forum.Domain.Common;
using Forum.Domain.Posts;
using Xunit;

namespace Forum.Tests.Domain;

public sealed class PostTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Author = Guid.CreateVersion7();
    private static readonly Guid Reader = Guid.CreateVersion7();

    private static Post NewPost() => Post.Create(Author, "Webhook retries", "Delivered twice.", Now);

    [Fact]
    public void Liking_someone_elses_discussion_records_the_like()
    {
        var post = NewPost();

        post.Like(Reader, Now);

        post.Likes.Should().ContainSingle(like => like.UserId == Reader);
    }

    [Fact]
    public void Liking_your_own_discussion_is_refused()
    {
        var post = NewPost();

        var act = () => post.Like(Author, Now);

        act.Should().Throw<DomainException>()
            .Which.Error.Should().Be(DomainError.RuleViolation);
    }

    [Fact]
    public void Liking_the_same_discussion_twice_is_refused()
    {
        var post = NewPost();
        post.Like(Reader, Now);

        var act = () => post.Like(Reader, Now);

        act.Should().Throw<DomainException>()
            .Which.Error.Should().Be(DomainError.Conflict);
    }

    [Fact]
    public void Unliking_removes_the_like_and_reports_nothing_when_there_was_none()
    {
        var post = NewPost();
        post.Like(Reader, Now);

        post.Unlike(Reader).Should().NotBeNull();
        post.Likes.Should().BeEmpty();
        post.Unlike(Reader).Should().BeNull();
    }

    [Fact]
    public void Flagging_twice_is_refused()
    {
        var post = NewPost();
        post.Flag(Reader, ModerationTag.MisleadingOrFalse, Now);

        var act = () => post.Flag(Reader, ModerationTag.MisleadingOrFalse, Now);

        act.Should().Throw<DomainException>()
            .Which.Error.Should().Be(DomainError.Conflict);
    }

    [Fact]
    public void Unflagging_removes_the_flag_and_reports_nothing_when_it_was_absent()
    {
        var post = NewPost();
        post.Flag(Reader, ModerationTag.MisleadingOrFalse, Now);

        post.Unflag(ModerationTag.MisleadingOrFalse).Should().NotBeNull();
        post.Tags.Should().BeEmpty();
        post.Unflag(ModerationTag.MisleadingOrFalse).Should().BeNull();
    }

    [Fact]
    public void Editing_someone_elses_discussion_is_refused()
    {
        var post = NewPost();

        var act = () => post.Update(Reader, "Hijacked", "Not mine.", Now);

        act.Should().Throw<DomainException>()
            .Which.Error.Should().Be(DomainError.Forbidden);
    }

    [Fact]
    public void Editing_your_own_discussion_records_when_it_changed()
    {
        var post = NewPost();
        var later = Now.AddHours(1);

        post.Update(Author, "  Webhook retries, revisited  ", "  Now with numbers.  ", later);

        post.Title.Should().Be("Webhook retries, revisited");
        post.Body.Should().Be("Now with numbers.");
        post.UpdatedAt.Should().Be(later);
    }

    [Theory]
    [InlineData("", "A body")]
    [InlineData("   ", "A body")]
    public void A_discussion_needs_a_title(string title, string body)
    {
        var act = () => Post.Create(Author, title, body, Now);

        act.Should().Throw<DomainException>()
            .Which.Error.Should().Be(DomainError.RuleViolation);
    }

    [Fact]
    public void A_title_longer_than_the_limit_is_refused()
    {
        var act = () => Post.Create(Author, new string('x', Post.TitleMaxLength + 1), "A body", Now);

        act.Should().Throw<DomainException>()
            .WithMessage($"*{Post.TitleMaxLength}*");
    }
}

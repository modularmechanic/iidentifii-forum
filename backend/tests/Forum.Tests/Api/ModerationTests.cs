using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Forum.Application.Common.Models;
using Forum.Application.Dtos;
using Forum.Domain.Posts;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Forum.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class ModerationTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly SignedInMembers _members = new(factory, factory.CreateClient());

    [Fact]
    public async Task A_moderator_can_flag_a_discussion()
    {
        var author = await _members.CreateAsync();
        var moderator = await _members.ModeratorAsync();
        var post = await StartDiscussionAsync(author, "Possibly wrong", "Something to check.");

        var response = await FlagAsync(moderator, post.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var flagged = await _client.GetFromJsonAsync<PostDto>($"/api/v1/posts/{post.Id}", TestJson.Options);
        flagged!.Tags.Should().ContainSingle();
        flagged.Tags[0].Tag.Should().Be(ModerationTag.MisleadingOrFalse);
        flagged.Tags[0].TaggedByUsername.Should().Be("mod");
    }

    [Fact]
    public async Task A_member_cannot_flag_a_discussion()
    {
        var author = await _members.CreateAsync();
        var member = await _members.CreateAsync();
        var post = await StartDiscussionAsync(author, "Not yours to mark", "A member tries to flag.");

        var response = await FlagAsync(member, post.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Flagging_without_a_session_is_refused()
    {
        var author = await _members.CreateAsync();
        var post = await StartDiscussionAsync(author, "Anonymous flagging", "Nobody is signed in.");

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/posts/{post.Id}/tags",
            new FlagPostRequest { Tag = ModerationTag.MisleadingOrFalse },
            TestJson.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task The_same_flag_twice_is_refused()
    {
        var author = await _members.CreateAsync();
        var moderator = await _members.ModeratorAsync();
        var post = await StartDiscussionAsync(author, "Once is enough", "Flagged twice.");

        (await FlagAsync(moderator, post.Id)).StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await FlagAsync(moderator, post.Id);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>(TestJson.Options);
        problem!.Detail.Should().Contain("already carries");
    }

    [Fact]
    public async Task A_flag_can_be_taken_off()
    {
        var author = await _members.CreateAsync();
        var moderator = await _members.ModeratorAsync();
        var post = await StartDiscussionAsync(author, "Cleared", "Flagged and then cleared.");

        await FlagAsync(moderator, post.Id);

        using var request = moderator.Request(
            HttpMethod.Delete,
            $"/api/v1/posts/{post.Id}/tags/MisleadingOrFalse");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var cleared = await _client.GetFromJsonAsync<PostDto>($"/api/v1/posts/{post.Id}", TestJson.Options);
        cleared!.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task Taking_off_a_flag_that_is_not_there_is_reported_as_missing()
    {
        var author = await _members.CreateAsync();
        var moderator = await _members.ModeratorAsync();
        var post = await StartDiscussionAsync(author, "Never flagged", "Nothing to clear.");

        using var request = moderator.Request(
            HttpMethod.Delete,
            $"/api/v1/posts/{post.Id}/tags/MisleadingOrFalse");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_flagged_discussion_is_found_by_filtering_for_the_flag()
    {
        var author = await _members.CreateAsync();
        var moderator = await _members.ModeratorAsync();
        var post = await StartDiscussionAsync(author, "Findable when flagged", "Filter should find it.");
        var untouched = await StartDiscussionAsync(author, "Not flagged", "This one stays out of it.");

        await FlagAsync(moderator, post.Id);

        var page = await _client.GetFromJsonAsync<PagedResult<PostDto>>(
            "/api/v1/posts?tag=MisleadingOrFalse&pageSize=100",
            TestJson.Options);

        page!.Items.Should().Contain(candidate => candidate.Id == post.Id);

        // Without a control the assertion above passes even when the filter is ignored entirely.
        page.Items.Should().NotContain(candidate => candidate.Id == untouched.Id);
        page.Items.Should().OnlyContain(
            candidate => candidate.Tags.Any(tag => tag.Tag == ModerationTag.MisleadingOrFalse));
    }

    [Fact]
    public async Task An_author_can_rewrite_their_own_discussion()
    {
        var author = await _members.CreateAsync();
        var post = await StartDiscussionAsync(author, "First wording", "The original body.");

        using var request = author.Request(
            HttpMethod.Put,
            $"/api/v1/posts/{post.Id}",
            new CreatePostRequest { Title = "Second wording", Body = "The corrected body." });
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PostDto>(TestJson.Options);
        updated!.Title.Should().Be("Second wording");
        updated.Body.Should().Be("The corrected body.", "the body was rewritten too");
        updated.UpdatedAt.Should().NotBeNull("an edited discussion says that it was edited");
    }

    [Fact]
    public async Task Somebody_elses_discussion_cannot_be_rewritten()
    {
        var author = await _members.CreateAsync();
        var stranger = await _members.CreateAsync();
        var post = await StartDiscussionAsync(author, "Mine", "Not yours to change.");

        using var request = stranger.Request(
            HttpMethod.Put,
            $"/api/v1/posts/{post.Id}",
            new CreatePostRequest { Title = "Hijacked", Body = "Changed by somebody else." });
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_moderator_is_not_an_author()
    {
        var author = await _members.CreateAsync();
        var moderator = await _members.ModeratorAsync();
        var post = await StartDiscussionAsync(author, "Flag it, do not rewrite it", "Moderation is not editing.");

        using var request = moderator.Request(
            HttpMethod.Put,
            $"/api/v1/posts/{post.Id}",
            new CreatePostRequest { Title = "Rewritten by a moderator", Body = "Should be refused." });
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_author_can_remove_their_own_discussion_and_everything_it_carried()
    {
        var author = await _members.CreateAsync();
        var reader = await _members.CreateAsync();
        var moderator = await _members.ModeratorAsync();
        var post = await StartDiscussionAsync(author, "Going away", "With a reply, a like and a flag.");

        await ReplyAsync(reader, post.Id, "A reply that should go with it.");
        using (var like = reader.Request(HttpMethod.Post, $"/api/v1/posts/{post.Id}/like"))
        {
            (await _client.SendAsync(like)).StatusCode.Should().Be(HttpStatusCode.Created);
        }

        await FlagAsync(moderator, post.Id);

        using var request = author.Request(HttpMethod.Delete, $"/api/v1/posts/{post.Id}");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The replies, likes and flags go by cascade; asking for any of them now is a 404.
        (await _client.GetAsync($"/api/v1/posts/{post.Id}")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
        (await _client.GetAsync($"/api/v1/posts/{post.Id}/comments")).StatusCode
            .Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Somebody_elses_discussion_cannot_be_removed()
    {
        var author = await _members.CreateAsync();
        var stranger = await _members.CreateAsync();
        var post = await StartDiscussionAsync(author, "Still mine", "Not yours to delete.");

        using var request = stranger.Request(HttpMethod.Delete, $"/api/v1/posts/{post.Id}");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        (await _client.GetAsync($"/api/v1/posts/{post.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task An_author_can_rewrite_and_remove_their_own_reply()
    {
        var author = await _members.CreateAsync();
        var responder = await _members.CreateAsync();
        var post = await StartDiscussionAsync(author, "With replies", "One reply, edited then removed.");
        var reply = await ReplyAsync(responder, post.Id, "First wording.");

        using (var edit = responder.Request(
            HttpMethod.Put,
            $"/api/v1/comments/{reply.Id}",
            new CreateCommentRequest { Body = "Second wording." }))
        {
            var edited = await _client.SendAsync(edit);
            edited.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await edited.Content.ReadFromJsonAsync<CommentDto>(TestJson.Options);
            body!.Body.Should().Be("Second wording.");
            body.UpdatedAt.Should().NotBeNull();
        }

        using var remove = responder.Request(HttpMethod.Delete, $"/api/v1/comments/{reply.Id}");
        (await _client.SendAsync(remove)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var replies = await _client.GetFromJsonAsync<PagedResult<CommentDto>>(
            $"/api/v1/posts/{post.Id}/comments",
            TestJson.Options);
        replies!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Somebody_elses_reply_cannot_be_changed()
    {
        var author = await _members.CreateAsync();
        var responder = await _members.CreateAsync();
        var stranger = await _members.CreateAsync();
        var post = await StartDiscussionAsync(author, "Whose reply", "Not the stranger's.");
        var reply = await ReplyAsync(responder, post.Id, "Mine.");

        using var edit = stranger.Request(
            HttpMethod.Put,
            $"/api/v1/comments/{reply.Id}",
            new CreateCommentRequest { Body = "Not mine, but changed anyway." });
        (await _client.SendAsync(edit)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var remove = stranger.Request(HttpMethod.Delete, $"/api/v1/comments/{reply.Id}");
        (await _client.SendAsync(remove)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<PostDto> StartDiscussionAsync(Member member, string title, string body)
    {
        using var request = member.Request(
            HttpMethod.Post,
            "/api/v1/posts",
            new CreatePostRequest { Title = title, Body = body });
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<PostDto>(TestJson.Options))!;
    }

    private async Task<CommentDto> ReplyAsync(Member member, Guid postId, string body)
    {
        using var request = member.Request(
            HttpMethod.Post,
            $"/api/v1/posts/{postId}/comments",
            new CreateCommentRequest { Body = body });
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<CommentDto>(TestJson.Options))!;
    }

    private async Task<HttpResponseMessage> FlagAsync(Member member, Guid postId)
    {
        using var request = member.Request(
            HttpMethod.Post,
            $"/api/v1/posts/{postId}/tags",
            new FlagPostRequest { Tag = ModerationTag.MisleadingOrFalse });

        return await _client.SendAsync(request);
    }
}

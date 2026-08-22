using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Forum.Application.Common.Models;
using Forum.Application.Dtos;
using Forum.Application.Services;
using Forum.Domain.Posts;
using Forum.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Forum.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class ContentTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly SignedInMembers _members = new(factory, factory.CreateClient());

    [Fact]
    public async Task A_member_can_start_a_discussion()
    {
        var member = await _members.CreateAsync();

        var post = await StartDiscussionAsync(member, "Webhook retries", "How many times does it retry?");

        post.Title.Should().Be("Webhook retries");
        post.Author.Username.Should().Be(member.Username);
        post.LikeCount.Should().Be(0);
        post.CommentCount.Should().Be(0);
        post.LikedByMe.Should().BeFalse();
    }

    [Fact]
    public async Task A_new_discussion_can_be_read_back_at_the_address_it_reports()
    {
        var member = await _members.CreateAsync();
        var post = await StartDiscussionAsync(member, "Reading it back", "The location header should work.");

        var response = await _client.GetAsync($"/api/v1/posts/{post.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await response.Content.ReadFromJsonAsync<PostDto>(TestJson.Options);
        fetched!.Title.Should().Be("Reading it back");
    }

    [Fact]
    public async Task A_member_can_reply()
    {
        var author = await _members.CreateAsync();
        var responder = await _members.CreateAsync();
        var post = await StartDiscussionAsync(author, "A question", "Does anybody know?");

        using var request = responder.Request(
            HttpMethod.Post,
            $"/api/v1/posts/{post.Id}/comments",
            new CreateCommentRequest { Body = "Yes, here is how." });
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var reply = await response.Content.ReadFromJsonAsync<CommentDto>(TestJson.Options);
        reply!.Body.Should().Be("Yes, here is how.");
        reply.Author.Username.Should().Be(responder.Username);
        reply.PostId.Should().Be(post.Id);
    }

    [Fact]
    public async Task A_reply_appears_in_the_discussion_and_in_its_count()
    {
        var author = await _members.CreateAsync();
        var responder = await _members.CreateAsync();
        var post = await StartDiscussionAsync(author, "Counting replies", "One reply is coming.");

        await ReplyAsync(responder, post.Id, "Here it is.");

        var replies = await _client.GetFromJsonAsync<PagedResult<CommentDto>>(
            $"/api/v1/posts/{post.Id}/comments",
            TestJson.Options);
        replies!.TotalCount.Should().Be(1);

        var fetched = await _client.GetFromJsonAsync<PostDto>($"/api/v1/posts/{post.Id}", TestJson.Options);
        fetched!.CommentCount.Should().Be(1);
    }

    [Fact]
    public async Task A_member_can_like_somebody_elses_discussion()
    {
        var author = await _members.CreateAsync();
        var reader = await _members.CreateAsync();
        var post = await StartDiscussionAsync(author, "Worth a like", "Something useful.");

        var response = await LikeAsync(reader, post.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var fetched = await _client.GetFromJsonAsync<PostDto>($"/api/v1/posts/{post.Id}", TestJson.Options);
        fetched!.LikeCount.Should().Be(1);
    }

    [Fact]
    public async Task The_like_is_reported_back_to_the_member_who_gave_it_and_to_nobody_else()
    {
        var author = await _members.CreateAsync();
        var reader = await _members.CreateAsync();
        var onlooker = await _members.CreateAsync();
        var post = await StartDiscussionAsync(author, "Whose like", "Only one of us liked this.");

        (await LikeAsync(reader, post.Id)).StatusCode.Should().Be(HttpStatusCode.Created);

        (await ReadAsAsync(reader, post.Id)).LikedByMe.Should().BeTrue();
        (await ReadAsAsync(onlooker, post.Id)).LikedByMe.Should().BeFalse();

        var anonymous = await _client.GetFromJsonAsync<PostDto>(
            $"/api/v1/posts/{post.Id}",
            TestJson.Options);
        anonymous!.LikedByMe.Should().BeFalse("nobody is signed in, so nobody has liked it");
        anonymous.LikeCount.Should().Be(1, "the count is public even when the reader is not");
    }

    [Fact]
    public async Task Liking_the_same_discussion_twice_is_refused()
    {
        var author = await _members.CreateAsync();
        var reader = await _members.CreateAsync();
        var post = await StartDiscussionAsync(author, "Once only", "You may like this once.");

        (await LikeAsync(reader, post.Id)).StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await LikeAsync(reader, post.Id);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>(TestJson.Options);
        problem!.Detail.Should().Contain("already liked");
    }

    [Fact]
    public async Task Two_likes_arriving_together_still_leave_one()
    {
        var author = await _members.CreateAsync();
        var reader = await _members.CreateAsync();
        var post = await StartDiscussionAsync(author, "At the same moment", "Two requests, one like.");

        // Checking and then inserting would let both requests through; the unique index is what
        // actually decides, and this proves the loser is reported as a conflict rather than a 500.
        var responses = await Task.WhenAll(
            LikeAsync(reader, post.Id),
            LikeAsync(reader, post.Id));

        responses.Count(response => response.StatusCode == HttpStatusCode.Created).Should().Be(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).Should().Be(1);

        var fetched = await _client.GetFromJsonAsync<PostDto>($"/api/v1/posts/{post.Id}", TestJson.Options);
        fetched!.LikeCount.Should().Be(1);
    }

    [Fact]
    public async Task Liking_your_own_discussion_is_refused()
    {
        var author = await _members.CreateAsync();
        var post = await StartDiscussionAsync(author, "My own", "I should not be able to like this.");

        var response = await LikeAsync(author, post.Id);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestJson.Options);
        problem!.Detail.Should().Contain("your own");
    }

    [Fact]
    public async Task A_like_can_be_taken_back()
    {
        var author = await _members.CreateAsync();
        var reader = await _members.CreateAsync();
        var post = await StartDiscussionAsync(author, "Changed my mind", "Liked, then not.");

        await LikeAsync(reader, post.Id);

        using var request = reader.Request(HttpMethod.Delete, $"/api/v1/posts/{post.Id}/like");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ReadAsAsync(reader, post.Id)).LikeCount.Should().Be(0);
    }

    [Fact]
    public async Task Taking_back_a_like_that_was_never_given_is_reported_as_missing()
    {
        var author = await _members.CreateAsync();
        var reader = await _members.CreateAsync();
        var post = await StartDiscussionAsync(author, "Never liked", "There is nothing to remove.");

        using var request = reader.Request(HttpMethod.Delete, $"/api/v1/posts/{post.Id}/like");
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Liking_a_discussion_that_does_not_exist_is_reported_as_missing()
    {
        var member = await _members.CreateAsync();

        var response = await LikeAsync(member, Guid.CreateVersion7());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Replying_to_a_discussion_that_does_not_exist_is_reported_as_missing()
    {
        var member = await _members.CreateAsync();

        using var request = member.Request(
            HttpMethod.Post,
            $"/api/v1/posts/{Guid.CreateVersion7()}/comments",
            new CreateCommentRequest { Body = "Anybody there?" });
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("", "A body")]
    [InlineData("A title", "")]
    [InlineData("   ", "A body")]
    public async Task A_discussion_needs_both_a_title_and_a_body(string title, string body)
    {
        var member = await _members.CreateAsync();

        using var request = member.Request(
            HttpMethod.Post,
            "/api/v1/posts",
            new CreatePostRequest { Title = title, Body = body });
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_title_longer_than_the_limit_is_refused()
    {
        var member = await _members.CreateAsync();

        using var request = member.Request(
            HttpMethod.Post,
            "/api/v1/posts",
            new CreatePostRequest { Title = new string('x', 201), Body = "Short enough." });
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("POST", "/api/v1/posts")]
    [InlineData("POST", "/api/v1/posts/{id}/comments")]
    [InlineData("POST", "/api/v1/posts/{id}/like")]
    [InlineData("DELETE", "/api/v1/posts/{id}/like")]
    public async Task Writing_anything_without_a_session_is_refused(string method, string path)
    {
        var author = await _members.CreateAsync();
        var post = await StartDiscussionAsync(author, "Read only", "Anonymous readers cannot act.");

        using var request = new HttpRequestMessage(
            new HttpMethod(method),
            path.Replace("{id}", post.Id.ToString()))
        {
            Content = JsonContent.Create(
                new CreateCommentRequest { Body = "Trying anyway." },
                options: TestJson.Options),
        };
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task The_rule_against_liking_twice_survives_a_popular_discussion()
    {
        var (post, newcomer) = await PopularDiscussionAsync();

        (await LikeAsync(newcomer, post.Id)).StatusCode.Should().Be(HttpStatusCode.Created);

        var fetched = await _client.GetFromJsonAsync<PostDto>($"/api/v1/posts/{post.Id}", TestJson.Options);
        fetched!.LikeCount.Should().Be(11);

        (await LikeAsync(newcomer, post.Id)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// The service is called directly here because the saving is invisible from outside: the
    /// answer is the same either way, and only the context that ran the query can say how many
    /// rows it loaded to produce it.
    /// </summary>
    [Fact]
    public async Task Liking_a_popular_discussion_loads_only_the_callers_own_like()
    {
        var (post, newcomer) = await PopularDiscussionAsync();

        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ForumDbContext>();

        await scope.ServiceProvider.GetRequiredService<PostService>()
            .LikeAsync(post.Id, newcomer.Id, CancellationToken.None);

        // The one just added, and nothing else. Loading the whole collection would leave eleven.
        LikesLoadedFor(database, post.Id).Should().Be(1);
    }

    [Fact]
    public async Task Unliking_a_popular_discussion_loads_only_the_callers_own_like()
    {
        var (post, admirer) = await PopularDiscussionAsync();
        (await LikeAsync(admirer, post.Id)).StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ForumDbContext>();

        await scope.ServiceProvider.GetRequiredService<PostService>()
            .UnlikeAsync(post.Id, admirer.Id, CancellationToken.None);

        // Deleting a row detaches it, so the caller's own like is gone and the other ten were
        // never read. Loading the whole collection would leave those ten behind.
        LikesLoadedFor(database, post.Id).Should().Be(0);
    }

    /// <summary>A discussion ten other members have liked, and a member who has not.</summary>
    private async Task<(PostDto Post, Member Newcomer)> PopularDiscussionAsync()
    {
        var author = await _members.CreateAsync();
        var post = await StartDiscussionAsync(author, "Popular", "Plenty of people liked this.");

        for (var i = 0; i < 10; i++)
        {
            var admirer = await _members.CreateAsync();
            (await LikeAsync(admirer, post.Id)).StatusCode.Should().Be(HttpStatusCode.Created);
        }

        return (post, await _members.CreateAsync());
    }

    /// <summary>How many of a discussion's likes the context read while doing its work.</summary>
    private static int LikesLoadedFor(ForumDbContext database, Guid postId)
        => database.ChangeTracker.Entries<PostLike>()
            .Count(entry => entry.Entity.PostId == postId);

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

    private async Task ReplyAsync(Member member, Guid postId, string body)
    {
        using var request = member.Request(
            HttpMethod.Post,
            $"/api/v1/posts/{postId}/comments",
            new CreateCommentRequest { Body = body });

        (await _client.SendAsync(request)).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<HttpResponseMessage> LikeAsync(Member member, Guid postId)
    {
        using var request = member.Request(HttpMethod.Post, $"/api/v1/posts/{postId}/like");

        return await _client.SendAsync(request);
    }

    private async Task<PostDto> ReadAsAsync(Member member, Guid postId)
    {
        using var request = member.Request(HttpMethod.Get, $"/api/v1/posts/{postId}");
        var response = await _client.SendAsync(request);

        return (await response.Content.ReadFromJsonAsync<PostDto>(TestJson.Options))!;
    }
}

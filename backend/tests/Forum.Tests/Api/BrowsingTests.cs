using FluentAssertions;
using Forum.Application.Common.Models;
using Forum.Application.Dtos;
using Forum.Domain.Posts;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Net;
using Xunit;

namespace Forum.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class BrowsingTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task The_first_page_returns_seeded_discussions_newest_first()
    {
        var page = await GetPageAsync("?page=1&pageSize=5");

        page.Items.Should().HaveCount(5);
        page.TotalCount.Should().BeGreaterThan(5);
        page.Page.Should().Be(1);
        page.HasPrevious.Should().BeFalse();
        page.HasNext.Should().BeTrue();
        page.Items.Should().BeInDescendingOrder(post => post.CreatedAt);
    }

    [Fact]
    public async Task Every_discussion_reports_its_author_and_counts()
    {
        var page = await GetPageAsync("?pageSize=20");

        page.Items.Should().OnlyContain(post => post.Author.Username.Length > 0);
        page.Items.Should().OnlyContain(post => post.LikeCount >= 0 && post.CommentCount >= 0);
        page.Items.Should().Contain(post => post.CommentCount > 0);
        page.Items.Should().Contain(post => post.LikeCount > 0);
    }

    [Fact]
    public async Task Anonymous_readers_are_never_reported_as_having_liked_anything()
    {
        var page = await GetPageAsync("?pageSize=20");

        page.Items.Should().OnlyContain(post => !post.LikedByMe);
    }

    [Fact]
    public async Task Seed_data_includes_flagged_discussions_naming_the_moderator()
    {
        var page = await GetPageAsync("?pageSize=100");

        var flagged = page.Items.Where(post => post.Tags.Count > 0).ToList();

        flagged.Should().NotBeEmpty();
        flagged.Should().OnlyContain(post =>
            post.Tags.All(tag => tag.Tag == ModerationTag.MisleadingOrFalse && tag.TaggedByUsername == "mod"));
    }

    [Fact]
    public async Task Paging_covers_every_discussion_exactly_once()
    {
        var first = await GetPageAsync("?page=1&pageSize=7");
        var second = await GetPageAsync("?page=2&pageSize=7");
        var third = await GetPageAsync("?page=3&pageSize=7");

        var seen = first.Items.Concat(second.Items).Concat(third.Items).Select(post => post.Id).ToList();

        seen.Should().OnlyHaveUniqueItems();
        seen.Should().HaveCount(Math.Min(21, first.TotalCount));
    }

    [Fact]
    public async Task A_discussion_can_be_read_on_its_own()
    {
        var page = await GetPageAsync("?pageSize=1");
        var expected = page.Items[0];

        var post = await _client.GetFromJsonAsync<PostDto>($"/api/v1/posts/{expected.Id}", TestJson.Options);

        post!.Id.Should().Be(expected.Id);
        post.Body.Should().NotBeEmpty();
        post.Author.Username.Should().Be(expected.Author.Username);
    }

    [Fact]
    public async Task An_unknown_discussion_reports_that_it_was_not_found()
    {
        var response = await _client.GetAsync($"/api/v1/posts/{Guid.CreateVersion7()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestJson.Options);
        problem!.Status.Should().Be(404);
    }

    [Fact]
    public async Task An_identifier_that_is_not_a_guid_does_not_match_the_route()
    {
        var response = await _client.GetAsync("/api/v1/posts/not-a-guid");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_page_number_beyond_the_limit_is_refused_rather_than_failing()
    {
        var response = await _client.GetAsync($"/api/v1/posts?page={int.MaxValue}&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("?page=0", "Page")]
    [InlineData("?page=2147483647", "Page")]
    [InlineData("?pageSize=0", "PageSize")]
    [InlineData("?pageSize=101", "PageSize")]
    public async Task Out_of_range_paging_is_refused_with_the_offending_field(string queryString, string field)
    {
        var response = await _client.GetAsync($"/api/v1/posts{queryString}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJson.Options);
        problem!.Errors.Should().ContainKey(field);
        problem.Extensions.Should().ContainKey("traceId");
    }

    [Fact]
    public async Task Replies_are_returned_oldest_first_for_one_discussion()
    {
        var page = await GetPageAsync("?pageSize=20");
        var discussion = page.Items.First(post => post.CommentCount > 1);

        var replies = await _client.GetFromJsonAsync<PagedResult<CommentDto>>(
            $"/api/v1/posts/{discussion.Id}/comments",
            TestJson.Options);

        replies!.TotalCount.Should().Be(discussion.CommentCount);
        replies.Items.Should().BeInAscendingOrder(comment => comment.CreatedAt);
        replies.Items.Should().OnlyContain(comment => comment.PostId == discussion.Id);
        replies.Items.Should().OnlyContain(comment => comment.Author.Username.Length > 0);
    }

    [Fact]
    public async Task Replies_to_an_unknown_discussion_report_that_it_was_not_found()
    {
        var response = await _client.GetAsync($"/api/v1/posts/{Guid.CreateVersion7()}/comments");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<PagedResult<PostDto>> GetPageAsync(string queryString)
        => (await _client.GetFromJsonAsync<PagedResult<PostDto>>($"/api/v1/posts{queryString}", TestJson.Options))!;
}

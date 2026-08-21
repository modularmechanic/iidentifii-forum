using FluentAssertions;
using Forum.Application.Common.Models;
using Forum.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Net;
using Xunit;

namespace Forum.Tests.Api;

[Collection(ApiCollection.Name)]
public sealed class FilteringAndSortingTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Filtering_by_author_returns_only_their_discussions()
    {
        var page = await GetPageAsync("?author=alice&pageSize=100");

        page.Items.Should().NotBeEmpty();
        page.Items.Should().OnlyContain(post => post.Author.Username == "alice");
        page.TotalCount.Should().Be(page.Items.Count);
    }

    [Fact]
    public async Task Filtering_by_author_ignores_the_case_of_the_name()
    {
        var lower = await GetPageAsync("?author=alice&pageSize=100");
        var upper = await GetPageAsync("?author=ALICE&pageSize=100");

        upper.TotalCount.Should().Be(lower.TotalCount).And.BeGreaterThan(0);
    }

    [Fact]
    public async Task Filtering_by_an_unknown_author_returns_an_empty_page_rather_than_failing()
    {
        var page = await GetPageAsync("?author=nobody&pageSize=100");

        page.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(0);
        page.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Filtering_by_flag_returns_only_flagged_discussions()
    {
        var page = await GetPageAsync("?tag=MisleadingOrFalse&pageSize=100");

        page.Items.Should().NotBeEmpty();
        page.Items.Should().OnlyContain(post => post.Tags.Count > 0);
    }

    [Fact]
    public async Task A_date_range_excludes_discussions_outside_it()
    {
        var all = await GetPageAsync("?pageSize=100");
        var newest = all.Items.Max(post => post.CreatedAt);
        var from = DateOnly.FromDateTime(newest.UtcDateTime.AddDays(-3));

        var page = await GetPageAsync($"?from={from:yyyy-MM-dd}&pageSize=100");

        page.Items.Should().NotBeEmpty();
        page.Items.Should().OnlyContain(post => DateOnly.FromDateTime(post.CreatedAt.UtcDateTime) >= from);
        page.TotalCount.Should().BeLessThan(all.TotalCount);
    }

    [Fact]
    public async Task The_end_of_a_range_includes_the_whole_of_that_day()
    {
        var all = await GetPageAsync("?pageSize=100");
        var newest = all.Items.Max(post => post.CreatedAt);
        var day = DateOnly.FromDateTime(newest.UtcDateTime);

        var page = await GetPageAsync($"?from={day:yyyy-MM-dd}&to={day:yyyy-MM-dd}&pageSize=100");

        page.Items.Should().Contain(post => post.CreatedAt == newest);
    }

    /// <summary>
    /// The last representable day has no next day to bound against. Nothing can be later than it,
    /// so the filter must simply match everything rather than failing.
    /// </summary>
    [Fact]
    public async Task The_last_representable_day_is_a_usable_upper_bound()
    {
        var all = await GetPageAsync("?pageSize=100");

        var page = await GetPageAsync("?to=9999-12-31&pageSize=100");

        page.TotalCount.Should().Be(all.TotalCount);
    }

    [Fact]
    public async Task The_first_representable_day_is_a_usable_lower_bound()
    {
        var all = await GetPageAsync("?pageSize=100");

        var page = await GetPageAsync("?from=0001-01-01&pageSize=100");

        page.TotalCount.Should().Be(all.TotalCount);
    }

    [Theory]
    [InlineData("Descending")]
    [InlineData("Ascending")]
    public async Task Discussions_can_be_ordered_by_when_they_started(string order)
    {
        var page = await GetPageAsync($"?sort=CreatedAt&order={order}&pageSize=100");

        var dates = page.Items.Select(post => post.CreatedAt).ToList();
        _ = order == "Ascending"
            ? dates.Should().BeInAscendingOrder()
            : dates.Should().BeInDescendingOrder();
    }

    [Theory]
    [InlineData("Descending")]
    [InlineData("Ascending")]
    public async Task Discussions_can_be_ordered_by_how_many_people_liked_them(string order)
    {
        var page = await GetPageAsync($"?sort=LikeCount&order={order}&pageSize=100");

        var counts = page.Items.Select(post => post.LikeCount).ToList();
        counts.Distinct().Should().HaveCountGreaterThan(1, "the sample content should vary");
        _ = order == "Ascending"
            ? counts.Should().BeInAscendingOrder()
            : counts.Should().BeInDescendingOrder();
    }

    /// <summary>
    /// Many discussions share a like count, so ordering by it alone would be ambiguous and a
    /// discussion could appear on two pages or on none.
    /// </summary>
    [Fact]
    public async Task Paging_by_like_count_covers_every_discussion_exactly_once()
    {
        var all = await GetPageAsync("?pageSize=100");

        var seen = new List<Guid>();
        for (var page = 1; page <= 3; page++)
        {
            var result = await GetPageAsync($"?sort=LikeCount&order=Descending&page={page}&pageSize=7");
            seen.AddRange(result.Items.Select(post => post.Id));
        }

        seen.Should().OnlyHaveUniqueItems();
        seen.Should().HaveCount(Math.Min(21, all.TotalCount));
    }

    [Fact]
    public async Task Filters_combine_rather_than_replace_one_another()
    {
        var byAuthor = await GetPageAsync("?author=carol&pageSize=100");
        var combined = await GetPageAsync("?author=carol&tag=MisleadingOrFalse&pageSize=100");

        combined.Items.Should().OnlyContain(post => post.Author.Username == "carol" && post.Tags.Count > 0);
        combined.TotalCount.Should().BeLessThanOrEqualTo(byAuthor.TotalCount);
    }

    [Fact]
    public async Task A_range_that_ends_before_it_starts_is_refused()
    {
        var response = await _client.GetAsync("/api/v1/posts?from=2026-08-20&to=2026-08-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJson.Options);
        problem!.Errors.Keys.Should().IntersectWith(["From", "To"]);
    }

    [Theory]
    [InlineData("?sort=Nonsense")]
    [InlineData("?order=Sideways")]
    [InlineData("?tag=NotATag")]
    [InlineData("?from=not-a-date")]
    public async Task An_unrecognised_value_is_refused(string queryString)
    {
        var response = await _client.GetAsync($"/api/v1/posts{queryString}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task An_author_name_longer_than_a_username_can_be_is_refused()
    {
        var response = await _client.GetAsync($"/api/v1/posts?author={new string('x', 33)}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Replies_can_be_read_newest_first()
    {
        var page = await GetPageAsync("?pageSize=20");
        var discussion = page.Items.First(post => post.CommentCount > 1);

        var replies = await _client.GetFromJsonAsync<PagedResult<CommentDto>>(
            $"/api/v1/posts/{discussion.Id}/comments?order=Descending",
            TestJson.Options);

        replies!.Items.Should().BeInDescendingOrder(comment => comment.CreatedAt);
    }

    private async Task<PagedResult<PostDto>> GetPageAsync(string queryString)
        => (await _client.GetFromJsonAsync<PagedResult<PostDto>>($"/api/v1/posts{queryString}", TestJson.Options))!;
}

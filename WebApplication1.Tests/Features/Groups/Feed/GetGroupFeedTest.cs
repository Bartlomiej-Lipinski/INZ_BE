using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Features.Groups.Feed;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Groups.Feed;

public class GetGroupFeedTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_FeedItems()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var feed1 = TestDataFactory.CreateGroupFeedItem(
            "1", "g1", "Post 1", "user1", DateTime.UtcNow.AddMinutes(-10));
        var feed2 = TestDataFactory.CreateGroupFeedItem(
            "2", "g1", "Post 2", "user1", DateTime.UtcNow);
        dbContext.GroupFeedItems.AddRange(feed1, feed2);
        await dbContext.SaveChangesAsync();

        var result = await GetGroupFeed.Handle(
            "g1",
            1,
            10,
            dbContext,
            CreateHttpContext("user1"),
            NullLogger<GetGroupFeed>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<PagedApiResponse<GroupFeedItemResponseDto>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<PagedApiResponse<GroupFeedItemResponseDto>>;
        ok!.Value!.Data?.Count.Should().Be(2);
        ok.Value.Data?[0].Id.Should().Be(feed2.Id);
        ok.Value.Data?[1].Id.Should().Be(feed1.Id);
    }

    [Fact]
    public async Task Handle_Should_Return_Empty_When_No_Items()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());

        var result = await GetGroupFeed.Handle(
            "g1",
            page: 1,
            pageSize: 10,
            dbContext,
            CreateHttpContext("user1"),
            NullLogger<GetGroupFeed>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<PagedApiResponse<GroupFeedItemResponseDto>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<PagedApiResponse<GroupFeedItemResponseDto>>;
        ok!.Value!.Data.Should().BeEmpty();
        ok.Value.Message.Should().Be("No feed items found for this group.");
    }
}
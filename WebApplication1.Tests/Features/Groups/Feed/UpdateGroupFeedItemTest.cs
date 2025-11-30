using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebApplication1.Features.Groups.Feed;
using WebApplication1.Infrastructure.Service;

namespace WebApplication1.Tests.Features.Groups.Feed;

public class UpdateGroupFeedItemTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Update_Text()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var mockStorage = new Mock<IStorageService>();
        var feedItem = TestDataFactory.CreateGroupFeedItem("1", "g1", "Old text", "user1");
        dbContext.GroupFeedItems.Add(feedItem);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateGroupFeedItemRequestDto("Updated text", null);

        var result = await UpdateGroupFeedItem.Handle(
            feedItem.GroupId,
            feedItem.Id,
            request,
            dbContext,
            mockStorage.Object,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<UpdateGroupFeedItem>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<Shared.Responses.ApiResponse<string>>>();
        
        var updated = await dbContext.GroupFeedItems.FindAsync(feedItem.Id);
        updated?.Description.Should().Be("Updated text");
    }

    [Fact]
    public async Task Handle_Should_Update_File()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var mockStorage = new Mock<IStorageService>();
        var feedItem = TestDataFactory.CreateGroupFeedItem("1", "g1", "Text with file", "user1");
        dbContext.GroupFeedItems.Add(feedItem);
        await dbContext.SaveChangesAsync();

        var file = TestDataFactory.CreateFormFile("test.jpg", "abc"u8.ToArray());
        mockStorage.Setup(s => s.SaveFileAsync(It.IsAny<Stream>(), file.FileName, file.ContentType, CancellationToken.None))
                   .ReturnsAsync("/uploads/test.jpg");

        var request = TestDataFactory.CreateGroupFeedItemRequestDto(null, file);

        var result = await UpdateGroupFeedItem.Handle(
            feedItem.GroupId,
            feedItem.Id,
            request,
            dbContext,
            mockStorage.Object,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<UpdateGroupFeedItem>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<Shared.Responses.ApiResponse<string>>>();

        var updated = await dbContext.GroupFeedItems.Include(f => f.StoredFile)
            .FirstAsync(f => f.Id == feedItem.Id);
        updated.StoredFile.Should().NotBeNull();
        updated.StoredFile.FileName.Should().Be("test.jpg");
        updated.StoredFile.Url.Should().Be("/uploads/test.jpg");
    }

    [Fact]
    public async Task Handle_Should_Reject_When_User_Is_Not_Owner()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var mockStorage = new Mock<IStorageService>();
        var feedItem = TestDataFactory.CreateGroupFeedItem("1", "g1", "Text", "ownerUser");
        dbContext.GroupFeedItems.Add(feedItem);
        await dbContext.SaveChangesAsync();

        var request = TestDataFactory.CreateGroupFeedItemRequestDto("New text", null);

        var result = await UpdateGroupFeedItem.Handle(
            feedItem.GroupId,
            feedItem.Id,
            request,
            dbContext,
            mockStorage.Object,
            CreateClaimsPrincipal("otherUser"),
            CreateHttpContext("otherUser"),
            NullLogger<UpdateGroupFeedItem>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }
}
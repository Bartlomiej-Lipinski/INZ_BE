using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebApplication1.Features.Groups.Feed;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Infrastructure.Service;

namespace WebApplication1.Tests.Features.Groups.Feed;

public class DeleteGroupFeedItemTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Delete_FeedItem_And_File()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var mockStorage = new Mock<IStorageService>();
        var feedItem = TestDataFactory.CreateGroupFeedItem("1", "g1", "Test feed", "user1");
        var storedFile = TestDataFactory.CreateStoredFile(
            "file1",
            "g1",
            "test.jpg",
            "image/jpeg",
            1024,
            "/uploads/test.jpg",
            DateTime.UtcNow,
            feedItem.Id,
            EntityType.GroupFeedItem,
            "user1",
            null
        );

        feedItem.StoredFile = storedFile;
        feedItem.StoredFileId = storedFile.Id;
        dbContext.GroupFeedItems.Add(feedItem);
        dbContext.StoredFiles.Add(storedFile);
        await dbContext.SaveChangesAsync();

        var result = await DeleteGroupFeedItem.Handle(
            feedItem.GroupId,
            feedItem.Id,
            dbContext,
            mockStorage.Object,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<DeleteGroupFeedItem>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<Shared.Responses.ApiResponse<string>>>();

        var deletedFeedItem = await dbContext.GroupFeedItems.FindAsync(feedItem.Id);
        deletedFeedItem.Should().BeNull();

        var deletedFile = await dbContext.StoredFiles.FindAsync(storedFile.Id);
        deletedFile.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_If_FeedItem_DoesNotExist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var mockStorage = new Mock<IStorageService>();

        var result = await DeleteGroupFeedItem.Handle(
            "g1",
            "nonexistent",
            dbContext,
            mockStorage.Object,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<DeleteGroupFeedItem>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<Shared.Responses.ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_Forbidden_If_User_Is_Not_Owner()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var mockStorage = new Mock<IStorageService>();
        var feedItem = TestDataFactory.CreateGroupFeedItem("1", "g1", "Test feed", "user1");
        dbContext.GroupFeedItems.Add(feedItem);
        await dbContext.SaveChangesAsync();

        var result = await DeleteGroupFeedItem.Handle(
            feedItem.GroupId,
            feedItem.Id,
            dbContext,
            mockStorage.Object,
            CreateClaimsPrincipal("user2"),
            CreateHttpContext("user2"),
            NullLogger<DeleteGroupFeedItem>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.ForbidHttpResult>();
    }
}
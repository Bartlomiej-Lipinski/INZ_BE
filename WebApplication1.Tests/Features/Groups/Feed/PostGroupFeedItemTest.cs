using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebApplication1.Features.Groups.Feed;
using WebApplication1.Infrastructure.Service;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Groups.Feed;

public class PostGroupFeedItemTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_NoTextAndNoFile()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var mockStorage = new Mock<IStorageService>();

        var request = TestDataFactory.CreateGroupFeedItemRequestDto(null, null);

        var result = await PostGroupFeedItem.Handle(
            "group1",
            request,
            dbContext,
            mockStorage.Object,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<PostGroupFeedItem>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        var badRequest = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        badRequest!.Value!.Success.Should().BeFalse();
        badRequest.Value.Message.Should().Be("Feed item must contain text or a file.");
    }

    [Fact]
    public async Task Handle_Should_Create_FeedItem_With_Text_Only()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var mockStorage = new Mock<IStorageService>();

        var request = TestDataFactory.CreateGroupFeedItemRequestDto("Hello world", null);

        var result = await PostGroupFeedItem.Handle(
            "group1",
            request,
            dbContext,
            mockStorage.Object,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<PostGroupFeedItem>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        ok!.Value!.Success.Should().BeTrue();
        ok.Value.Message.Should().Be("Feed item created successfully.");

        var feedItem = dbContext.GroupFeedItems.FirstOrDefault();
        feedItem.Should().NotBeNull();
        feedItem.Description.Should().Be("Hello world");
        feedItem.StoredFileId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_Create_FeedItem_With_File()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var mockStorage = new Mock<IStorageService>();
        var file = TestDataFactory.CreateFormFile("test.jpg", "abc"u8.ToArray());

        var request = TestDataFactory.CreateGroupFeedItemRequestDto("With file", file);
        mockStorage.Setup(s => s.SaveFileAsync(It.IsAny<Stream>(), file.FileName, file.ContentType, CancellationToken.None))
            .ReturnsAsync("/uploads/test.jpg");
        
        var result = await PostGroupFeedItem.Handle(
            "group1",
            request,
            dbContext,
            mockStorage.Object,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<PostGroupFeedItem>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        ok!.Value!.Success.Should().BeTrue();

        var feedItem = dbContext.GroupFeedItems.Include(f => f.StoredFile).FirstOrDefault();
        feedItem.Should().NotBeNull();
        feedItem.Description.Should().Be("With file");
        feedItem.StoredFile.Should().NotBeNull();
    }
}
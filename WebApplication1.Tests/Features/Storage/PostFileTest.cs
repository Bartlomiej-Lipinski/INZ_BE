using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Storage;
using WebApplication1.Infrastructure.Service;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Storage;

public class PostFileTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_User_Is_Null()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var logger = NullLogger<PostFile>.Instance;
        var mockStorageService = new Mock<IStorageService>();
        var file = TestDataFactory.CreateFormFile("test.jpg", "content"u8.ToArray());

        var result = await PostFile.Handle(
            "testEntity",
            "entity-123",
            file,
            dbContext,
            mockStorageService.Object,
            CreateClaimsPrincipal(),
            httpContext,
            logger,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_File_Is_Null()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = CreateClaimsPrincipal("user1");
        var httpContext = CreateHttpContext("user1");
        var logger = NullLogger<PostFile>.Instance;
        var mockStorageService = new Mock<IStorageService>();

        var result = await PostFile.Handle(
            "testEntity",
            "entity-123",
            null,
            dbContext,
            mockStorageService.Object,
            user,
            httpContext,
            logger,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        var badRequest = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        badRequest!.Value?.Success.Should().BeFalse();
        badRequest.Value?.Message.Should().Be("No file uploaded.");
        badRequest.Value?.TraceId.Should().Be("test-trace-id");
    }

    [Fact]
    public async Task Handle_Should_Upload_File_Successfully()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = CreateClaimsPrincipal("user1");
        var httpContext = CreateHttpContext("user1");
        var logger = NullLogger<PostFile>.Instance;
        var mockStorageService = new Mock<IStorageService>();

        var file = TestDataFactory.CreateFormFile("test.jpg", "content"u8.ToArray());
        const string expectedUrl = "/uploads/profile/test-123.jpg";
        mockStorageService
            .Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), file.FileName, file.ContentType, CancellationToken.None))
            .ReturnsAsync(expectedUrl);

        var result = await PostFile.Handle(
            "testEntity",
            "entity-123",
            file,
            dbContext,
            mockStorageService.Object,
            user,
            httpContext,
            logger,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<PostFile.StoredFileResponseDto>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<PostFile.StoredFileResponseDto>>;
        okResult!.Value?.Success.Should().BeTrue();
        okResult.Value?.Data?.Url.Should().Be(expectedUrl);
        okResult.Value?.Data?.FileName.Should().Be("test.jpg");
        okResult.Value?.Data?.EntityType.Should().Be("testEntity");
        okResult.Value?.Data?.EntityId.Should().Be("entity-123");
        okResult.Value?.Message.Should().Be("File uploaded.");
        okResult.Value?.TraceId.Should().Be("test-trace-id");

        mockStorageService.Verify(
            x => x.SaveFileAsync(It.IsAny<Stream>(), file.FileName, file.ContentType, CancellationToken.None),
            Times.Once);
    }
}
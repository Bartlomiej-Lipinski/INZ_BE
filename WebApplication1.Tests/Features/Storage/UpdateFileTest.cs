using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using WebApplication1.Features.Storage;
using WebApplication1.Infrastructure.Data.Entities.Storage;
using WebApplication1.Infrastructure.Storage;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Storage;

public class UpdateFileTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_User_Is_Null()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var mockLogger = new Mock<ILogger<UpdateFile>>();
        var mockStorageService = new Mock<IStorageService>();
        var file = CreateFormFile("test.jpg", "content"u8.ToArray());

        var result = await UpdateFile.Handle(
            "test-id",
            file,
            dbContext,
            mockStorageService.Object,
            new ClaimsPrincipal(),
            httpContext,
            mockLogger.Object,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_File_Is_Null()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = CreateClaimsPrincipal("user1");
        var httpContext = CreateHttpContext("user1");
        var mockLogger = new Mock<ILogger<UpdateFile>>();
        var mockStorageService = new Mock<IStorageService>();

        var existingFile = new StoredFile
        {
            Id = "test-id",
            FileName = "old.jpg",
            ContentType = "image/jpeg",
            Size = 100,
            Url = "/uploads/old.jpg",
            UploadedAt = DateTime.UtcNow,
            EntityId = "entity-123",
            EntityType = "testEntity",
            UploadedBy = "user1"
        };
        dbContext.StoredFiles.Add(existingFile);
        await dbContext.SaveChangesAsync();

        var result = await UpdateFile.Handle(
            "test-id",
            null,
            dbContext,
            mockStorageService.Object,
            user,
            httpContext,
            mockLogger.Object,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        var badRequest = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        badRequest!.Value?.Success.Should().BeFalse();
        badRequest.Value?.Message.Should().Be("No file uploaded.");
        badRequest.Value?.TraceId.Should().Be("test-trace-id");
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_File_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = CreateClaimsPrincipal("user1");
        var httpContext = CreateHttpContext("user1");
        var mockLogger = new Mock<ILogger<UpdateFile>>();
        var mockStorageService = new Mock<IStorageService>();
        var file = CreateFormFile("test.jpg", "content"u8.ToArray());

        var result = await UpdateFile.Handle(
            "non-existent-id",
            file,
            dbContext,
            mockStorageService.Object,
            user,
            httpContext,
            mockLogger.Object,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
        var notFound = result as Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>;
        notFound!.Value?.Success.Should().BeFalse();
        notFound.Value?.Message.Should().Be("File not found.");
        notFound.Value?.TraceId.Should().Be("test-trace-id");
    }

    [Fact]
    public async Task Handle_Should_Update_File_Successfully()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = CreateClaimsPrincipal("user1");
        var httpContext = CreateHttpContext("user1");
        var mockLogger = new Mock<ILogger<UpdateFile>>();
        var mockStorageService = new Mock<IStorageService>();

        var existingFile = new StoredFile
        {
            Id = "test-id",
            FileName = "old.jpg",
            ContentType = "image/jpeg",
            Size = 100,
            Url = "/uploads/old.jpg",
            UploadedAt = DateTime.UtcNow.AddDays(-1),
            EntityId = "entity-123",
            EntityType = "testEntity",
            UploadedBy = "user1"
        };
        dbContext.StoredFiles.Add(existingFile);
        await dbContext.SaveChangesAsync();

        var newUrl = "/uploads/new.jpg";
        var file = CreateFormFile("new.jpg", "new content"u8.ToArray());

        mockStorageService
            .Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), file.FileName, file.ContentType, CancellationToken.None))
            .ReturnsAsync(newUrl);

        var result = await UpdateFile.Handle(
            "test-id",
            file,
            dbContext,
            mockStorageService.Object,
            user,
            httpContext,
            mockLogger.Object,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<PostFile.StoredFileResponseDto>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<PostFile.StoredFileResponseDto>>;
        okResult!.Value?.Success.Should().BeTrue();
        okResult.Value?.Data?.Url.Should().Be(newUrl);
        okResult.Value?.Data?.FileName.Should().Be("new.jpg");
        okResult.Value?.Message.Should().Be("File updated.");
        okResult.Value?.TraceId.Should().Be("test-trace-id");

        mockStorageService.Verify(
            x => x.DeleteFileAsync("/uploads/old.jpg", CancellationToken.None),
            Times.Once);
        mockStorageService.Verify(
            x => x.SaveFileAsync(It.IsAny<Stream>(), file.FileName, file.ContentType, CancellationToken.None),
            Times.Once);
    }

    private static IFormFile CreateFormFile(string fileName, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };
    }
}
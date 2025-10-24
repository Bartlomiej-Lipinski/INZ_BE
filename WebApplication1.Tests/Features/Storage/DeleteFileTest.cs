using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using WebApplication1.Features.Storage;
using WebApplication1.Infrastructure.Data.Entities.Storage;
using WebApplication1.Infrastructure.Storage;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Storage;

public class DeleteFileTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_User_Is_Null()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var mockLogger = new Mock<ILogger<DeleteFile>>();
        var mockStorageService = new Mock<IStorageService>();

        var result = await DeleteFile.Handle(
            "test-id",
            dbContext,
            mockStorageService.Object,
            new ClaimsPrincipal(),
            httpContext,
            mockLogger.Object,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_File_Does_Not_Exist_In_Database()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = CreateClaimsPrincipal("user1");
        var httpContext = CreateHttpContext("user1");
        var mockLogger = new Mock<ILogger<DeleteFile>>();
        var mockStorageService = new Mock<IStorageService>();

        var result = await DeleteFile.Handle(
            "non-existent-id",
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
    public async Task Handle_Should_Delete_File_Successfully()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = CreateClaimsPrincipal("user1");
        var httpContext = CreateHttpContext("user1");
        var mockLogger = new Mock<ILogger<DeleteFile>>();
        var mockStorageService = new Mock<IStorageService>();

        var storedFile = new StoredFile
        {
            Id = "test-id",
            FileName = "test.jpg",
            ContentType = "image/jpeg",
            Size = 100,
            Url = "/uploads/profile/test.jpg",
            UploadedAt = DateTime.UtcNow,
            EntityId = "entity-123",
            EntityType = "testEntity",
            UploadedBy = "user1"
        };
        dbContext.StoredFiles.Add(storedFile);
        await dbContext.SaveChangesAsync();

        var result = await DeleteFile.Handle(
            "test-id",
            dbContext,
            mockStorageService.Object,
            user,
            httpContext,
            mockLogger.Object,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<string>>;
        okResult!.Value?.Success.Should().BeTrue();
        okResult.Value?.Message.Should().Be("File deleted.");
        okResult.Value?.TraceId.Should().Be("test-trace-id");

        mockStorageService.Verify(
            x => x.DeleteFileAsync(storedFile.Url, CancellationToken.None),
            Times.Once);

        var deletedFile = await dbContext.StoredFiles.FindAsync("test-id");
        deletedFile.Should().BeNull();
    }
}
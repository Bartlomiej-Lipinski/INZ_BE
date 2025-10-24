using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebApplication1.Features.Storage;
using WebApplication1.Infrastructure.Data.Entities.Storage;
using WebApplication1.Infrastructure.Storage;

namespace WebApplication1.Tests.Features.Storage;

public class GetFileTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_File_Does_Not_Exist_In_Database()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var mockLogger = new Mock<ILogger<GetFile>>();
        var mockStorageService = new Mock<IStorageService>();

        var result = await GetFile.Handle(
            "non-existent-id",
            dbContext,
            mockStorageService.Object,
            httpContext,
            mockLogger.Object,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound>();
    }

    [Fact]
    public async Task Handle_Should_Return_File_Stream_Successfully()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var mockLogger = new Mock<ILogger<GetFile>>();
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

        var fileContent = "test content"u8.ToArray();
        var memoryStream = new MemoryStream(fileContent);

        mockStorageService
            .Setup(x => x.OpenReadAsync(storedFile.Url, CancellationToken.None))
            .ReturnsAsync(memoryStream);

        var result = await GetFile.Handle(
            "test-id",
            dbContext,
            mockStorageService.Object,
            httpContext,
            mockLogger.Object,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.FileStreamHttpResult>();
        var fileResult = result as Microsoft.AspNetCore.Http.HttpResults.FileStreamHttpResult;
        fileResult!.ContentType.Should().Be("image/jpeg");
        fileResult.FileDownloadName.Should().Be("test.jpg");

        mockStorageService.Verify(
            x => x.OpenReadAsync(storedFile.Url, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Physical_File_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var mockLogger = new Mock<ILogger<GetFile>>();
        var mockStorageService = new Mock<IStorageService>();

        var storedFile = new StoredFile
        {
            Id = "test-id",
            FileName = "missing.jpg",
            ContentType = "image/jpeg",
            Size = 100,
            Url = "/uploads/missing.jpg",
            UploadedAt = DateTime.UtcNow,
            EntityId = "entity-123",
            EntityType = "testEntity",
            UploadedBy = "user1"
        };
        dbContext.StoredFiles.Add(storedFile);
        await dbContext.SaveChangesAsync();

        mockStorageService
            .Setup(x => x.OpenReadAsync(storedFile.Url, CancellationToken.None))
            .ReturnsAsync((Stream?)null);

        var result = await GetFile.Handle(
            "test-id",
            dbContext,
            mockStorageService.Object,
            httpContext,
            mockLogger.Object,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound>();
    }
}
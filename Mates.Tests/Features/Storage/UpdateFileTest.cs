using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Storage;
using Mates.Features.Storage.Dtos;
using Mates.Infrastructure.Data.Enums;
using Mates.Infrastructure.Service;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Storage;

public class UpdateFileTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_File_Is_Null()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var mockStorageService = new Mock<IStorageService>();

        var existingFile = TestDataFactory.CreateStoredFile(
            "test-id",
            "g1",
            "old.jpg",
            "image/jpeg",
            100,
            "/uploads/old.jpg",
            DateTime.UtcNow,
            "entity-123",
            EntityType.Recommendation,
            "user1",
            null
        );

        dbContext.StoredFiles.Add(existingFile);
        await dbContext.SaveChangesAsync();

        var result = await UpdateFile.Handle(
            "g1",
            "test-id",
            null,
            dbContext,
            mockStorageService.Object,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<UpdateFile>.Instance,
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
        var mockStorageService = new Mock<IStorageService>();
        var file = TestDataFactory.CreateFormFile("test.jpg", "content"u8.ToArray());

        var result = await UpdateFile.Handle(
            "g1",
            "non-existent-id",
            file,
            dbContext,
            mockStorageService.Object,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<UpdateFile>.Instance,
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
        var mockStorageService = new Mock<IStorageService>();

        var existingFile = TestDataFactory.CreateStoredFile(
            "test-id",
            "g1",
            "old.jpg",
            "image/jpeg",
            100,
            "/uploads/old.jpg",
            DateTime.UtcNow.AddDays(-1),
            "entity-123",
            EntityType.Recommendation,
            "user1",
            null
        );

        dbContext.StoredFiles.Add(existingFile);
        await dbContext.SaveChangesAsync();

        const string newUrl = "/uploads/new.jpg";
        var file = TestDataFactory.CreateFormFile("new.jpg", "new content"u8.ToArray());

        mockStorageService
            .Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), file.FileName, file.ContentType, CancellationToken.None))
            .ReturnsAsync(newUrl);

        var result = await UpdateFile.Handle(
            "g1",
            "test-id",
            file,
            dbContext,
            mockStorageService.Object,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<UpdateFile>.Instance,
            CancellationToken.None);

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<StoredFileResponseDto>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<StoredFileResponseDto>>;
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
}
﻿using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Storage;
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
        var logger = NullLogger<DeleteFile>.Instance;
        var mockStorageService = new Mock<IStorageService>();

        var result = await DeleteFile.Handle(
            "test-id",
            dbContext,
            mockStorageService.Object,
            CreateClaimsPrincipal(),
            httpContext,
            logger,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_File_Does_Not_Exist_In_Database()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = CreateClaimsPrincipal("user1");
        var httpContext = CreateHttpContext("user1");
        var logger = NullLogger<DeleteFile>.Instance;
        var mockStorageService = new Mock<IStorageService>();

        var result = await DeleteFile.Handle(
            "non-existent-id",
            dbContext,
            mockStorageService.Object,
            user,
            httpContext,
            logger,
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
        var logger = NullLogger<DeleteFile>.Instance;
        var mockStorageService = new Mock<IStorageService>();

        var storedFile = TestDataFactory.CreateStoredFile(
            "test-id",
            "test.jpg",
            "image/jpeg",
            100,
            "/uploads/profile/test.jpg",
            DateTime.UtcNow,
            "entity-123",
            "testEntity",
            "user1");

        dbContext.StoredFiles.Add(storedFile);
        await dbContext.SaveChangesAsync();

        var result = await DeleteFile.Handle(
            "test-id",
            dbContext,
            mockStorageService.Object,
            user,
            httpContext,
            logger,
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
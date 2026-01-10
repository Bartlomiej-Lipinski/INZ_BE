using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mates.Features.Storage;
using Mates.Features.Storage.Dtos;
using Mates.Infrastructure.Data.Enums;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Storage;

public class GetGroupAlbumTest: TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Empty_List_When_No_Files()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        const string groupId = "g1";

        var result = await GetGroupAlbum.Handle(
            groupId,
            dbContext,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<GetGroupAlbum>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<StoredFileResponseDto>>>>();
        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<StoredFileResponseDto>>>;
        ok!.Value!.Data.Should().BeEmpty();
        ok.Value.Message.Should().Be("No album found for this group.");
    }

    [Fact]
    public async Task Handle_Should_Return_All_Album_Files()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        const string groupId = "g1";

        var file1 = TestDataFactory.CreateStoredFile(
            "f1",
            groupId,
            "photo1.jpg",
            "image/jpeg",
            12345,
            "url1",
            DateTime.UtcNow.AddMinutes(-10),
            null,
            EntityType.AlbumMedia,
            "user1",
            null
        );

        var file2 = TestDataFactory.CreateStoredFile(
            "f2",
            groupId,
            "video1.mp4",
            "video/mp4",
            54321,
            "url2",
            DateTime.UtcNow.AddMinutes(-10),
            null,
            EntityType.AlbumMedia,
            "user1",
            null
        );

        dbContext.StoredFiles.AddRange(file1, file2);
        await dbContext.SaveChangesAsync();

        var result = await GetGroupAlbum.Handle(
            groupId,
            dbContext,
            CreateClaimsPrincipal("user1"),
            CreateHttpContext("user1"),
            NullLogger<GetGroupAlbum>.Instance,
            CancellationToken.None
        );

        var ok = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<StoredFileResponseDto>>>;
        ok.Should().NotBeNull();
        ok!.Value!.Data.Should().HaveCount(2);
        ok.Value.Data.Select(f => f.FileName).Should().Contain(["photo1.jpg", "video1.mp4"]);
        ok.Value.Message.Should().Be("Group album retrieved successfully.");
    }
}
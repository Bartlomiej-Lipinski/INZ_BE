using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mates.Features.Storage;
using Mates.Features.Storage.Dtos;
using Mates.Infrastructure.Service;
using Mates.Shared.Responses;

namespace Mates.Tests.Features.Storage;

public class UploadUserProfilePhotoTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_File_Is_Null()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var mockStorage = new Mock<IStorageService>();
        dbContext.Users.Add(TestDataFactory.CreateUser("user1","Test", "User"));
        await dbContext.SaveChangesAsync();

        var result = await UploadUserProfilePhoto.Handle(
            null,
            CreateClaimsPrincipal("user1"),
            dbContext,
            mockStorage.Object,
            CreateHttpContext(),
            NullLogger<UploadUserProfilePhoto>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        var badRequest = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        badRequest!.Value!.Success.Should().BeFalse();
        badRequest.Value.Message.Should().Be("No file uploaded.");
        badRequest.Value.TraceId.Should().Be("test-trace-id");
    }

    [Fact]
    public async Task Handle_Should_Upload_File_Successfully()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var mockStorage = new Mock<IStorageService>();
        var user = TestDataFactory.CreateUser("user1", "Test", "User");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var file = TestDataFactory.CreateFormFile("test.jpg", new byte[] { 1, 2, 3 });
        const string expectedUrl = "/uploads/test.jpg";
        mockStorage
            .Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), file.FileName, file.ContentType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUrl);

        var result = await UploadUserProfilePhoto.Handle(
            file,
            CreateClaimsPrincipal(user.Id),
            dbContext,
            mockStorage.Object,
            CreateHttpContext(),
            NullLogger<UploadUserProfilePhoto>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<ProfilePictureResponseDto>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<ProfilePictureResponseDto>>;
        okResult!.Value!.Success.Should().BeTrue();
        okResult.Value.Data!.Url.Should().Be(expectedUrl);
        okResult.Value.Data.FileName.Should().Be("test.jpg");
        okResult.Value.Message.Should().Be("Profile photo updated successfully.");
        okResult.Value.TraceId.Should().Be("test-trace-id");

        var storedFile = await dbContext.StoredFiles.FirstOrDefaultAsync();
        storedFile.Should().NotBeNull();
        storedFile!.Url.Should().Be(expectedUrl);
        storedFile.EntityType.Should().Be(Infrastructure.Data.Enums.EntityType.User);

        mockStorage.Verify(x => x.SaveFileAsync(It.IsAny<Stream>(), file.FileName, file.ContentType, It.IsAny<CancellationToken>()), Times.Once);
    }
}
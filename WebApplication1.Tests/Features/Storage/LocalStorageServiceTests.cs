using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WebApplication1.Infrastructure.Storage;

namespace WebApplication1.Tests.Features.Storage;

public class LocalStorageServiceTests : IDisposable
{
    private readonly LocalStorageService _sut;
    private readonly string _testUploadPath;

    public LocalStorageServiceTests()
    {
        _testUploadPath = Path.Combine(Path.GetTempPath(), "test-uploads", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testUploadPath);
        
        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        mockWebHostEnvironment.Setup(x => x.WebRootPath).Returns(_testUploadPath);
        mockWebHostEnvironment.Setup(x => x.ContentRootPath).Returns(_testUploadPath);
        
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(x => x["Storage:UploadsFolder"]).Returns(_testUploadPath);
        mockConfiguration.Setup(x => x["Storage:BaseRequestPath"]).Returns("/uploads");
        
        _sut = new LocalStorageService(
            mockConfiguration.Object,
            mockWebHostEnvironment.Object,
            Mock.Of<ILogger<LocalStorageService>>());
    }

    [Fact]
    public async Task SaveFileAsync_ShouldSaveFile_AndReturnUrl()
    {
        // Arrange
        var fileName = "test-image.jpg";
        var content = "test content"u8.ToArray();
        var formFile = CreateFormFile(fileName, content);

        // Act
        await using var stream = formFile.OpenReadStream();
        var result = await _sut.SaveFileAsync(stream, formFile.FileName, formFile.ContentType, CancellationToken.None);

        // Assert
        result.Should().StartWith("/uploads/");
        result.Should().EndWith(".jpg");

        var savedFileName = Path.GetFileName(result);
        var savedFilePath = Path.Combine(_testUploadPath, savedFileName);
        File.Exists(savedFilePath).Should().BeTrue();
        var savedContent = await File.ReadAllBytesAsync(savedFilePath);
        savedContent.Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task SaveFileAsync_ShouldGenerateUniqueFileName()
    {
        // Arrange
        var fileName = "duplicate.jpg";
        var file1 = CreateFormFile(fileName, "content1"u8.ToArray());
        var file2 = CreateFormFile(fileName, "content2"u8.ToArray());

        // Act
        await using var stream1 = file1.OpenReadStream();
        var url1 = await _sut.SaveFileAsync(stream1, file1.FileName, file1.ContentType, CancellationToken.None);
        
        await using var stream2 = file2.OpenReadStream();
        var url2 = await _sut.SaveFileAsync(stream2, file2.FileName, file2.ContentType, CancellationToken.None);

        // Assert
        url1.Should().NotBe(url2);
    }

    [Fact]
    public async Task DeleteFileAsync_ShouldRemoveFile()
    {
        // Arrange
        var formFile = CreateFormFile("to-delete.jpg", "content"u8.ToArray());
        await using var stream = formFile.OpenReadStream();
        var url = await _sut.SaveFileAsync(stream, formFile.FileName, formFile.ContentType, CancellationToken.None);
        var filePath = Path.Combine(_testUploadPath, Path.GetFileName(url));

        // Act
        await _sut.DeleteFileAsync(url, CancellationToken.None);

        // Assert
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFileAsync_ShouldNotThrow_WhenFileDoesNotExist()
    {
        // Arrange
        var nonExistentUrl = "/uploads/non-existent.jpg";

        // Act
        var act = async () => await _sut.DeleteFileAsync(nonExistentUrl, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OpenReadAsync_ShouldReturnFileStream()
    {
        // Arrange
        var content = "stream content"u8.ToArray();
        var formFile = CreateFormFile("stream-test.jpg", content);
        await using var saveStream = formFile.OpenReadStream();
        var url = await _sut.SaveFileAsync(saveStream, formFile.FileName, formFile.ContentType, CancellationToken.None);

        // Act
        var stream = await _sut.OpenReadAsync(url, CancellationToken.None);
        stream.Should().NotBeNull();
        
        await using var _ = stream;
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        // Assert
        memoryStream.ToArray().Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task OpenReadAsync_ShouldReturnNull_WhenFileDoesNotExist()
    {
        // Arrange
        var nonExistentUrl = "/uploads/missing.jpg";

        // Act
        var result = await _sut.OpenReadAsync(nonExistentUrl, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("test.jpg", ".jpg")]
    [InlineData("document.pdf", ".pdf")]
    [InlineData("image.PNG", ".PNG")]
    public async Task SaveFileAsync_ShouldPreserveFileExtension(string fileName, string expectedExtension)
    {
        // Arrange
        var formFile = CreateFormFile(fileName, "content"u8.ToArray());

        // Act
        await using var stream = formFile.OpenReadStream();
        var result = await _sut.SaveFileAsync(stream, formFile.FileName, formFile.ContentType, CancellationToken.None);

        // Assert
        result.Should().EndWith(expectedExtension);
    }

    [Fact]
    public async Task SaveFileAsync_ShouldCreateUploadsDirectory_IfNotExists()
    {
        // Arrange
        var newPath = Path.Combine(Path.GetTempPath(), "new-uploads", Guid.NewGuid().ToString());
        var mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
        mockWebHostEnvironment.Setup(x => x.WebRootPath).Returns(newPath);
        mockWebHostEnvironment.Setup(x => x.ContentRootPath).Returns(newPath);
        
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(x => x["Storage:UploadsFolder"]).Returns(newPath);
        mockConfiguration.Setup(x => x["Storage:BaseRequestPath"]).Returns("/uploads");
        
        var service = new LocalStorageService(
            mockConfiguration.Object,
            mockWebHostEnvironment.Object,
            Mock.Of<ILogger<LocalStorageService>>());
        var formFile = CreateFormFile("test.jpg", "content"u8.ToArray());

        // Act
        await using var stream = formFile.OpenReadStream();
        await service.SaveFileAsync(stream, formFile.FileName, formFile.ContentType, CancellationToken.None);

        // Assert
        Directory.Exists(newPath).Should().BeTrue();

        // Cleanup
        Directory.Delete(newPath, true);
    }

    private static IFormFile CreateFormFile(string fileName, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_testUploadPath))
        {
            Directory.Delete(_testUploadPath, true);
        }
    }
}
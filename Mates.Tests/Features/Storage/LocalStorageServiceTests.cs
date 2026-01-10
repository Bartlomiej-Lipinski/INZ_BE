using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Mates.Infrastructure.Service;

namespace Mates.Tests.Features.Storage;

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
        const string fileName = "test-image.jpg";
        var content = "test content"u8.ToArray();
        var formFile = TestDataFactory.CreateFormFile(fileName, content);

        await using var stream = formFile.OpenReadStream();
        var result = await _sut.SaveFileAsync(stream, formFile.FileName, formFile.ContentType, CancellationToken.None);

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
        const string fileName = "duplicate.jpg";
        var file1 = TestDataFactory.CreateFormFile(fileName, "content1"u8.ToArray());
        var file2 = TestDataFactory.CreateFormFile(fileName, "content2"u8.ToArray());

        await using var stream1 = file1.OpenReadStream();
        var url1 = await _sut.SaveFileAsync(stream1, file1.FileName, file1.ContentType, CancellationToken.None);
        
        await using var stream2 = file2.OpenReadStream();
        var url2 = await _sut.SaveFileAsync(stream2, file2.FileName, file2.ContentType, CancellationToken.None);

        url1.Should().NotBe(url2);
    }

    [Fact]
    public async Task DeleteFileAsync_ShouldRemoveFile()
    {
        var formFile = TestDataFactory.CreateFormFile("to-delete.jpg", "content"u8.ToArray());
        await using var stream = formFile.OpenReadStream();
        var url = await _sut.SaveFileAsync(stream, formFile.FileName, formFile.ContentType, CancellationToken.None);
        var filePath = Path.Combine(_testUploadPath, Path.GetFileName(url));

        await _sut.DeleteFileAsync(url, CancellationToken.None);

        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteFileAsync_ShouldNotThrow_WhenFileDoesNotExist()
    {
        const string nonExistentUrl = "/uploads/non-existent.jpg";

        var act = async () => await _sut.DeleteFileAsync(nonExistentUrl, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OpenReadAsync_ShouldReturnFileStream()
    {
        var content = "stream content"u8.ToArray();
        var formFile = TestDataFactory.CreateFormFile("stream-test.jpg", content);
        await using var saveStream = formFile.OpenReadStream();
        var url = await _sut.SaveFileAsync(saveStream, formFile.FileName, formFile.ContentType, CancellationToken.None);

        var stream = await _sut.OpenReadAsync(url, CancellationToken.None);
        stream.Should().NotBeNull();
        
        await using var _ = stream;
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        memoryStream.ToArray().Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task OpenReadAsync_ShouldReturnNull_WhenFileDoesNotExist()
    {
        const string nonExistentUrl = "/uploads/missing.jpg";

        var result = await _sut.OpenReadAsync(nonExistentUrl, CancellationToken.None);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("test.jpg", ".jpg")]
    [InlineData("document.pdf", ".pdf")]
    [InlineData("image.PNG", ".PNG")]
    public async Task SaveFileAsync_ShouldPreserveFileExtension(string fileName, string expectedExtension)
    {
        var formFile = TestDataFactory.CreateFormFile(fileName, "content"u8.ToArray());

        await using var stream = formFile.OpenReadStream();
        var result = await _sut.SaveFileAsync(stream, formFile.FileName, formFile.ContentType, CancellationToken.None);

        result.Should().EndWith(expectedExtension);
    }

    [Fact]
    public async Task SaveFileAsync_ShouldCreateUploadsDirectory_IfNotExists()
    {
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
        var formFile = TestDataFactory.CreateFormFile("test.jpg", "content"u8.ToArray());

        await using var stream = formFile.OpenReadStream();
        await service.SaveFileAsync(stream, formFile.FileName, formFile.ContentType, CancellationToken.None);

        Directory.Exists(newPath).Should().BeTrue();
        Directory.Delete(newPath, true);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testUploadPath))
        {
            Directory.Delete(_testUploadPath, true);
        }
    }
}
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebApplication1.Features.Groups;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Groups;

public class GetGroupByIdTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_Ok_With_Group_When_Group_Exists()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var mockLogger = new Mock<ILogger<GetGroupById>>();
        
        var group = TestDataFactory.CreateGroup("group1", "Test Group", "#FFFFFF", "CODE1");
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();
        
        var result = await GetGroupById
            .Handle(group.Id, dbContext, httpContext, mockLogger.Object, CancellationToken.None);
            
        result.Should()
            .BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<GetGroupById.GroupResponseDto>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<GetGroupById.GroupResponseDto>>;
            
        okResult!.Value?.Success.Should().BeTrue();
        okResult.Value?.Data.Should().NotBeNull();
        okResult.Value?.Data!.Id.Should().Be(group.Id);
        okResult.Value?.Data!.Name.Should().Be(group.Name);
        okResult.Value?.Data!.Color.Should().Be(group.Color);
        okResult.Value?.Data!.Code.Should().Be(group.Code);
        okResult.Value?.TraceId.Should().Be("test-trace-id");
    }
    
    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Group_Does_Not_Exist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var mockLogger = new Mock<ILogger<GetGroupById>>();
        
        var result = await GetGroupById
            .Handle("nonexistent", dbContext, httpContext, mockLogger.Object, CancellationToken.None);
            
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
        var notFoundResult = result as Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>;
            
        notFoundResult!.Value?.Success.Should().BeFalse();
        notFoundResult.Value?.Message.Should().Be("Group not found");
        notFoundResult.Value?.TraceId.Should().Be("test-trace-id");
    }
    
    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Id_Is_NullOrEmpty()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var mockLogger = new Mock<ILogger<GetGroupById>>();
        
        var result = await GetGroupById
            .Handle("", dbContext, httpContext, mockLogger.Object, CancellationToken.None);
            
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        var badRequestResult = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        
        badRequestResult!.Value?.Success.Should().BeFalse();
        badRequestResult.Value?.Message.Should().Be("Group ID cannot be null or empty.");
        badRequestResult.Value?.TraceId.Should().Be("test-trace-id");
    }

    [Fact]
    public async Task Handle_Should_Return_Ok_With_Correct_Dto_Properties()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContext();
        var mockLogger = new Mock<ILogger<GetGroupById>>();
        
        var group = TestDataFactory.CreateGroup("group2", "Another Group", "#000000", "CODE2");
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();
        
        var result = await GetGroupById
            .Handle(group.Id, dbContext, httpContext, mockLogger.Object, CancellationToken.None);
            
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<GetGroupById.GroupResponseDto>>;
        okResult.Should().NotBeNull();

        var dto = okResult.Value?.Data;
        dto.Should().NotBeNull();
        dto.Id.Should().Be("group2");
        dto.Name.Should().Be("Another Group");
        dto.Color.Should().Be("#000000");
        dto.Code.Should().Be("CODE2");
        okResult.Value?.TraceId.Should().Be("test-trace-id");
    }
}

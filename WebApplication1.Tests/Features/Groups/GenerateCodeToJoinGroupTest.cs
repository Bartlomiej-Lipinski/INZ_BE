using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WebApplication1.Features.Groups;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Groups;

public class GenerateCodeToJoinGroupTest : TestBase
{
    [Fact]
    public async Task Handle_ShouldGenerateUniqueCode()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var logger = new LoggerFactory().CreateLogger<GenerateCodeToJoinGroup>();
        var httpContext = new DefaultHttpContext();

        var group = TestDataFactory.CreateGroup(id: "g1", name: "Test Group", color: "#FFFFFF");
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();
        
        var result = await GenerateCodeToJoinGroup.Handle("g1", dbContext, httpContext, logger, CancellationToken.None);
        
        result.Should()
            .BeOfType<Microsoft.AspNetCore.Http.HttpResults
                .Ok<ApiResponse<GenerateCodeToJoinGroup.GenerateCodeResponse>>>();
        
        var okResult = 
            result as Microsoft.AspNetCore.Http.HttpResults
                .Ok<ApiResponse<GenerateCodeToJoinGroup.GenerateCodeResponse>>;
        okResult!.Value?.Success.Should().BeTrue();
        okResult.Value?.Data?.message.Should().Be("New code generated successfully. The code is valid for 5 minutes.");
        
        var updatedGroup = await dbContext.Groups.FindAsync("g1");
        updatedGroup.Should().NotBeNull();
        updatedGroup.CodeExpirationTime.Should().BeAfter(DateTime.UtcNow); 
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenGroupDoesNotExist()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = new DefaultHttpContext();
        var logger = new LoggerFactory().CreateLogger<GenerateCodeToJoinGroup>();
        
        var result = await GenerateCodeToJoinGroup.Handle("non-existent", dbContext, httpContext, logger, CancellationToken.None);
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>>();
       
        var notFoundResult = result as Microsoft.AspNetCore.Http.HttpResults.NotFound<ApiResponse<string>>;
        notFoundResult!.Value?.Success.Should().BeFalse();
        notFoundResult.Value?.Message.Should().Be("Group not found.");
    }

    [Fact]
    public async Task Handle_ShouldReturnBadRequest_WhenGroupIdIsEmpty()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = new DefaultHttpContext();
        var logger = new LoggerFactory().CreateLogger<GenerateCodeToJoinGroup>();
        
        var result = await GenerateCodeToJoinGroup.Handle("", dbContext, httpContext, logger, CancellationToken.None);
        
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        
        var badRequestResult = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        badRequestResult!.Value?.Success.Should().BeFalse();
        badRequestResult.Value?.Message.Should().Be("Group ID cannot be null or empty.");
    }
}
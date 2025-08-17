using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Groups;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Groups;

public class PostGroupTest : TestBase
{
    private HttpContext CreateHttpContextWithUser(string? userId = null)
    {
        var context = new DefaultHttpContext();

        if (userId == null) return context;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        context.User = new ClaimsPrincipal(identity);

        return context;
    }
    
    [Fact]
    public async Task Handle_ReturnsCreatedResult_WhenValidRequest()
    {
        // Arrange
        await using var dbContext = GetInMemoryDbContext();
        const string userId = "user-123";
        var httpContext = CreateHttpContextWithUser(userId);
        var requestDto = TestDataFactory.CreateGroupRequestDto("MyGroup", "Blue");

        // Act
        var result = await PostGroup.Handle(httpContext, requestDto, dbContext, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status201Created, statusCodeResult.StatusCode);
        
        await result.ExecuteAsync(httpContext);
        
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        var apiResponse = JsonSerializer.Deserialize<ApiResponse<PostGroup.GroupResponseDto>>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(apiResponse);
        Assert.True(apiResponse!.Success);
        Assert.Equal("MyGroup", apiResponse.Data?.Name);
        Assert.Equal("Blue", apiResponse.Data?.Color);
        Assert.NotNull(apiResponse.Data?.Id);
        Assert.NotNull(apiResponse.Data.Code);
        
        var groupInDb = await dbContext.Groups.FindAsync(apiResponse.Data.Id);
        Assert.NotNull(groupInDb);
        Assert.Equal("MyGroup", groupInDb.Name);

        var groupUserInDb = await dbContext.GroupUsers.FirstOrDefaultAsync(gu => gu.GroupId == groupInDb.Id && gu.UserId == userId);
        Assert.NotNull(groupUserInDb);
        Assert.True(groupUserInDb.IsAdmin);
    }
    
    [Fact]
    public async Task Handle_ReturnsBadRequest_WhenRequestInvalid()
    {
        // Arrange
        await using var dbContext = GetInMemoryDbContext();
        var httpContext = CreateHttpContextWithUser("user-123");
        var invalidRequestDto = new PostGroup.GroupRequestDto { Name = "", Color = "" };

        // Act
        var result = await PostGroup.Handle(httpContext, invalidRequestDto, dbContext, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusCodeResult.StatusCode);
        
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        
        var jsonDoc = JsonDocument.Parse(responseBody);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("message", out var messageProp));
        Assert.Equal("Name and Color are required", messageProp.GetString());
    }
    
    [Fact]
    public async Task Handle_ReturnsUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        await using var dbContext = GetInMemoryDbContext();
        var httpContext = CreateHttpContextWithUser();
        var requestDto = TestDataFactory.CreateGroupRequestDto();

        // Act
        var result = await PostGroup.Handle(httpContext, requestDto, dbContext, CancellationToken.None);

        // Assert
        var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, statusCodeResult.StatusCode);
    }
}
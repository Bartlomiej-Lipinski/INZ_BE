using System.Security.Claims;
using FluentAssertions;
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
        if (string.IsNullOrEmpty(userId)) return context;
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId)
        ], "TestAuth");
        context.User = new ClaimsPrincipal(identity);
        return context;
    }
    
    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Name_Is_Missing()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContextWithUser("user1");

        var result = 
            await PostGroup.Handle(
                httpContext, TestDataFactory.CreateGroupRequestDto("",  "#FFF"), dbContext, CancellationToken.None);
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
    }
    
    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_No_UserId()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContextWithUser();
        
        var result = 
            await PostGroup.Handle(
                httpContext, TestDataFactory.CreateGroupRequestDto(
                    "Test Group",  "#FFF"), dbContext, CancellationToken.None);
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }
    
    [Fact]
    public async Task Handle_Should_Create_Group_And_Assign_User_As_Admin()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var httpContext = CreateHttpContextWithUser("user1");
        var dto = TestDataFactory.CreateGroupRequestDto("My Group",  "#FFF");
        
        var result = await PostGroup.Handle(httpContext, dto, dbContext, CancellationToken.None);
        
        result.Should()
            .BeOfType<Microsoft.AspNetCore.Http.HttpResults.Created<ApiResponse<PostGroup.GroupResponseDto>>>();
        var created = result as Microsoft.AspNetCore.Http.HttpResults.Created<ApiResponse<PostGroup.GroupResponseDto>>;
            
        created!.Value.Should().NotBeNull();
        created.Value!.Success.Should().BeTrue();
        created.Value.Data.Should().NotBeNull();
        created.Value.Data!.Name.Should().Be(dto.Name);
        created.Value.Data.Color.Should().Be(dto.Color);
        created.Value.Data.Code.Should().NotBeNullOrEmpty();
        
        var group = await dbContext.Groups.FirstOrDefaultAsync(g => g.Name == dto.Name);
        group.Should().NotBeNull();

        var groupUser =
            await dbContext.GroupUsers.FirstOrDefaultAsync(gu => gu.UserId == "user1" && gu.GroupId == group.Id);
        groupUser.Should().NotBeNull();
        groupUser.IsAdmin.Should().BeTrue();
    }
}
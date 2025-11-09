using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Features.Groups.GroupCRUD;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Groups;

public class PostGroupTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Name_Is_Missing()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        
        var result = await PostGroup.Handle(
            CreateHttpContext(user.Id),
            TestDataFactory.CreateGroupRequestDto("",  "#FFF"),
            dbContext,
            CreateClaimsPrincipal(user.Id),
            NullLogger<PostGroup>.Instance,
            CancellationToken.None
        );
                
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        var badRequest = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        badRequest!.Value!.TraceId.Should().Be("test-trace-id");
    }
    
    [Fact]
    public async Task Handle_Should_Create_Group_And_Assign_User_As_Admin()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "testUser");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        
        var dto = TestDataFactory.CreateGroupRequestDto("My Group",  "#FFF");
        
        var result = await PostGroup.Handle(
            CreateHttpContext(user.Id),
            dto, 
            dbContext,
            CreateClaimsPrincipal(user.Id),
            NullLogger<PostGroup>.Instance,
            CancellationToken.None
        );
        
        result.Should()
            .BeOfType<Microsoft.AspNetCore.Http.HttpResults.Created<ApiResponse<GroupResponseDto>>>();
        var created = result as Microsoft.AspNetCore.Http.HttpResults.Created<ApiResponse<GroupResponseDto>>;
            
        created!.Value.Should().NotBeNull();
        created.Value!.Success.Should().BeTrue();
        created.Value.Data.Should().NotBeNull();
        created.Value.Data!.Name.Should().Be(dto.Name);
        created.Value.Data.Color.Should().Be(dto.Color);
        created.Value.TraceId.Should().Be("test-trace-id");
        
        var group = await dbContext.Groups.FirstOrDefaultAsync(g => g.Name == dto.Name);
        group.Should().NotBeNull();

        var groupUser =
            await dbContext.GroupUsers.FirstOrDefaultAsync(gu => gu.UserId == user.Id && gu.GroupId == group.Id);
        groupUser.Should().NotBeNull();
        groupUser.IsAdmin.Should().BeTrue();
    }
}
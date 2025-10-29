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

        var result = await PostGroup.Handle(
            CreateHttpContext("user1"),
            TestDataFactory.CreateGroupRequestDto("",  "#FFF"),
            dbContext,
            NullLogger<PostGroup>.Instance,
            CancellationToken.None
        );
                
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>>();
        var badRequest = result as Microsoft.AspNetCore.Http.HttpResults.BadRequest<ApiResponse<string>>;
        badRequest!.Value!.TraceId.Should().Be("test-trace-id");
    }
    
    [Fact]
    public async Task Handle_Should_Return_Unauthorized_When_No_UserId()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        
        var result = await PostGroup.Handle(
            CreateHttpContext(), 
            TestDataFactory.CreateGroupRequestDto("Test Group",  "#FFF"), 
            dbContext,
            NullLogger<PostGroup>.Instance,
            CancellationToken.None
        );
                    
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>();
    }
    
    [Fact]
    public async Task Handle_Should_Create_Group_And_Assign_User_As_Admin()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var dto = TestDataFactory.CreateGroupRequestDto("My Group",  "#FFF");
        
        var result = await PostGroup.Handle(
            CreateHttpContext("user1"),
            dto, 
            dbContext,
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
            await dbContext.GroupUsers.FirstOrDefaultAsync(gu => gu.UserId == "user1" && gu.GroupId == group.Id);
        groupUser.Should().NotBeNull();
        groupUser.IsAdmin.Should().BeTrue();
    }
}
using FluentAssertions;
using WebApplication1.Features.Groups;
using WebApplication1.Infrastructure.Data.Entities;

namespace WebApplication1.Tests.Features.Groups;

public class GetJOinRequestForAdminsTest : TestBase
{
    [Fact]
    public async Task Test_JoinRequest_For_Admins()
    {
        var dbContext = GetInMemoryDbContext();
        var user1 = TestDataFactory.CreateUser("user1");
        var user2 = TestDataFactory.CreateUser("user2");
        var group1 = TestDataFactory.CreateGroup("group1", "Group 1", "#FFFFFF", "CODE1");
        var group2 = TestDataFactory.CreateGroup("group2", "Group 2", "#000000", "CODE2");
        dbContext.Users.AddRange(user1, user2);
        dbContext.Groups.AddRange(group1, group2);
        dbContext.GroupUsers.Add(new() { GroupId = group1.Id, UserId = user1.Id, IsAdmin = true, AcceptanceStatus = AcceptanceStatus.Accepted });
        dbContext.GroupUsers.Add(new() { GroupId = group2.Id, UserId = user1.Id, IsAdmin = false, AcceptanceStatus = AcceptanceStatus.Pending });
        dbContext.GroupUsers.Add(new() { GroupId = group1.Id, UserId = user2.Id, IsAdmin = false, AcceptanceStatus = AcceptanceStatus.Pending });
        await dbContext.SaveChangesAsync();
        var request = new GetJoinRequestsForAdmins.GetJoinRequestsForAdminsRequest(user1.Id);
        var result = await GetJoinRequestsForAdmins.Handle(request, dbContext, CancellationToken.None);
        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<WebApplication1.Shared.Responses.ApiResponse<IEnumerable<GetJoinRequestsForAdmins.SingleJoinRequestResponse>>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<WebApplication1.Shared.Responses.ApiResponse<IEnumerable<GetJoinRequestsForAdmins.SingleJoinRequestResponse>>>;
        okResult!.Value.Success.Should().BeTrue();
        okResult.Value.Data.Should().NotBeNull();
        var responses = okResult.Value.Data.ToList();
        responses!.Count.Should().Be(1);
        responses[0].GroupId.Should().Be(group1.Id);
        responses[0].GroupName.Should().Be(group1.Name);
        responses[0].UserId.Should().Be(user2.Id);
        responses[0].UserName.Should().Be(user2.Name); 
        
    }
}
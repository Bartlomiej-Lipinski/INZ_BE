using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Features.Groups.JoinGroupFeatures;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Groups;

public class GetJoinRequestsForAdminsTest : TestBase
{
    [Fact]
    public async Task Test_JoinRequest_For_Admins()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user1 = TestDataFactory.CreateUser("user1", "Test","User");
        var user2 = TestDataFactory.CreateUser("user2", "Test","User");
        var group1 = TestDataFactory.CreateGroup("group1", "Group 1", "#FFFFFF", "CODE1");
        var group2 = TestDataFactory.CreateGroup("group2", "Group 2", "#000000", "CODE2");

        dbContext.Users.AddRange(user1, user2);
        dbContext.Groups.AddRange(group1, group2);

        dbContext.GroupUsers.Add(TestDataFactory.CreateGroupUser(user1.Id, group1.Id, true));
        dbContext.GroupUsers.Add(TestDataFactory.CreateGroupUser(user1.Id, group2.Id));
        dbContext.GroupUsers.Add(TestDataFactory.CreateGroupUser(user2.Id, group1.Id, false, AcceptanceStatus.Pending));
        await dbContext.SaveChangesAsync();

        var result = await GetJoinRequestsForAdmins.Handle(
            CreateClaimsPrincipal(user1.Id),
            dbContext,
            CreateHttpContext(),
            NullLogger<GetJoinRequestsForAdmins>.Instance,
            CancellationToken.None
        );

        result
            .Should()
            .BeOfType<Ok<ApiResponse<IEnumerable<JoinRequestResponseDto>>>>();
        var okResult =
            result as Ok<ApiResponse<IEnumerable<JoinRequestResponseDto>>>;
        okResult!.Value?.Success.Should().BeTrue();
        okResult.Value?.Data.Should().NotBeNull();
        okResult.Value?.TraceId.Should().Be("test-trace-id");

        if (okResult.Value?.Data != null)
        {
            var responses = okResult.Value?.Data.ToList();
            responses!.Count.Should().Be(1);
            responses[0].GroupId.Should().Be(group1.Id);
            responses[0].GroupName.Should().Be(group1.Name);
            responses[0].User.Id.Should().Be(user2.Id);
            responses[0].User.Username.Should().Be(user2.UserName);
        }
    }
}
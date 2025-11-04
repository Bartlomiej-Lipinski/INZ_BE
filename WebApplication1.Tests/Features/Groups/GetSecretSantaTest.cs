using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Groups;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Groups;

public class GetSecretSantaTest : TestBase
{
    [Fact]
    public async Task Handle_Should_Return_BadRequest_When_Group_Has_Less_Than_Two_Users()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "User1");
        var group = TestDataFactory.CreateGroup("g1", "Group1");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);

        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        await dbContext.SaveChangesAsync();

        var result = await GetSecretSanta.Handle(
            group.Id,
            dbContext,
            CreateHttpContext(user.Id),
            NullLogger<GetSecretSanta>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<BadRequest<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_NotFound_When_Group_Record_Does_Not_Exist_But_GroupUsers_Exist()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user1 = TestDataFactory.CreateUser("u1", "User1");
        var user2 = TestDataFactory.CreateUser("u2", "User2");
        var groupUser1 = TestDataFactory.CreateGroupUser(user1.Id, "g-no-group");
        var groupUser2 = TestDataFactory.CreateGroupUser(user2.Id, "g-no-group");

        dbContext.Users.AddRange(user1, user2);
        dbContext.GroupUsers.AddRange(groupUser1, groupUser2);
        await dbContext.SaveChangesAsync();

        var result = await GetSecretSanta.Handle(
            "g-no-group",
            dbContext,
            CreateHttpContext(user1.Id),
            NullLogger<GetSecretSanta>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<NotFound<ApiResponse<string>>>();
    }

    [Fact]
    public async Task Handle_Should_Return_Ok_With_Pairs_When_Succeeds()
    {
        await using var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user1 = TestDataFactory.CreateUser("u1", "User1");
        var user2 = TestDataFactory.CreateUser("u2", "User2");
        var group = TestDataFactory.CreateGroup("g1", "Group1");
        var gu1 = TestDataFactory.CreateGroupUser(user1.Id, group.Id);
        var gu2 = TestDataFactory.CreateGroupUser(user2.Id, group.Id);

        dbContext.Users.AddRange(user1, user2);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.AddRange(gu1, gu2);
        await dbContext.SaveChangesAsync();

        var result = await GetSecretSanta.Handle(
            group.Id,
            dbContext,
            CreateHttpContext(user1.Id),
            NullLogger<GetSecretSanta>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Ok<ApiResponse<List<GetSecretSanta.SecretSantaPairDto>>>>();

        var okResult = result as Ok<ApiResponse<List<GetSecretSanta.SecretSantaPairDto>>>;
        okResult!.Value!.Success.Should().BeTrue();
        okResult.Value.Data.Should().NotBeNull();
        okResult.Value.Data!.Count.Should().Be(2);
    }
}
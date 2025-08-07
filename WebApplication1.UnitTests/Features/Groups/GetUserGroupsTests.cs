using WebApplication1.Features.Groups;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;

namespace WebApplication1.UnitTests.Features.Groups;

using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace WebApplication1.Tests.Features.Groups
{
    public class GetUserGroupsTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly GetUserGroups _sut; // System Under Test

        public GetUserGroupsTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique name for each test
                .Options;

            _dbContext = new AppDbContext(options);
            _sut = new GetUserGroups();
        }

        [Fact]
        public async Task Handle_WhenUserHasNoGroups_ShouldReturnEmptyList()
        {
            // Arrange
            var userId = "user-without-groups";
            var request = new GetUserGroupsRequest(userId);

            // Act
            var result = await InvokeHandleMethod(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
            result.Data.Count().Should().Be(0);
        }

        [Fact]
        public async Task Handle_WhenUserHasOneGroup_ShouldReturnOneElement()
        {
            // Arrange
            var userId = "user-123";
            var groupId = "group-1";
            var groupName = "Test Group 1";

            await SeedDatabase(userId, new[] { (groupId, groupName) });

            var request = new GetUserGroupsRequest(userId);

            // Act
            var result = await InvokeHandleMethod(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Count().Should().Be(1);
            
            var group = result.Data.First();
            group.Id.Should().Be(groupId);
            group.Name.Should().Be(groupName);
        }

        [Fact]
        public async Task Handle_WhenUserHasMultipleGroups_ShouldReturnAllGroups()
        {
            // Arrange
            var userId = "user-456";
            var groups = new[]
            {
                ("group-1", "Development Team"),
                ("group-2", "QA Team"),
                ("group-3", "DevOps Team")
            };

            await SeedDatabase(userId, groups);

            var request = new GetUserGroupsRequest(userId);

            // Act
            var result = await InvokeHandleMethod(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Count().Should().Be(3);

            // Verify all groups are returned
            var returnedGroups = result.Data.ToList();
            returnedGroups.Should().Contain(g => g.Id == "group-1" && g.Name == "Development Team");
            returnedGroups.Should().Contain(g => g.Id == "group-2" && g.Name == "QA Team");
            returnedGroups.Should().Contain(g => g.Id == "group-3" && g.Name == "DevOps Team");
        }

        [Fact]
        public async Task Handle_WhenMultipleUsersExist_ShouldReturnOnlyRequestedUserGroups()
        {
            // Arrange
            var targetUserId = "user-target";
            var otherUserId = "user-other";

            // Add groups for target user
            await SeedDatabase(targetUserId, new[]
            {
                ("group-1", "Target Group 1"),
                ("group-2", "Target Group 2")
            });

            // Add groups for other user
            await SeedDatabase(otherUserId, new[]
            {
                ("group-3", "Other Group 1"),
                ("group-4", "Other Group 2"),
                ("group-5", "Other Group 3")
            });

            var request = new GetUserGroupsRequest(targetUserId);

            // Act
            var result = await InvokeHandleMethod(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Count().Should().Be(2); // Only target user's groups

            var groupIds = result.Data.Select(g => g.Id).ToList();
            groupIds.Should().Contain("group-1");
            groupIds.Should().Contain("group-2");
            groupIds.Should().NotContain("group-3");
            groupIds.Should().NotContain("group-4");
            groupIds.Should().NotContain("group-5");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(50)]
        public async Task Handle_WithVariousGroupCounts_ShouldReturnCorrectCount(int groupCount)
        {
            // Arrange
            var userId = $"user-{groupCount}";
            var groups = Enumerable.Range(1, groupCount)
                .Select(i => ($"group-{i}", $"Group Name {i}"))
                .ToArray();

            if (groupCount > 0)
            {
                await SeedDatabase(userId, groups);
            }

            var request = new GetUserGroupsRequest(userId);

            // Act
            var result = await InvokeHandleMethod(request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Count().Should().Be(groupCount);
        }

        [Fact]
        public async Task Handle_WhenUserBelongsToSameGroupMultipleTimes_ShouldNotReturnDuplicates()
        {
            // Arrange (This scenario shouldn't happen with proper DB constraints, but testing defensive coding)
            var userId = "user-duplicate";
            var groupId = "group-same";
            var groupName = "Duplicate Test Group";

            // Add group
            var group = new Group { Id = groupId, Name = groupName };
            _dbContext.Groups.Add(group);

            // Try to add user to same group twice (if DB allows)
            _dbContext.GroupUsers.Add(new GroupUser { UserId = userId, GroupId = groupId, Group = group });
            
            try
            {
                _dbContext.GroupUsers.Add(new GroupUser { UserId = userId, GroupId = groupId, Group = group });
                await _dbContext.SaveChangesAsync();
            }
            catch
            {
                // If DB prevents duplicates, that's good - just continue with one entry
            }

            var request = new GetUserGroupsRequest(userId);

            // Act
            var result = await InvokeHandleMethod(request);

            // Assert
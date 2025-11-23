using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WebApplication1.Features.Materials;
using WebApplication1.Features.Storage.Dtos;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Tests.Features.Materials;

public class GetGroupMaterialsTest : TestBase
{
    [Fact]
    public async Task GetGroupMaterials_Should_Return_Materials_For_Valid_User_In_Group()
    {
        var dbContext = GetInMemoryDbContext(Guid.NewGuid().ToString());
        var user = TestDataFactory.CreateUser("u1", "Test", "User");
        var group = TestDataFactory.CreateGroup("g1", "Test Group");
        var groupUser = TestDataFactory.CreateGroupUser(user.Id, group.Id);

        var material1 = TestDataFactory.CreateStoredFile(
            "m1",
            group.Id,
            "File1.pdf",
            "application/pdf",
            1024,
            "/files/file1.pdf",
            DateTime.UtcNow.AddDays(-1),
            null,
            EntityType.Material,
            user.Id
        );
        var material2 = TestDataFactory.CreateStoredFile(
            "m2",
            group.Id,
            "File2.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            2048,
            "/files/file2.docx",
            DateTime.UtcNow,
            null,
            EntityType.Material,
            user.Id
        );

        dbContext.Users.Add(user);
        dbContext.Groups.Add(group);
        dbContext.GroupUsers.Add(groupUser);
        dbContext.StoredFiles.AddRange(material1, material2);
        await dbContext.SaveChangesAsync();

        var result = await GetGroupMaterials.Handle(
            group.Id,
            null,
            null,
            dbContext,
            CreateClaimsPrincipal(user.Id),
            CreateHttpContext(user.Id),
            NullLogger<GetGroupMaterials>.Instance,
            CancellationToken.None
        );

        result.Should().BeOfType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<StoredFileResponseDto>>>>();
        var okResult = result as Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResponse<List<StoredFileResponseDto>>>;
        okResult!.Value!.Success.Should().BeTrue();

        var materials = okResult.Value!.Data!;
        materials.Should().HaveCount(2);
        materials.Should().ContainSingle(m => m.FileName == "File1.pdf");
        materials.Should().ContainSingle(m => m.FileName == "File2.docx");
    }
}
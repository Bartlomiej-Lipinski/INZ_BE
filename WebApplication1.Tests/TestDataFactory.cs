using System.Diagnostics;
using WebApplication1.Features.Auth;
using WebApplication1.Features.Groups;
using WebApplication1.Infrastructure.Data.Entities;

namespace WebApplication1.Tests;

public static class TestDataFactory
{
    public static Group CreateGroup(string? id = null, string? name = null, string? color = null, string? code = null)
    {
        return new Group
        {
            Id = id ?? "g1",
            Name = name ?? "Test Group",
            Color = color ?? "#FFFFFF",
            Code = code ?? GenerateUniqueCode()
        };
    }
    
    public static GroupUser CreateGroupUser(
        string? userId = null,
        string? groupId = null,
        bool isAdmin = false,
        AcceptanceStatus acceptance = AcceptanceStatus.Accepted)
    {
        return new GroupUser
        {
            UserId = userId ?? "user1",
            GroupId = groupId ?? "g1",
            IsAdmin = isAdmin,
            AcceptanceStatus = acceptance
        };
    }
    
    public static PostGroup.GroupRequestDto CreateGroupRequestDto(string? name = null, string? color = null)
    {
        return new PostGroup.GroupRequestDto
        {
            Name = name ?? "TestGroup",
            Color = color ?? "Red"
        };
    }

    public static AcceptUserJoinRequest.AcceptUserJoinRequestDto CreateAcceptUserJoinRequestDto(
        string? groupId, string? userId)
    {
        Debug.Assert(groupId != null, nameof(groupId) + " != null");
        Debug.Assert(userId != null, nameof(userId) + " != null");
        return new AcceptUserJoinRequest.AcceptUserJoinRequestDto
        {
            GroupId = groupId,
            UserId = userId
        };
    }

    public static User CreateUser(
        string? id = null, string? name = null, string? email = null, string? userName = null, string? surname = null)
    {
        Debug.Assert(id != null, nameof(id) + " != null");
        return new User
        {
            Id = id,
            Name = name,
            Email = email,
            UserName = userName ?? name,
            Surname = surname
        };
    }
    
    public static GetUserGroups.GetUserGroupsRequest CreateGetUserGroupsRequest(string? userId = null)
    {
        Debug.Assert(userId != null, nameof(userId) + " != null");
        return new GetUserGroups.GetUserGroupsRequest(userId);
    }

    public static RejectUserJoinRequest.RejectUserJoinRequestDto CreateRejectUserJoinRequestDto(
        string groupId, string userId)
    {
        return new RejectUserJoinRequest.RejectUserJoinRequestDto
        {
            GroupId = groupId,
            UserId = userId
        };
    }
    
    public static JoinGroup.JoinGroupRequest CreateJoinGroupRequest(string groupCode, string userId)
    {
        return new JoinGroup.JoinGroupRequest(groupCode, userId);
    }
    
    public static AuthController.UserRequestDto CreateUserRequestDto(
        string email, string userName, string password, string name, string surname)
    {
        return new AuthController.UserRequestDto
        {
            Email = email,
            UserName = userName,
            Name = name,
            Surname = surname,
            Password = password
        };
    }
    
    public static ExtendedLoginRequest CreateExtendedLoginRequest(string email, string password)
    {
        return new ExtendedLoginRequest
        {
            Email = email,
            Password = password
        };
    }
    
    private static string GenerateUniqueCode()
    {
        return Guid.NewGuid().ToString()[..8].ToUpper();
    }
}
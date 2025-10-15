using WebApplication1.Features.Auth;
using WebApplication1.Features.Groups;
using WebApplication1.Features.Recommendations;
using WebApplication1.Features.Recommendations.Comments;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Infrastructure.Data.Entities.Recommendations;

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
        ArgumentNullException.ThrowIfNull(groupId, nameof(groupId));
        ArgumentNullException.ThrowIfNull(userId, nameof(userId));
        return new AcceptUserJoinRequest.AcceptUserJoinRequestDto
        {
            GroupId = groupId,
            UserId = userId
        };
    }

    public static User CreateUser(
        string? id = null, string? name = null, string? email = null, string? userName = null, string? surname = null)
    {
        ArgumentNullException.ThrowIfNull(id, nameof(id));
        return new User
        {
            Id = id,
            Name = name,
            Email = email,
            UserName = userName ?? name,
            Surname = surname
        };
    }
    
    // public static GetUserGroups.GetUserGroupsRequest CreateGetUserGroupsRequest(string? userId = null)
    // {
    //     ArgumentNullException.ThrowIfNull(userId, nameof(userId));
    //     return new GetUserGroups.GetUserGroupsRequest(userId);
    // }

    public static RejectUserJoinRequest.RejectUserJoinRequestDto CreateRejectUserJoinRequestDto(
        string groupId, string userId)
    {
        return new RejectUserJoinRequest.RejectUserJoinRequestDto
        {
            GroupId = groupId,
            UserId = userId
        };
    }
    
    public static JoinGroup.JoinGroupRequest CreateJoinGroupRequest(string groupCode)
    {
        return new JoinGroup.JoinGroupRequest
        {
            GroupCode = groupCode,
        };
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

    public static Recommendation CreateRecommendation(
        string id, string groupId, string userId, string title, string content, DateTime createdAt)
    {
        return new Recommendation
        {
            Id = id,
            GroupId = groupId,
            UserId = userId,
            Title = title,
            Content = content,
            CreatedAt = createdAt
        };
    }

    public static RecommendationComment CreateRecommendationComment(
        string id, string recommendationId, string userId, string content, DateTime createdAt)
    {
        return new RecommendationComment
        {
            Id = id,
            RecommendationId = recommendationId,
            UserId = userId,
            Content = content,
            CreatedAt = createdAt
        };
    }

    public static RecommendationReaction CreateRecommendationReaction(string recommendationId, string userId)
    {
        return new RecommendationReaction
        {
            RecommendationId = recommendationId,
            UserId = userId,
        };
    }

    public static PostRecommendation.RecommendationRequestDto CreateRecommendationRequestDto(
        string title, string content, string? category = null)
    {
        return new PostRecommendation.RecommendationRequestDto
        {
            Title = title,
            Content = content,
            Category = category
        };
    }

    public static UpdateRecommendation.UpdateRecommendationDto CreateUpdateRecommendationDto(
        string title, string content, string? category = null, string? imageUrl = null, string? linkUrl = null)
    {
        return new UpdateRecommendation.UpdateRecommendationDto
        {
            Title = title,
            Content = content,
            Category = category,
            ImageUrl = imageUrl,
            LinkUrl = linkUrl
        };
    }

    public static PostRecommendationComment.CommentRequestDto CreateCommentRequestDto(string content)
    {
        return new PostRecommendationComment.CommentRequestDto
        {
            Content = content
        };
    }
    
    private static string GenerateUniqueCode()
    {
        return Guid.NewGuid().ToString()[..8].ToUpper();
    }
}
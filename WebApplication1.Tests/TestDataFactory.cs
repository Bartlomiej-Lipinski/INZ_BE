using Microsoft.AspNetCore.Http;
using WebApplication1.Features.Auth;
using WebApplication1.Features.Comments.Dtos;
using WebApplication1.Features.Events.Dtos;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Features.Polls.Dtos;
using WebApplication1.Features.Recommendations.Dtos;
using WebApplication1.Features.Settlements.Dtos;
using WebApplication1.Features.Timeline.Dtos;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Infrastructure.Data.Entities.Comments;
using WebApplication1.Infrastructure.Data.Entities.Events;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Infrastructure.Data.Entities.Polls;
using WebApplication1.Infrastructure.Data.Entities.Settlements;
using WebApplication1.Infrastructure.Data.Entities.Storage;
using WebApplication1.Infrastructure.Data.Enums;

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

    public static GroupRequestDto CreateGroupRequestDto(string? name = null, string? color = null)
    {
        return new GroupRequestDto
        {
            Name = name ?? "TestGroup",
            Color = color ?? "Red"
        };
    }

    public static GroupResponseDto CreateGroupResponseDto(string groupId, string name)
    {
        return new GroupResponseDto
        {
            Id = groupId,
            Name = name
        };
    }

    public static AcceptUserJoinRequestDto CreateAcceptUserJoinRequestDto(
        string? groupId, string? userId)
    {
        ArgumentNullException.ThrowIfNull(groupId, nameof(groupId));
        ArgumentNullException.ThrowIfNull(userId, nameof(userId));
        return new AcceptUserJoinRequestDto
        {
            GroupId = groupId,
            UserId = userId
        };
    }

    public static GrantAdminPrivilegesDto CreateGrantAdminPrivilegesDto(string groupId, string userId)
    {
        return new GrantAdminPrivilegesDto
        {
            GroupId = groupId,
            UserId = userId
        };
    }

    public static User CreateUser(
        string? id = null,
        string? name = null,
        string? email = null,
        string? userName = null,
        string? surname = null, 
        DateOnly? birthDate = null)
    {
        ArgumentNullException.ThrowIfNull(id, nameof(id));
        return new User
        {
            Id = id,
            Name = name,
            Email = email,
            UserName = userName ?? name,
            Surname = surname,
            BirthDate = birthDate
        };
    }

    public static RejectUserJoinRequestDto CreateRejectUserJoinRequestDto(string userId)
    {
        return new RejectUserJoinRequestDto
        {
            UserId = userId
        };
    }

    public static JoinGroupRequestDto CreateJoinGroupRequest(string groupCode)
    {
        return new JoinGroupRequestDto
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

    public static Comment CreateComment(
        string id, string targetId, EntityType entityType, string userId, string content, DateTime createdAt)
    {
        return new Comment
        {
            Id = id,
            TargetId = targetId,
            EntityType = entityType,
            UserId = userId,
            Content = content,
            CreatedAt = createdAt
        };
    }

    public static Reaction CreateReaction(string targetId, EntityType entityType, string userId)
    {
        return new Reaction
        {
            TargetId = targetId,
            EntityType = entityType,
            UserId = userId,
        };
    }

    public static RecommendationRequestDto CreateRecommendationRequestDto(
        string title, string content, string? category = null, string? imageUrl = null, string? linkUrl = null)
    {
        return new RecommendationRequestDto
        {
            Title = title,
            Content = content,
            Category = category,
            ImageUrl = imageUrl,
            LinkUrl = linkUrl
        };
    }

    public static CommentRequestDto CreateCommentRequestDto(string content, string targetType = "Recommendation")
    {
        return new CommentRequestDto
        {
            EntityType = targetType,
            Content = content
        };
    }

    public static Event CreateEvent(
        string id,
        string groupId,
        string userId,
        string title,
        string? description,
        DateTime? startDate, 
        string? location, 
        DateTime createdAt)
    {
        return new Event
        {
            Id = id,
            GroupId = groupId,
            UserId = userId,
            Title = title,
            StartDate = startDate,
            Description = description,
            Location = location,
            CreatedAt = createdAt
        };
    }

    public static EventRequestDto CreateEventRequestDto(
        string title,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? description = null,
        string? location = null)
    {
        return new EventRequestDto
        {
            Title = title,
            StartDate = startDate,
            EndDate = endDate,
            Description = description,
            Location = location
        };
    }

    public static EventAvailability CreateEventAvailability(
        string eventId, string userId, EventAvailabilityStatus status, DateTime createdAt)
    {
        return new EventAvailability
        {
            EventId = eventId,
            UserId = userId,
            Status = status,
            CreatedAt = createdAt
        };
    }

    public static EventAvailabilityRequestDto CreateEventAvailabilityRequestDto(
        EventAvailabilityStatus status)
    {
        return new EventAvailabilityRequestDto
        {
            Status = status
        };
    }

    public static EventAvailabilityRange CreateEventAvailabilityRange(
        string eventId, string userId, DateTime availableFrom, DateTime availableTo)
    {
        return new EventAvailabilityRange
        {
            Id = Guid.NewGuid().ToString(),
            EventId = eventId,
            UserId = userId,
            AvailableFrom = availableFrom,
            AvailableTo = availableTo
        };
    }

    public static List<AvailabilityRangeRequestDto> CreateAvailabilityRangeRequestDto(
        DateTime startTime,
        int numberOfRanges = 1,
        int rangeLengthHours = 2,
        int gapBetweenRangesHours = 1)
    {
        var list = new List<AvailabilityRangeRequestDto>();

        for (var i = 0; i < numberOfRanges; i++)
        {
            var from = startTime.AddHours(i * (rangeLengthHours + gapBetweenRangesHours));
            var to = from.AddHours(rangeLengthHours);

            list.Add(new AvailabilityRangeRequestDto
            {
                AvailableFrom = from,
                AvailableTo = to
            });
        }

        return list;
    }

    public static EventSuggestion CreateEventSuggestion(
        string id, string eventId, DateTime startTime, int availableUserCount)
    {
        return new EventSuggestion
        {
            Id = id,
            EventId = eventId,
            StartTime = startTime,
            AvailableUserCount = availableUserCount
        };
    }

    public static StoredFile CreateStoredFile(
        string id,
        string fileName,
        string contentType,
        int size,
        string url,
        DateTime uploadedAt,
        string entityId,
        string entityType,
        string uploadedBy)
    {
        return new StoredFile
        {
            Id = id,
            FileName = fileName,
            ContentType = contentType,
            Size = size,
            Url = url,
            UploadedAt = uploadedAt,
            EntityId = entityId,
            EntityType = entityType,
            UploadedById = uploadedBy
        };
    }

    public static IFormFile CreateFormFile(string fileName, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };
    }

    public static Expense CreateExpense(
        string id, string groupId, string paidByUserId, string title, decimal amount, bool isEvenSplit)
    {
        return new Expense
        {
            Id = id,
            GroupId = groupId,
            PaidByUserId = paidByUserId,
            Title = title,
            Amount = amount,
            IsEvenSplit = isEvenSplit
        };
    }

    public static ExpenseBeneficiary CreateExpenseBeneficiary(string expenseId, string userId, decimal share)
    {
        return new ExpenseBeneficiary
        {
            ExpenseId = expenseId,
            UserId = userId,
            Share = share
        };
    }

    public static Settlement CreateSettlement(
        string id, string groupId, string fromUserId, string toUserId, decimal amount)
    {
        return new Settlement
        {
            Id = id,
            GroupId = groupId,
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Amount = amount
        };
    }

    public static ExpenseRequestDto CreateExpenseRequestDto(
        string title, string paidByUserId, decimal amount, bool isEvenSplit, List<ExpenseBeneficiaryDto> beneficiaries)
    {
        return new ExpenseRequestDto
        {
            Title = title,
            PaidByUserId = paidByUserId,
            Amount = amount,
            IsEvenSplit = isEvenSplit,
            Beneficiaries = beneficiaries
        };
    }

    public static Poll CreatePoll(string id, string groupId, string userId, string question)
    {
        return new Poll
        {
            Id = id,
            GroupId = groupId,
            CreatedByUserId = userId,
            Question = question
        };
    }

    public static PollOption CreatePollOption(string id, string pollId, string text)
    {
        return new PollOption
        {
            Id = id,
            PollId = pollId,
            Text = text
        };
    }

    public static PollRequestDto CreatePollRequestDto(string question, List<PollOptionDto> options)
    {
        return new PollRequestDto
        {
            Question = question,
            Options = options
        };
    }

    public static TimelineEvent CreateTimelineEvent(string id, string groupId, string title, DateTime date)
    {
        return new TimelineEvent
        {
            Id = id,
            GroupId = groupId,
            Title = title,
            Date = date
        };
    }

    public static TimelineEventRequestDto CreateTimelineEventRequestDto(
        string title, DateTime date, string? description = null)
    {
        return new TimelineEventRequestDto
        {
            Title = title,
            Date = date,
            Description = description
        };
    }
    
    private static string GenerateUniqueCode()
    {
        return Guid.NewGuid().ToString()[..8].ToUpper();
    }
}
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Features.Challenges.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Challenges;
using WebApplication1.Infrastructure.Data.Enums;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Challenges;

public class PostChallenge : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/challenges", Handle)
            .WithName("PostChallenge")
            .WithDescription("Creates a new challenge for a group")
            .WithTags("Challenges")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromBody] ChallengeRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostChallenge> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("Attempting to create challenge in group {GroupId} by user {UserId}. TraceId: {TraceId}",
            groupId, userId, traceId);
        
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Description))
        {
            logger.LogWarning("Challenge creation failed: name and description are required. User {UserId}, " +
                              "Group {GroupId}, TraceId: {TraceId}", userId, groupId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Challenge name and description are required.",
                traceId));
        }

        if (request.EndDate != null)
        {
            if (request.EndDate < request.StartDate)
            {
                logger.LogWarning("Challenge creation failed: end date before start date. User {UserId}, Group {GroupId}," +
                                  " TraceId: {TraceId}", userId, groupId, traceId);
                return Results.BadRequest(ApiResponse<string>
                    .Fail("Range end cannot be earlier than range start.", traceId));
            }
        }

        var challenge = new Challenge
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            UserId = userId!,
            EntityType = EntityType.Challenge,
            Name = request.Name,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PointsPerUnit = request.PointsPerUnit,
            Unit = request.Unit,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };
        
        dbContext.Challenges.Add(challenge);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("User {UserId} created challenge {ChallengeId} in group {GroupId}. TraceId: {TraceId}",
            userId, challenge.Id, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Challenge created successfully.", challenge.Id, traceId));
    }
}
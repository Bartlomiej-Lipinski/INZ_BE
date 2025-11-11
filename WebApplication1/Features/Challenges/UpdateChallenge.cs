using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Challenges.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Challenges;

public class UpdateChallenge : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}/challenges/{challengeId}", Handle)
            .WithName("UpdateChallenge")
            .WithDescription("Updates a specific challenge in a group")
            .WithTags("Challenges")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string challengeId,
        [FromBody] ChallengeRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<UpdateChallenge> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("Attempting to update challenge in group {GroupId} by user {UserId}. TraceId: {TraceId}",
            groupId, userId, traceId);

        var existingChallenge = await dbContext.Challenges
            .SingleOrDefaultAsync(c => c.Id == challengeId && c.GroupId == groupId, cancellationToken);
        
        if (existingChallenge == null)
        {
            logger.LogWarning("Challenge not found. User {UserId}, ChallengeId {ChallengeId}, Group {GroupId}, TraceId: {TraceId}",
                userId, challengeId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Challenge not found.", traceId));
        }
        
        if (existingChallenge.UserId != userId)
        {
            logger.LogWarning("User {UserId} attempted to update challenge {ChallengeId} not created by them. TraceId: {TraceId}", 
                userId, challengeId, traceId);
            return Results.Forbid();
        }
        
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Description))
        {
            logger.LogWarning("Challenge update failed: name and description are required. User {UserId}, " +
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

        existingChallenge.Name = request.Name;
        existingChallenge.Description = request.Description;
        existingChallenge.StartDate = request.StartDate;
        existingChallenge.EndDate = request.EndDate;
        existingChallenge.PointsPerUnit = request.PointsPerUnit;
        existingChallenge.Unit = request.Unit;
        existingChallenge.IsCompleted = request.IsCompleted ?? false;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} updated challenge {ChallengeId} in group {GroupId}. TraceId: {TraceId}",
            userId, challengeId, groupId, traceId);
        
        return Results.Ok(ApiResponse<string>.Ok("Challenge updated successfully.", challengeId, traceId));
    }
}
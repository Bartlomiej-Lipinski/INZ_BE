using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups.JoinGroupFeatures;

public class GenerateCodeToJoinGroup : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}/join-code-generate", Handle)
            .WithName("GenerateCodeToJoinGroup")
            .WithDescription("Generates a new code to join a group")
            .WithTags("Groups")
            .RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<GenerateCodeToJoinGroup> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("Unauthorized attempt to generate code. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(groupId))
        {
            logger.LogWarning("Group ID is null or empty. TraceId: {TraceId}", traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Group ID cannot be null or empty.", traceId));
        }

        var group = await dbContext.Groups
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group not found. GroupId: {GroupId}, TraceId: {TraceId}", groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }
        
        var groupUser = group.GroupUsers
            .FirstOrDefault(gu => gu.UserId == userId && gu.AcceptanceStatus == AcceptanceStatus.Accepted);
        if (groupUser == null)
        {
            logger.LogWarning("User {UserId} attempted to generate code in group {GroupId} but is not a member. " +
                              "TraceId: {TraceId}", userId, groupId, traceId);
            return Results.Forbid();
        }
        
        group.Code = GenerateUniqueCode(dbContext, group.Id);
        group.CodeExpirationTime = DateTime.UtcNow.AddMinutes(5);
        dbContext.Groups.Update(group);
        
        var updated = await dbContext.SaveChangesAsync(cancellationToken);

        if (updated > 0)
        {
            logger.LogInformation("New join code generated. GroupId: {GroupId}, TraceId: {TraceId}", groupId, traceId);
            return Results
                .Ok(ApiResponse<string>
                    .Ok("New code generated successfully. The code is valid for 5 minutes."+ $" Code: {group.Code}",
                        "Code Generated", traceId));
        }
        
        logger.LogError("Failed to generate code. GroupId: {GroupId}, TraceId: {TraceId}", groupId, traceId);
        return Results.Json(ApiResponse<string>.Fail("Failed to generate code.", traceId), statusCode: 500);
    }

    private static string GenerateUniqueCode(AppDbContext dbContext, string groupId)
    {
        string code;
        do
        {
            var random = new Random();
            code = random.Next(10000, 99999).ToString();
        } while (dbContext.Groups.Any(g => g.Code == code && g.Id != groupId) 
                 || dbContext.Groups.Any(g => g.CodeExpirationTime > DateTime.UtcNow && g.Code == code));
        return code;
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;

namespace WebApplication1.Features.Groups;

public class RejectUserJoinRequest : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/reject-join-request/{userId}", Handle)
            .WithName("RejectUserJoinRequest")
            .WithDescription("Rejects a user's join request to a group")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }
    
    public static async Task<IResult> Handle(
        [FromBody] RejectUserJoinRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        ILogger<RejectUserJoinRequest> logger,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        
        var currentUserId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? currentUser.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(currentUserId))
        {
            logger.LogWarning("Unauthorized attempt to reject join request. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }
        var currentGroupUser = await dbContext.GroupUsers
            .FirstOrDefaultAsync(gu => gu.GroupId == request.GroupId && gu.UserId == currentUserId, cancellationToken);

        var isAdmin = currentGroupUser?.IsAdmin == true;
        if (!isAdmin)
        {
            logger.LogWarning("User {UserId} is not admin of group {GroupId}. TraceId: {TraceId}", 
                currentUserId, request.GroupId, traceId);
            return Results.BadRequest(ApiResponse<string>
                .Fail("Only group admin can reject join requests.", traceId));
        }
        
        var groupUser = await dbContext.GroupUsers
            .FirstOrDefaultAsync(gu => gu.GroupId == request.GroupId && gu.UserId == request.UserId, cancellationToken);

        if (groupUser == null)
        {
            logger.LogWarning("Join request not found. GroupId: {GroupId}, UserId: {UserId}. TraceId: {TraceId}",
                request.GroupId, request.UserId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Join request not found.", traceId));
        }

        if (groupUser.AcceptanceStatus != AcceptanceStatus.Pending)
        {
            logger.LogWarning("Join request is not pending. GroupId: {GroupId}, UserId: {UserId}. TraceId: {TraceId}",
                request.GroupId, request.UserId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Join request is not pending.", traceId));
        }

        dbContext.GroupUsers.Remove(groupUser);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Join request rejected successfully. " +
                              "GroupId: {GroupId}, UserId: {UserId}. TraceId: {TraceId}", 
            request.GroupId, request.UserId, traceId);
        return Results.Ok(ApiResponse<string>.Ok("Join request rejected successfully.", null, traceId));
    }
    
    public record RejectUserJoinRequestDto
    {
        [Required]
        [MaxLength(50)]
        public string GroupId { get; set; }
        [Required]
        [MaxLength(50)]
        public string UserId { get; set; } 
    }
}
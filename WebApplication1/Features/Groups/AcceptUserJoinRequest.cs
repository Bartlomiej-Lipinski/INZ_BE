﻿using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

public class AcceptUserJoinRequest : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/accept-join-request", Handle)
            .WithName("AcceptUserJoinRequest")
            .WithDescription("Accepts a user's join request to a group")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }
    
    public static async Task<IResult> Handle(
        [FromBody] AcceptUserJoinRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal? user,
        CancellationToken cancellationToken,
        HttpContext httpContext,
        ILogger<AcceptUserJoinRequest> logger)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        
        if (user == null)
        {
            logger.LogWarning("Null user principal in AcceptUserJoinRequest. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }
        
        var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                            ?? user.FindFirst("sub")?.Value;
        
        if (string.IsNullOrEmpty(currentUserId))
        {
            logger.LogWarning("No user ID found in claims for AcceptUserJoinRequest. TraceId: {TraceId}", traceId);
            return Results.Unauthorized();
        }
        
        logger.LogInformation("Processing join request acceptance. GroupId: {GroupId}, UserId: {UserId}, AdminId: {AdminId}. TraceId: {TraceId}", 
            request.GroupId, request.UserId, currentUserId, traceId);
        
        var admin = await dbContext.GroupUsers
            .FirstOrDefaultAsync(gu => gu.GroupId == request.GroupId && gu.UserId == currentUserId && gu.IsAdmin, cancellationToken);
        
        if (admin == null)
        {
            logger.LogWarning("User {UserId} is not admin of group {GroupId}. TraceId: {TraceId}", 
                currentUserId, request.GroupId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Only group admin can accept join requests.", traceId));
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
            logger.LogWarning("Join request is not pending. GroupId: {GroupId}, UserId: {UserId}, Status: {Status}. TraceId: {TraceId}", 
                request.GroupId, request.UserId, groupUser.AcceptanceStatus, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Join request is not pending.", traceId));
        }

        groupUser.AcceptanceStatus = AcceptanceStatus.Accepted;
        dbContext.GroupUsers.Update(groupUser);
        var saved = await dbContext.SaveChangesAsync(cancellationToken);

        if (saved > 0)
        {
            logger.LogInformation("Join request accepted successfully. GroupId: {GroupId}, UserId: {UserId}. TraceId: {TraceId}", 
                request.GroupId, request.UserId, traceId);
            return Results.Ok(ApiResponse<string>.Ok("Join request accepted successfully.", null, traceId));
        }

        logger.LogError("Failed to save join request acceptance. GroupId: {GroupId}, UserId: {UserId}. TraceId: {TraceId}", 
            request.GroupId, request.UserId, traceId);
        return Results.Json(ApiResponse<string>.Fail("Failed to accept join request.", traceId), statusCode: 500);
    }
    public record AcceptUserJoinRequestDto
    {
        [Required]
        [MaxLength(50)]
        public string GroupId { get; init; } = null!;
        [Required]
        [MaxLength(50)]
        public string UserId { get; init; } = null!;
    }
}
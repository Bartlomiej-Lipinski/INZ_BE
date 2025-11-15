using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Groups.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups.JoinGroupFeatures;

public class JoinGroup : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/join", Handle)
            .WithName("JoinGroup")
            .WithDescription("Allows a user to join a group")
            .WithTags("Groups")
            .RequireAuthorization();
    }
    
    public static async Task<IResult> Handle(
        [FromBody] JoinGroupRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<JoinGroup> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} attempting to join group with code {Code}. TraceId: {TraceId}",
            userId, request.GroupCode, traceId);
        
        if (string.IsNullOrWhiteSpace(request.GroupCode))
        {
            logger.LogWarning("Invalid join request: empty code. UserId: {UserId}, TraceId: {TraceId}", 
                userId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Group code is required.", traceId));
        }

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g =>  g.Code == request.GroupCode, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group not found or invalid code. Code: {Code}, UserId: {UserId}, TraceId: {TraceId}", 
                request.GroupCode, userId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found or code is invalid.", traceId));
        }
        
        if (group.CodeExpirationTime < DateTime.UtcNow)
        {
            logger.LogWarning("Code expired. GroupId: {GroupId}, UserId: {UserId}, TraceId: {TraceId}", 
                group.Id, userId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("The code has expired.", traceId));
        }
        
        if (await dbContext.GroupUsers.AnyAsync(gu => gu.GroupId == group.Id && gu.UserId == userId, cancellationToken))
        {
            logger.LogWarning("User already a member. GroupId: {GroupId}, UserId: {UserId}, TraceId: {TraceId}", 
                group.Id, userId, traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("You are already a member of this group.", traceId));
        }

        var groupUser = new GroupUser
        {
            GroupId = group.Id,
            UserId = userId!,
            IsAdmin = false,
            AcceptanceStatus = AcceptanceStatus.Pending
        };

        await dbContext.GroupUsers.AddAsync(groupUser, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} successfully requested to join group {GroupId}. TraceId: {TraceId}", 
            userId, group.Id, traceId);
        return Results.Ok(ApiResponse<string>.Ok("Successfully joined the group. Awaiting admin approval.",
            null, traceId));
    }
}
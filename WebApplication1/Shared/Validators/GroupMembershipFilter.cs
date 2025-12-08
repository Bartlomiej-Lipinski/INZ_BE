using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Shared.Validators;

public class GroupMembershipFilter(AppDbContext dbContext, ILogger<GroupMembershipFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = httpContext.User.GetUserId();
        var groupId = ResolveGroupId(context);

        if (string.IsNullOrWhiteSpace(groupId))
        {
            logger.LogWarning("GroupId could not be resolved from request. TraceId: {TraceId}", traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("GroupId is required.", traceId));
        }
        
        var group = await dbContext.Groups
            .AsNoTracking()
            .Include(g => g.GroupUsers)
            .SingleOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
        {
            logger.LogWarning("Group {GroupId} not found. TraceId: {TraceId}", groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }

        var groupUser = group.GroupUsers
            .SingleOrDefault(gu => gu.UserId == userId && gu.AcceptanceStatus == AcceptanceStatus.Accepted);

        httpContext.Items["GroupUser"] = groupUser;

        if (groupUser != null) return await next(context);
        logger.LogWarning("User {UserId} is not a member of group {GroupId}. TraceId: {TraceId}",
            userId, groupId, traceId);
        return Results.Forbid();
    }

    private static string? ResolveGroupId(EndpointFilterInvocationContext context)
    {
        var httpContext = context.HttpContext;
        if (httpContext.Request.RouteValues.TryGetValue("groupId", out var routeValue))
        {
            var routeGroupId = routeValue?.ToString();
            if (!string.IsNullOrWhiteSpace(routeGroupId))
            {
                return routeGroupId;
            }
        }

        if (context.Arguments.Count > 0 &&
            context.Arguments[0] is string directGroupId &&
            !string.IsNullOrWhiteSpace(directGroupId))
        {
            return directGroupId;
        }

        foreach (var argument in context.Arguments)
        {
            if (argument is null) continue;

            var groupIdProperty = argument.GetType().GetProperty("GroupId");
            if (groupIdProperty?.GetValue(argument) is string dtoGroupId &&
                !string.IsNullOrWhiteSpace(dtoGroupId))
            {
                return dtoGroupId;
            }
        }

        return null;
    }
}
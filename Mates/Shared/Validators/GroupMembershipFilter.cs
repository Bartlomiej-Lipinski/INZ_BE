using System.Diagnostics;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities.Groups;
using Mates.Shared.Responses;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Shared.Validators;

public class GroupMembershipFilter(AppDbContext dbContext, ILogger<GroupMembershipFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = httpContext.User.GetUserId();
        var groupId = context.GetArgument<string>(0);

        
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

}
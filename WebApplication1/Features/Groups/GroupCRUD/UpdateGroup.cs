using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

public class UpdateGroup : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}", Handle)
            .WithName("UpdateGroup")
            .WithDescription("Updates group details")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }
    
    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromBody] UpdateGroupRequest request,
        AppDbContext dbContext,
        HttpContext httpContext,
        ILogger<UpdateGroup> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group not found. GroupId: {GroupId}, TraceId: {TraceId}", groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }
 
        group.Name = request.Name ?? group.Name;
        group.Color = request.Color ?? group.Color;

        dbContext.Groups.Update(group);
        var updated = await dbContext.SaveChangesAsync(cancellationToken);

        if (updated > 0)
        {
            logger.LogInformation("Group updated successfully. GroupId: {GroupId}, TraceId: {TraceId}", groupId, traceId);
            return Results.Ok(ApiResponse<string>.Ok("Group updated successfully.", "Group Updated", traceId));
        }

        logger.LogError("Failed to update group. GroupId: {GroupId}, TraceId: {TraceId}", groupId, traceId);
        return Results.Json(ApiResponse<string>.Fail("Failed to update group.", traceId), statusCode: 500);
    }
    
    public record UpdateGroupRequest
    {
        public string? Name { get; init; }
        public string? Color { get; init; }
    }
}
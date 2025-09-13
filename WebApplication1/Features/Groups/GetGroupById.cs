using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

[ApiExplorerSettings(GroupName = "Groups")]
public class GetGroupById : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{id}", Handle)
            .WithName("GetGroupById")
            .WithDescription("Returns a group by ID")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string id,
        AppDbContext dbContext,
        CancellationToken cancellationToken,
        HttpContext httpContext,
        ILogger<GetGroupById> logger)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (string.IsNullOrWhiteSpace(id))
        {
            logger.LogWarning("Invalid group ID provided. TraceId: {TraceId}", traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Group ID cannot be null or empty.", traceId));
        }

        logger.LogInformation("Fetching group with ID: {GroupId}. TraceId: {TraceId}", id, traceId);

        var group = await dbContext.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group not found with ID: {GroupId}. TraceId: {TraceId}", id, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found", traceId));
        }

        var dto = new GroupResponseDto
        {
            Id = group.Id,
            Name = group.Name,
            Color = group.Color,
            Code = group.Code
        };

        logger.LogInformation("Group successfully retrieved with ID: {GroupId}. TraceId: {TraceId}", id, traceId);
        return Results.Ok(ApiResponse<GroupResponseDto>.Ok(dto, null, traceId));
    }

    public class GroupResponseDto
    {
        [MaxLength(50)]
        public string Id { get; set; } = null!;
        [MaxLength(50)]
        public string Name { get; set; } = null!;
        [MaxLength(7)]
        public string Color { get; set; } = null!;
        [MaxLength(5)]
        public string Code { get; set; } = null!;
    }
}
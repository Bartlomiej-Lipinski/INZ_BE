using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;

namespace WebApplication1.Features.Groups;

public class GenerateCodeToJoinGroup : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{id}/join-code-generate", Handle)
            .WithName("GenerateCodeToJoinGroup")
            .WithDescription("Generates a new code to join a group")
            .WithTags("Groups")
            .RequireAuthorization()
            .WithOpenApi();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string id,
        AppDbContext dbContext,
        HttpContext httpContext,
        ILogger<GenerateCodeToJoinGroup> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (string.IsNullOrWhiteSpace(id))
        {
            logger.LogWarning("Group ID is null or empty. TraceId: {TraceId}", traceId);
            return Results.BadRequest(ApiResponse<string>.Fail("Group ID cannot be null or empty.", traceId));
        }

        var group = await dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (group == null)
        {
            logger.LogWarning("Group not found. GroupId: {GroupId}, TraceId: {TraceId}", id, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Group not found.", traceId));
        }

        group.Code = GenerateUniqueCode(dbContext, group.Id);
        group.CodeExpirationTime = DateTime.UtcNow.AddMinutes(5);
        dbContext.Groups.Update(group);
        
        var updated = await dbContext.SaveChangesAsync(cancellationToken);

        if (updated > 0)
        {
            logger.LogInformation("New join code generated. GroupId: {GroupId}, TraceId: {TraceId}", id, traceId);
            return Results.Ok(ApiResponse<GenerateCodeResponse>.Ok(
                new GenerateCodeResponse("New code generated successfully. The code is valid for 5 minutes."+ $" Code: {group.Code}"), "Code Generated", traceId));
        }
        
        logger.LogError("Failed to generate code. GroupId: {GroupId}, TraceId: {TraceId}", id, traceId);
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
                 || dbContext.Groups.Any(g => g.CodeExpirationTime > DateTime.UtcNow && g.Code == code)
                 );

        return code;
    }
    
    public record GenerateCodeResponse(string Message);
}
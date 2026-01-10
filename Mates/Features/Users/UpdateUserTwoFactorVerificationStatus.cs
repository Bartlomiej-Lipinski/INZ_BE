using System.Diagnostics;
using System.Security.Claims;
using Mates.Infrastructure.Data.Context;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using Mates.Shared.Extensions;

namespace Mates.Features.Users;

public class UpdateUserTwoFactorVerificationStatus : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/users/two-factor-verification/{flag}", Handle)
        .WithTags("Users")
        .WithName("UpdateUserTwoFactorVerificationStatus")
        .WithSummary("Updates the two-factor verification status of a user.")
        .WithDescription("Enables or disables two-factor verification for a specified user.")
        .RequireAuthorization();
    }

    public static async Task<IResult> Handle(
        [FromRoute] bool flag,
        ClaimsPrincipal currentUser,
        AppDbContext dbContext,
        HttpContext httpContext,
        ILogger<UpdateUserTwoFactorVerificationStatus> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation("Attempting to update two-factor status for user ID: {UserId}. TraceId: {TraceId}",
            userId, traceId);

        var user = await dbContext.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User not found for two-factor status update with ID: {UserId}. TraceId: {TraceId}",
                userId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("User not found.", traceId));
        }

        if (user.TwoFactorEnabled == flag)
        {
            logger.LogWarning("No change in two-factor verification status for user ID: {UserId}. TraceId: {TraceId}",
                userId, traceId);
            return Results.BadRequest(ApiResponse<string>
                .Fail("Two-factor verification status is already set to the specified value.", traceId));
        }

        user.TwoFactorEnabled = flag;
        var updated = await dbContext.SaveChangesAsync(cancellationToken);

        if (updated > 0)
        {
            logger.LogInformation("Two-factor verification status successfully updated for ID: {UserId} to {Status}." +
                                  " TraceId: {TraceId}", userId, flag, traceId);
            return Results
                .Ok(ApiResponse<string>
                    .Ok("Two-factor verification status updated successfully.", null, traceId));
        }

        logger.LogError("No changes were saved for two-factor status update. UserId: {UserId}, TraceId: {TraceId}", 
            userId, traceId);
        return Results.Json(ApiResponse<string>.Fail("No changes were saved.", traceId), statusCode: 500);
    }
}
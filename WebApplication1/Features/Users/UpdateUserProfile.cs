using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Globalization;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Users.Dtos;
using WebApplication1.Shared.Extensions;

namespace WebApplication1.Features.Users;

public class UpdateUserProfile : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/users/profile", Handle)
            .WithName("UpdateUserProfile")
            .WithDescription("Updates a user's profile")
            .WithTags("Users")
            .RequireAuthorization();
    }
    
    public static async Task<IResult> Handle(
        [FromBody] UserProfileRequestDto request,
        ClaimsPrincipal currentUser,
        AppDbContext dbContext,
        HttpContext httpContext,
        ILogger<UpdateUserProfile> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation("Attempting to update profile for user ID: {UserId}. TraceId: {TraceId}", userId, traceId);

        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null)
        {
            logger.LogWarning("User not found for profile update with ID: {UserId}. TraceId: {TraceId}", userId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("User not found.", traceId));
        }

        user.Name = request.Name;
        user.Surname = request.Surname;
        user.BirthDate = request.BirthDate;
        user.Status = request.Status;
        user.Description = request.Description;
        user.UserName = RemoveDiacritics(request.UserName!).ToLowerInvariant();

        dbContext.Users.Update(user);
        var updated = await dbContext.SaveChangesAsync(cancellationToken);

        if (updated > 0)
        {
            logger.LogInformation("User profile successfully updated for ID: {UserId}. TraceId: {TraceId}",
                userId, traceId);
            return Results.Ok(ApiResponse<string>.Ok("User profile updated successfully.", traceId));
        }

        logger.LogError("No changes were saved for user profile update. UserId: {UserId}, TraceId: {TraceId}", 
            userId, traceId);
        return Results.Json(ApiResponse<string>.Fail("No changes were saved.", traceId), statusCode: 500);
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var normalized = text.Normalize(NormalizationForm.FormD);
        return string.Concat(
            normalized.Where(c => char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
        ).Normalize(NormalizationForm.FormC);
    }
}
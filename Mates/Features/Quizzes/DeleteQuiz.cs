using System.Diagnostics;
using System.Security.Claims;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities.Groups;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Quizzes;

public class DeleteQuiz : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/groups/{groupId}/quizzes/{quizId}", Handle)
            .WithName("DeleteQuiz")
            .WithDescription("Deletes a specific quiz from a group")
            .WithTags("Quizzes")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string quizId,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<DeleteQuiz> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation(
            "Attempting to delete quiz {QuizId} in group {GroupId} by user {UserId}. TraceId: {TraceId}",
            quizId, groupId, userId, traceId);

        var quiz = await dbContext.Quizzes
            .SingleOrDefaultAsync(q => q.Id == quizId && q.GroupId == groupId, cancellationToken);
        
        if (quiz == null)
        {
            logger.LogWarning("Quiz {QuizId} not found in group {GroupId}. TraceId: {TraceId}", quizId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Quiz not found.", traceId));
        }
        
        var groupUser = httpContext.Items["GroupUser"] as GroupUser;
        var isAdmin = groupUser?.IsAdmin ?? false;
        if (quiz.UserId != userId && !isAdmin)
        {
            logger.LogWarning("User {UserId} attempted to delete quiz {QuizId} they do not own and is not admin. " +
                              "TraceId: {TraceId}", userId, quizId, traceId);
            return Results.Forbid();
        }
        
        dbContext.Quizzes.Remove(quiz);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("User {UserId} deleted quiz {QuizId} from group {GroupId}. TraceId: {TraceId}",
            userId, quizId, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Quiz deleted successfully.", quizId, traceId));
    }
}
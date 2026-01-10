using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Quizzes.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Quizzes.Attempts;

public class GetUsersLastQuizAttempt : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/quizzes/{quizId}/attempts/last", Handle)
            .WithName("GetUsersLastQuizAttempt")
            .WithDescription("Retrieves the user's most recent attempt for a quiz")
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
        ILogger<GetUsersLastQuizAttempt> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation("Fetching last quiz attempt for quiz {QuizId} by user {UserId} in group {GroupId}. TraceId: {TraceId}",
            quizId, userId, groupId, traceId);

        var attempt = await dbContext.QuizAttempts
            .AsNoTracking()
            .Include(a => a.Answers)
            .Where(a => a.QuizId == quizId && a.UserId == userId)
            .OrderByDescending(a => a.CompletedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (attempt == null)
        {
            logger.LogInformation("No attempt found for quiz {QuizId} by user {UserId}. TraceId: {TraceId}",
                quizId, userId, traceId);

            return Results.NotFound(ApiResponse<string>.Fail("No attempts found for this user.", traceId));
        }

        var response = new QuizAttemptResponseDto
        {
            AttemptId = attempt.Id,
            QuizId = attempt.QuizId,
            Score = attempt.Score,
            CompletedAt = attempt.CompletedAt,
            Answers = attempt.Answers.Select(a => new QuizAttemptAnswerResponseDto
            {
                QuestionId = a.QuestionId,
                SelectedOptionId = a.SelectedOptionId,
                SelectedTrueFalse = a.SelectedTrueFalse
            }).ToList()
        };
        
        logger.LogInformation("Last attempt {AttemptId} retrieved successfully. TraceId: {TraceId}", attempt.Id, traceId);
        return Results.Ok(ApiResponse<QuizAttemptResponseDto>.Ok(response, "Last attempt retrieved.", traceId));
    }
}
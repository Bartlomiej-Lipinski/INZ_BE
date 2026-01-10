using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Quizzes.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities.Quizzes;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mates.Shared.Extensions;

namespace Mates.Features.Quizzes.Attempts;

public class PostQuizAttempt : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/quizzes/{quizId}/attempts", Handle)
            .WithName("PostQuizAttempt")
            .WithDescription("Submits answers for a quiz and calculates the score")
            .WithTags("Quizzes")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string quizId,
        [FromBody] QuizAttemptRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostQuizAttempt> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation("User {UserId} submitting attempt for quiz {QuizId} in group {GroupId}. TraceId: {TraceId}",
            userId, quizId, groupId, traceId);

        var quiz = await dbContext.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .SingleOrDefaultAsync(q => q.Id == quizId && q.GroupId == groupId, cancellationToken);

        if (quiz == null)
        {
            logger.LogWarning("Quiz {QuizId} not found in group {GroupId}. TraceId: {TraceId}", quizId, groupId, traceId);
            return Results.NotFound(ApiResponse<string>.Fail("Quiz not found.", traceId));
        }

        if (quiz.Questions.Count != request.Answers.Count)
        {
            return Results.BadRequest(
                ApiResponse<string>.Fail("Number of answers does not match number of questions.", traceId));
        }

        var attempt = new QuizAttempt
        {
            Id = Guid.NewGuid().ToString(),
            QuizId = quiz.Id,
            UserId = userId!,
            CompletedAt = DateTime.UtcNow,
            Score = 0,
            Answers = new List<QuizAttemptAnswer>()
        };

        var score = 0;

        foreach (var answerDto in request.Answers)
        {
            var question = quiz.Questions.SingleOrDefault(q => q.Id == answerDto.QuestionId);
            if (question == null)
                continue;

            var attemptAnswer = new QuizAttemptAnswer
            {
                Id = Guid.NewGuid().ToString(),
                AttemptId = attempt.Id,
                QuestionId = question.Id,
                SelectedOptionId = null,
                SelectedTrueFalse = null
            };

            switch (question.Type)
            {
                case QuizQuestionType.SingleChoice:
                    attemptAnswer.SelectedOptionId = answerDto.SelectedOptionId;
                    if (question.Options.Any(o => o.Id == answerDto.SelectedOptionId && o.IsCorrect))
                        score++;
                    break;
                case QuizQuestionType.TrueFalse:
                    attemptAnswer.SelectedTrueFalse = answerDto.SelectedTrueFalse;
                    if (answerDto.SelectedTrueFalse == question.CorrectTrueFalse)
                        score++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            attempt.Answers.Add(attemptAnswer);
        }

        attempt.Score = score;

        dbContext.QuizAttempts.Add(attempt);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} completed quiz {QuizId} with score {Score}. TraceId: {TraceId}",
            userId, quizId, score, traceId);

        return Results.Ok(ApiResponse<int>.Ok(score, "Quiz attempt submitted successfully.", traceId));
    }
}
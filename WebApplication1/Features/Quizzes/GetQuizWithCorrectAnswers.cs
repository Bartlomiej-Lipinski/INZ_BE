using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Quizzes.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Quizzes;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Quizzes;

public class GetQuizWithCorrectAnswers : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/quizzes/{quizId}/answers", Handle)
            .WithName("GetQuizWithCorrectAnswers")
            .WithDescription("Retrieves quiz details with correct answers")
            .WithTags("Quizzes")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string quizId,
        AppDbContext dbContext,
        HttpContext httpContext,
        ILogger<GetQuizWithCorrectAnswers> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        logger.LogInformation("Fetching quiz {QuizId} with correct answers in group {GroupId}. TraceId: {TraceId}", 
            quizId, groupId, traceId);

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

        var response = new QuizResponseDto
        {
            Id = quiz.Id,
            GroupId = quiz.GroupId,
            Title = quiz.Title,
            Description = quiz.Description,
            CreatedAt = quiz.CreatedAt,
            Questions = quiz.Questions.Select(q => new QuizQuestionResponseDto
            {
                Id = q.Id,
                Type = q.Type,
                Content = q.Content,
                CorrectTrueFalse = q.Type == QuizQuestionType.TrueFalse ? q.CorrectTrueFalse : null,
                Options = q.Options.Select(o => new QuizAnswerOptionResponseDto
                {
                    Id = o.Id,
                    QuestionId = o.QuestionId,
                    Text = o.Text,
                    IsCorrect = q.Type == QuizQuestionType.SingleChoice ? o.IsCorrect : null
                }).ToList()
            }).ToList()
        };

        logger.LogInformation("Quiz {QuizId} with correct answers retrieved. TraceId: {TraceId}", quizId, traceId);
        return Results.Ok(ApiResponse<QuizResponseDto>.Ok(response, "Quiz retrieved with correct answers.", traceId));
    }
}
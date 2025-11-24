using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Features.Quizzes.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Quizzes;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Quizzes;

public class PostQuiz : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/groups/{groupId}/quizzes", Handle)
            .WithName("PostQuiz")
            .WithDescription("Creates a new quiz within a group by a member")
            .WithTags("Quizzes")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromBody] QuizRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<PostQuiz> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();
        
        logger.LogInformation("User {UserId} creating quiz in group {GroupId}. TraceId: {TraceId}",
            userId, groupId, traceId);
        
        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.BadRequest(ApiResponse<string>.Fail("Title is required.", traceId));

        if (request.Questions.Count == 0)
            return Results.BadRequest(ApiResponse<string>.Fail("Quiz must contain at least one question.", traceId));
        
        foreach (var q in request.Questions)
        {
            if (string.IsNullOrWhiteSpace(q.Content))
                return Results.BadRequest(ApiResponse<string>.Fail("Question content is required.", traceId));

            switch (q.Type)
            {
                case QuizQuestionType.SingleChoice when q.Options == null || q.Options.Count < 2:
                    return Results.BadRequest(ApiResponse<string>.Fail("SingleChoice question must have at least 2 options.", traceId));
                case QuizQuestionType.SingleChoice:
                {
                    var correctCount = q.Options.Count(o => o.IsCorrect);
                    if (correctCount != 1)
                        return Results.BadRequest(ApiResponse<string>.Fail("SingleChoice must have exactly 1 correct option.", traceId));
                    break;
                }
                case QuizQuestionType.TrueFalse: 
                    if (q.CorrectTrueFalse == null)
                        return Results.BadRequest(ApiResponse<string>.Fail("TrueFalse question must have correct answer defined.", traceId));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        var quiz = new Quiz
        {
            Id = Guid.NewGuid().ToString(),
            GroupId = groupId,
            UserId = userId!,
            Title = request.Title,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        quiz.Questions = request.Questions.Select(q =>
        {
            var questionId = Guid.NewGuid().ToString();
        
            return new QuizQuestion
            {
                Id = questionId,
                QuizId = quiz.Id,
                Type = q.Type,
                Content = q.Content,
                CorrectTrueFalse = q.CorrectTrueFalse,
                Options = q.Type == QuizQuestionType.SingleChoice
                    ? q.Options!.Select(o => new QuizAnswerOption
                    {
                        Id = Guid.NewGuid().ToString(),
                        QuestionId = questionId,
                        Text = o.Text,
                        IsCorrect = o.IsCorrect
                    }).ToList()
                    : []
            };
        }).ToList();
        
        dbContext.Quizzes.Add(quiz);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Quiz {QuizId} created by user {UserId} in group {GroupId}. TraceId: {TraceId}",
            quiz.Id, userId, groupId, traceId);
        
        return Results.Ok(ApiResponse<string>.Ok("Quiz created successfully.", quiz.Id, traceId));
    }
}
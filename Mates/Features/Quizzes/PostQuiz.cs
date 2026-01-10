using System.Diagnostics;
using System.Security.Claims;
using Mates.Features.Quizzes.Dtos;
using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities.Quizzes;
using Mates.Shared.Endpoints;
using Mates.Shared.Responses;
using Mates.Shared.Validators;
using Microsoft.AspNetCore.Mvc;
using Mates.Shared.Extensions;

namespace Mates.Features.Quizzes;

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
        
        var validationResult = QuizValidator.ValidateQuizRequest(request, traceId);
        if (validationResult != null)
            return Results.BadRequest(validationResult);
        
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
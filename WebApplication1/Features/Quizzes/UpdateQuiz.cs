using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Quizzes.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Groups;
using WebApplication1.Infrastructure.Data.Entities.Quizzes;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Extensions;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Quizzes;

public class UpdateQuiz : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/groups/{groupId}/quizzes/{quizId}", Handle)
            .WithName("UpdateQuiz")
            .WithDescription("Updates an existing quiz in a group")
            .WithTags("Quizzes")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        [FromRoute] string quizId,
        [FromBody] QuizRequestDto request,
        AppDbContext dbContext,
        ClaimsPrincipal currentUser,
        HttpContext httpContext,
        ILogger<UpdateQuiz> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        var userId = currentUser.GetUserId();

        logger.LogInformation("User {UserId} updating quiz {QuizId} in group {GroupId}. TraceId: {TraceId}",
            userId, quizId, groupId, traceId);

        var quiz = await dbContext.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(qt => qt.Options)
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
            logger.LogWarning("User {UserId} tried to update quiz {QuizId} without permission. TraceId: {TraceId}",
                userId, quizId, traceId);
            return Results.Forbid();
        }
        
        var hasAttempts = await dbContext.QuizAttempts
            .AnyAsync(a => a.QuizId == quiz.Id, cancellationToken);

        if (hasAttempts)
            return Results.BadRequest(ApiResponse<string>.Fail("Cannot update quiz with existing attempts.", traceId));

        var validationResult = QuizValidator.ValidateQuizRequest(request, traceId);
        if (validationResult != null)
            return Results.BadRequest(validationResult);

        quiz.Title = request.Title;
        quiz.Description = request.Description;

        dbContext.QuizAnswerOptions.RemoveRange(quiz.Questions.SelectMany(q => q.Options));
        dbContext.QuizQuestions.RemoveRange(quiz.Questions);

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

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Quiz {QuizId} updated by user {UserId} in group {GroupId}. TraceId: {TraceId}",
            quiz.Id, userId, groupId, traceId);

        return Results.Ok(ApiResponse<string>.Ok("Quiz updated successfully.", quiz.Id, traceId));
    }
}
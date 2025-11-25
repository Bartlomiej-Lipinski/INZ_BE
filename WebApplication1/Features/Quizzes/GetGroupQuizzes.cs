using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Features.Quizzes.Dtos;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Shared.Endpoints;
using WebApplication1.Shared.Responses;
using WebApplication1.Shared.Validators;

namespace WebApplication1.Features.Quizzes;

public class GetGroupQuizzes : IEndpoint
{
    public void RegisterEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/groups/{groupId}/quizzes", Handle)
            .WithName("GetGroupQuizzes")
            .WithDescription("Retrieves all quizzes for a group")
            .WithTags("Quizzes")
            .RequireAuthorization()
            .AddEndpointFilter<GroupMembershipFilter>();
    }

    public static async Task<IResult> Handle(
        [FromRoute] string groupId,
        AppDbContext dbContext,
        HttpContext httpContext,
        ILogger<GetGroupQuizzes> logger,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        logger.LogInformation("Fetching all quizzes for group {GroupId}. TraceId: {TraceId}", groupId, traceId);

        var quizzes = await dbContext.Quizzes
            .AsNoTracking()
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .Where(q => q.GroupId == groupId)
            .OrderBy(p => p.CreatedAt)
            .Select(q => new QuizResponseDto
            {
                Id = q.Id,
                Title = q.Title,
                Description = q.Description,
                CreatedAt = q.CreatedAt,
                Questions = q.Questions.Select(qt => new QuizQuestionResponseDto
                {
                    Id = qt.Id,
                    Type = qt.Type,
                    Content = qt.Content,
                    CorrectTrueFalse = null,
                    Options = qt.Options.Select(o => new QuizAnswerOptionResponseDto
                    {
                        Id = o.Id,
                        QuestionId = o.QuestionId,
                        Text = o.Text
                    }).ToList()
                }).ToList()
            })
            .ToListAsync(cancellationToken);
        
        if (quizzes.Count == 0)
        {
            logger.LogInformation("No quizzes found for group {GroupId}. TraceId: {TraceId}", groupId, traceId);
            return Results.Ok(ApiResponse<List<QuizResponseDto>>.Ok(quizzes, "No quizzes found for this group.",
                traceId));
        }

        logger.LogInformation("Retrieved {Count} quizzes for group {GroupId}. TraceId: {TraceId}",
            quizzes.Count, groupId, traceId);

        return Results.Ok(ApiResponse<List<QuizResponseDto>>.Ok(quizzes, "Quizzes retrieved successfully.", traceId));
    }
}
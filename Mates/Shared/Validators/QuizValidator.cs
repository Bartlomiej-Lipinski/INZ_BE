using Mates.Features.Quizzes.Dtos;
using Mates.Infrastructure.Data.Entities.Quizzes;
using Mates.Shared.Responses;

namespace Mates.Shared.Validators;

public static class QuizValidator
{
    public static ApiResponse<string>? ValidateQuizRequest(QuizRequestDto request, string traceId)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return ApiResponse<string>.Fail("Title is required.", traceId);

        if (request.Questions.Count == 0)
            return ApiResponse<string>.Fail("Quiz must contain at least one question.", traceId);

        foreach (var q in request.Questions)
        {
            if (string.IsNullOrWhiteSpace(q.Content))
                return ApiResponse<string>.Fail("Question content is required.", traceId);

            switch (q.Type)
            {
                case QuizQuestionType.SingleChoice when q.Options == null || q.Options.Count < 2:
                    return ApiResponse<string>.Fail("SingleChoice question must have at least 2 options.", traceId);
                case QuizQuestionType.SingleChoice:
                {
                    var correctCount = q.Options.Count(o => o.IsCorrect);
                    if (correctCount != 1)
                        return ApiResponse<string>.Fail("SingleChoice must have exactly 1 correct option.", traceId);
                    break;
                }
                case QuizQuestionType.TrueFalse:
                    if (q.CorrectTrueFalse == null)
                        return ApiResponse<string>.Fail("TrueFalse question must have correct answer defined.", traceId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return null;
    }
}
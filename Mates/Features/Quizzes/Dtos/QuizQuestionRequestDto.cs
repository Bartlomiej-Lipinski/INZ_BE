using Mates.Infrastructure.Data.Entities.Quizzes;

namespace Mates.Features.Quizzes.Dtos;

public record QuizQuestionRequestDto
{
    public QuizQuestionType Type { get; set; }
    public string Content { get; set; } = null!;
    public List<QuizAnswerOptionRequestDto>? Options { get; set; }
    public bool? CorrectTrueFalse { get; set; }
}
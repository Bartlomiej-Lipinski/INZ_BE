using WebApplication1.Infrastructure.Data.Entities.Quizzes;

namespace WebApplication1.Features.Quizzes.Dtos;

public record QuizQuestionResponseDto
{
    public string Id { get; set; } = null!;
    public QuizQuestionType Type { get; set; }
    public string Content { get; set; } = null!;
    public bool? CorrectTrueFalse { get; set; }
    public List<QuizAnswerOptionResponseDto> Options { get; set; } = [];
}
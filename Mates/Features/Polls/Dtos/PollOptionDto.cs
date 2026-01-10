namespace Mates.Features.Polls.Dtos;

public record PollOptionDto
{
    public string Id { get; set; }
    public string Text { get; set; } = null!;
    public List<string> VotedUsersIds { get; set; } = [];
}
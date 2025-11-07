namespace WebApplication1.Features.Groups.Dtos;

public record SecretSantaResponseDto
{
    public string Giver { get; set; } = null!;
    public string Receiver { get; set; } = null!;
}
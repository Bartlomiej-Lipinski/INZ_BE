namespace Mates.Features.Storage.Dtos;

public record DeleteProfilePhotoDto
{
    public string FileId { get; set; } = null!;
}
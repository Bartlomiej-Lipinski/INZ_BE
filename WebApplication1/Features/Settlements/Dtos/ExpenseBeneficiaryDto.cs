using WebApplication1.Features.Users.Dtos;

namespace WebApplication1.Features.Settlements.Dtos;

public record ExpenseBeneficiaryDto
{
    public string UserId { get; set; } = null!;
    public UserResponseDto? User { get; set; } = null!;
    public decimal? Share { get; set; }
}
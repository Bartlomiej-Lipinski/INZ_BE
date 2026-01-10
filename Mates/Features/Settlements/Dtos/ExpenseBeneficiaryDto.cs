using Mates.Features.Users.Dtos;

namespace Mates.Features.Settlements.Dtos;

public record ExpenseBeneficiaryDto
{
    public string UserId { get; set; } = null!;
    public UserResponseDto? User { get; set; } = null!;
    public decimal? Share { get; set; }
}
using WebApplication1.Features.Users.Dtos;

namespace WebApplication1.Features.Settlements.Dtos;

public record SettlementResponseDto
{
    public string Id { get; set; } = null!;
    public string GroupId { get; set; } = null!;
    public UserResponseDto ToUser { get; set; } = null!;
    public decimal Amount { get; set; }
}
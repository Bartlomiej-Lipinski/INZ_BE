namespace WebApplication1.Features.Settlements.Dtos;

public class SettlementResponseDto
{
    public string Id { get; set; } = null!;
    public string GroupId { get; set; } = null!;
    public string ToUserId { get; set; } = null!;
    public decimal Amount { get; set; }
}
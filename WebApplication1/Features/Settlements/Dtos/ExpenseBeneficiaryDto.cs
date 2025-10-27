namespace WebApplication1.Features.Settlements.Dtos;

public record ExpenseBeneficiaryDto
{
    public string UserId { get; set; } = null!;
    public decimal? Share { get; set; }
}
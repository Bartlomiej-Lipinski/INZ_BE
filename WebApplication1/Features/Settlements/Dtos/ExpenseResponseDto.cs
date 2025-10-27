namespace WebApplication1.Features.Settlements.Dtos;

public record ExpenseResponseDto
{
    public string Id { get; set; } = null!;
    public string GroupId { get; set; } = null!;
    public string PaidByUserId { get; set; } = null!;

    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? PhoneNumber { get; set; }
    public string? BankAccount { get; set; }
    public bool IsEvenSplit { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public List<ExpenseBeneficiaryDto> Beneficiaries { get; set; } = [];
}
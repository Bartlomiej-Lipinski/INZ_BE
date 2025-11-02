namespace WebApplication1.Features.Settlements.Dtos;

public record ExpenseRequestDto
{
    public string PaidByUserId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? PhoneNumber { get; set; }
    public string? BankAccount { get; set; }
    public bool IsEvenSplit { get; set; }
    public List<ExpenseBeneficiaryDto> Beneficiaries { get; set; } = [];
}
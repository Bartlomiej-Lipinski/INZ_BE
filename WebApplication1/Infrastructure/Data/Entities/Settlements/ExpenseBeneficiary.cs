namespace WebApplication1.Infrastructure.Data.Entities.Settlements;

public class ExpenseBeneficiary
{
    public string ExpenseId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    
    public decimal Share { get; set; }

    public Expense Expense { get; set; } = null!;
    public User User { get; set; } = null!;
}
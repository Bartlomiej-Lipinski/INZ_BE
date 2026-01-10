using Mates.Infrastructure.Data.Entities.Groups;

namespace Mates.Infrastructure.Data.Entities.Settlements;

public class Expense
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
    
    public Group Group { get; set; } = null!;
    public User PaidByUser { get; set; } = null!;
    public ICollection<ExpenseBeneficiary> Beneficiaries { get; set; } = new List<ExpenseBeneficiary>();
}
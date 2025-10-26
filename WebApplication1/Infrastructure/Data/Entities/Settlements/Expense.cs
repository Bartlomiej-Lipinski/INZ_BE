using WebApplication1.Infrastructure.Data.Entities.Groups;

namespace WebApplication1.Infrastructure.Data.Entities.Settlements;

public class Expense
{
    public string Id { get; set; } = null!;
    public string GroupId { get; set; } = null!;

    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public  Group Group { get; set; } = null!;
    public ICollection<ExpensePayer> Payers { get; set; } = new List<ExpensePayer>();
    public ICollection<ExpenseBeneficiary> Beneficiaries { get; set; } = new List<ExpenseBeneficiary>();
}
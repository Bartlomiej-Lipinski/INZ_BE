using Microsoft.EntityFrameworkCore;
using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Settlements;

namespace WebApplication1.Features.Settlements;

public static class SettlementCalculator
{
    public static async Task RecalculateSettlementsAsync(
    AppDbContext dbContext,
    string groupId,
    ILogger logger,
    CancellationToken cancellationToken)
    {
        logger.LogInformation("Recalculating settlements for group {GroupId}", groupId);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            "SELECT * FROM Expenses WHERE GroupId = {0} FOR UPDATE", groupId);
        await dbContext.Database.ExecuteSqlRawAsync(
            "SELECT * FROM Settlements WHERE GroupId = {0} FOR UPDATE", groupId);
        
        try
        {
            var expenses = await dbContext.Expenses
                .Include(e => e.Beneficiaries)
                .Where(e => e.GroupId == groupId)
                .ToListAsync(cancellationToken);

            if (expenses.Count == 0)
            {
                logger.LogInformation("No expenses found for group {GroupId}", groupId);
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            var balances = new Dictionary<string, decimal>();
            foreach (var expense in expenses)
            {
                balances.TryAdd(expense.PaidByUserId, 0);
                balances[expense.PaidByUserId] += expense.Amount;

                foreach (var b in expense.Beneficiaries)
                {
                    balances.TryAdd(b.UserId, 0);
                    balances[b.UserId] -= b.Share;
                }
            }

            var debtors = balances
                .Where(b => b.Value < 0)
                .Select(b => new SettlementParticipant { UserId = b.Key, Amount = -b.Value })
                .OrderBy(b => b.Amount)
                .ToList();

            var creditors = balances
                .Where(b => b.Value > 0)
                .Select(b => new SettlementParticipant { UserId = b.Key, Amount = b.Value })
                .OrderByDescending(b => b.Amount)
                .ToList();

            var newSettlements = new List<Settlement>();
            foreach (var debtor in debtors)
            {
                var amountToPay = debtor.Amount;

                foreach (var creditor in creditors.Where(c => c.Amount > 0))
                {
                    if (amountToPay <= 0)
                        break;

                    var amount = Math.Min(amountToPay, creditor.Amount);
                    if (amount <= 0) continue;

                    newSettlements.Add(new Settlement
                    {
                        GroupId = groupId,
                        FromUserId = debtor.UserId,
                        ToUserId = creditor.UserId,
                        Amount = Math.Round(amount, 2),
                    });

                    amountToPay -= amount;
                    creditor.Amount -= amount;
                }
            }

            var existingSettlements = await dbContext.Settlements
                .Where(s => s.GroupId == groupId)
                .ToListAsync(cancellationToken);

            foreach (var newSet in newSettlements)
            {
                var existing = existingSettlements.FirstOrDefault(s =>
                    s.FromUserId == newSet.FromUserId &&
                    s.ToUserId == newSet.ToUserId &&
                    s.GroupId == groupId);

                if (existing != null)
                {
                    if (existing.Amount == newSet.Amount) continue;
                    existing.Amount = newSet.Amount;
                    dbContext.Settlements.Update(existing);
                }
                else
                {
                    newSet.Id = Guid.NewGuid().ToString();
                    dbContext.Settlements.Add(newSet);
                }
            }

            var toRemove = existingSettlements
                .Where(es => es.Status != SettlementStatus.Paid)
                .Where(es => !newSettlements.Any(ns =>
                    ns.FromUserId == es.FromUserId &&
                    ns.ToUserId == es.ToUserId))
                .ToList();

            if (toRemove.Count != 0)
                dbContext.Settlements.RemoveRange(toRemove);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Settlements successfully recalculated and committed for group {GroupId}", groupId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while recalculating settlements for group {GroupId}", groupId);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
    
    private class SettlementParticipant
    {
        public string UserId { get; set; } = null!;
        public decimal Amount { get; set; }
    }
}
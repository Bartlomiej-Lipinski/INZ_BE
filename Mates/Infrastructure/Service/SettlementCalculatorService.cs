using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities.Settlements;
using Microsoft.EntityFrameworkCore;

namespace Mates.Infrastructure.Service;

public class SettlementCalculatorService : ISettlementCalculator
{
    public async Task RecalculateSettlementsForExpenseChangeAsync(
    Expense expense,
    AppDbContext dbContext,
    string groupId,
    bool isAddition,
    ILogger logger,
    CancellationToken cancellationToken)
    {
        var action = isAddition ? "addition" : "removal";
        logger.LogInformation("Recalculating settlements for expense {Action} in group {GroupId}", action, groupId);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var settlements = await dbContext.Settlements
            .Where(s => s.GroupId == groupId)
            .ToListAsync(cancellationToken);

        try
        {
            var balances = new Dictionary<string, decimal>();
            foreach (var settlement in settlements)
            {
                balances.TryAdd(settlement.ToUserId, 0);
                balances[settlement.ToUserId] += settlement.Amount;
                
                balances.TryAdd(settlement.FromUserId, 0);
                balances[settlement.FromUserId] -= settlement.Amount;
            }

            var sign = isAddition ? 1 : -1;
            balances.TryAdd(expense.PaidByUserId, 0);
            balances[expense.PaidByUserId] += sign * expense.Amount;

            foreach (var b in expense.Beneficiaries)
            {
                balances.TryAdd(b.UserId, 0);
                balances[b.UserId] -= sign * b.Share;
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
                        Id = Guid.NewGuid().ToString(),
                        GroupId = groupId,
                        FromUserId = debtor.UserId,
                        ToUserId = creditor.UserId,
                        Amount = Math.Round(amount, 2),
                    });

                    amountToPay -= amount;
                    creditor.Amount -= amount;
                }
            }

            foreach (var newSet in newSettlements)
            {
                var existing = settlements.FirstOrDefault(s =>
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
                    dbContext.Settlements.Add(newSet);
                }
            }

            var toRemove = settlements
                .Where(es => !newSettlements.Any(ns =>
                    ns.FromUserId == es.FromUserId &&
                    ns.ToUserId == es.ToUserId))
                .ToList();

            if (toRemove.Count != 0)
                dbContext.Settlements.RemoveRange(toRemove);

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Settlements successfully recalculated after expense {Action} for group {GroupId}", action, groupId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while recalculating settlements after expense {Action} for group {GroupId}", action, groupId);
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
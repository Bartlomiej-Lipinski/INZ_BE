using Mates.Infrastructure.Data.Context;
using Mates.Infrastructure.Data.Entities.Settlements;

namespace Mates.Infrastructure.Service;

public interface ISettlementCalculator
{
    Task RecalculateSettlementsForExpenseChangeAsync(
        Expense expense,
        AppDbContext dbContext,
        string groupId,
        bool isAddition,
        ILogger logger,
        CancellationToken cancellationToken);
}
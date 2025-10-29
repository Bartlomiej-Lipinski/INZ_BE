using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Settlements;

namespace WebApplication1.Infrastructure.Service;

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
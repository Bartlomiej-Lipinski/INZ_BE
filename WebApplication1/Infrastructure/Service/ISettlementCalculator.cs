using WebApplication1.Infrastructure.Data.Context;
using WebApplication1.Infrastructure.Data.Entities.Settlements;

namespace WebApplication1.Infrastructure.Service;

public interface ISettlementCalculator
{
    Task RecalculateSettlementsForExpenseAdditionAsync(
        Expense expense,
        AppDbContext dbContext,
        string groupId,
        ILogger logger,
        CancellationToken cancellationToken);
}
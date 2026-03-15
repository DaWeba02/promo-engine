using PromoEngine.Application.Dtos;
using PromoEngine.Domain.Models;

namespace PromoEngine.Application.Abstractions;

public interface IPromotionRepository
{
    Task<IReadOnlyList<Promotion>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Promotion>> GetActiveAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Promotion promotion, CancellationToken cancellationToken);
    Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public interface IQuoteAuditRepository
{
    Task AddAsync(QuoteAuditEntry entry, CancellationToken cancellationToken);
}

public interface IBudgetConsumptionRepository
{
    Task<IReadOnlyDictionary<Guid, BudgetConsumptionSnapshot>> GetSnapshotsAsync(
        IReadOnlyCollection<Guid> promotionIds,
        DateOnly consumptionDateUtc,
        string customerId,
        CancellationToken cancellationToken);

    Task AddRangeAsync(IEnumerable<BudgetConsumptionEntry> entries, CancellationToken cancellationToken);
}

public interface IPromotionRedemptionRepository
{
    Task AddRangeAsync(IEnumerable<PromotionRedemptionEntry> entries, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

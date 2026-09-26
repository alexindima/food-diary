namespace FoodDiary.Application.Contracts.Common.Abstractions.Persistence;

public interface IUnitOfWork {
    bool HasPendingChanges { get; }
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

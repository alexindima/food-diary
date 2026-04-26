namespace FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;

public interface IUnitOfWork {
    bool HasPendingChanges { get; }
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

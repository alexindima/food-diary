namespace FoodDiary.Application.Common.Abstractions.Persistence;

public interface IUnitOfWork {
    bool HasPendingChanges { get; }
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

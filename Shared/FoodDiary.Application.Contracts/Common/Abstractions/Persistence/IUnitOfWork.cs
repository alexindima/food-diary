namespace FoodDiary.Application.Contracts.Common.Abstractions.Persistence;

public interface IUnitOfWork {
    bool HasPendingChanges { get; }
    /// <summary>Discards unsaved tracking and domain events from a failed outer command.</summary>
    void DiscardChanges();
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

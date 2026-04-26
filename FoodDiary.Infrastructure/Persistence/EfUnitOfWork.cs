using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;

namespace FoodDiary.Infrastructure.Persistence;

internal sealed class EfUnitOfWork(FoodDiaryDbContext context) : IUnitOfWork {
    public bool HasPendingChanges => context.ChangeTracker.HasChanges();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) {
        return context.SaveChangesAsync(cancellationToken);
    }
}

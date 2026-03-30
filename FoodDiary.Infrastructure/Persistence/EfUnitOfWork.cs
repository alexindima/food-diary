using FoodDiary.Application.Common.Abstractions.Persistence;

namespace FoodDiary.Infrastructure.Persistence;

internal sealed class EfUnitOfWork(FoodDiaryDbContext context) : IUnitOfWork {
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) {
        return context.SaveChangesAsync(cancellationToken);
    }
}

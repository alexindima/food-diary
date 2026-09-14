using FoodDiary.Persistence.Abstractions;

namespace FoodDiary.Infrastructure.Persistence.Shared;

internal sealed class EfModuleScopeGuard(FoodDiaryDbContext context) : IModuleScopeGuard {
    public void EnsureCleanEntry() => SharedTransactionBoundary.EnsureCleanEntry(context);
}

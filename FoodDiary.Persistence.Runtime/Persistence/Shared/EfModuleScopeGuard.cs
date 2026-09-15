using FoodDiary.Persistence.Abstractions;

namespace FoodDiary.Persistence.Runtime.Persistence.Shared;

internal sealed class EfModuleScopeGuard(SharedPersistenceDbContext context) : IModuleScopeGuard {
    public void EnsureCleanEntry() => SharedTransactionBoundary.EnsureCleanEntry(context);
}

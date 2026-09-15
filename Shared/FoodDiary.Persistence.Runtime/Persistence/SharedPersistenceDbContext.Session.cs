using FoodDiary.Persistence.Runtime.Persistence.Shared;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FoodDiary.Persistence.Runtime.Persistence;

public abstract partial class SharedPersistenceDbContext : IModuleContextFactory, IModuleChangeTrackerSource, IModuleScopeGuard {
    void IModuleScopeGuard.EnsureCleanEntry() => Shared.SharedTransactionBoundary.EnsureCleanEntry(this);

    internal PersistenceSession Session { get; private set; } = null!;

    internal void AttachSession(PersistenceSession session) => Session = session;

    internal bool IsCoordinatingModuleSave { get => Session.IsSaving; set => Session.IsSaving = value; }
    internal IReadOnlyList<DbContext> ModuleContexts => Session.Contexts;

    IReadOnlyList<EntityEntry> IModuleChangeTrackerSource.GetModuleEntries<TContext>() => Session.GetModuleEntries<TContext>();

    public TContext CreateModuleContext<TContext>(Func<DbContextOptions<TContext>, TContext> factory, int saveOrder = 100)
        where TContext : DbContext => Session.CreateModuleContext(factory, saveOrder);

    internal int GetSaveOrder(DbContext participant) => Session.GetSaveOrder(participant);

    public override int SaveChanges(bool acceptAllChangesOnSuccess) {
        EnsureModuleSaveIsCoordinated();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) {
        EnsureModuleSaveIsCoordinated();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    internal void EnsureModuleSaveIsCoordinated() {
        if (!IsCoordinatingModuleSave && Session.HasOtherChanges(this)) {
            throw new InvalidOperationException("Module changes must be saved through IUnitOfWork so all contexts commit atomically.");
        }
    }
}

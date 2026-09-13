using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FoodDiary.Infrastructure.Persistence;

public sealed partial class FoodDiaryDbContext {
    private readonly List<DbContext> _moduleContexts = [];

    internal bool IsCoordinatingModuleSave { get; set; }
    internal IReadOnlyList<DbContext> ModuleContexts => _moduleContexts;

    public TContext CreateModuleContext<TContext>(Func<DbContextOptions<TContext>, TContext> factory)
        where TContext : DbContext {
        ArgumentNullException.ThrowIfNull(factory);
        var builder = new DbContextOptionsBuilder<TContext>();
        foreach (IDbContextOptionsExtension extension in this.GetService<IDbContextOptions>().Extensions
                     .Where(extension => extension.Info.IsDatabaseProvider)) {
            ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(extension);
        }
        if (Database.IsRelational()) {
            builder.UseNpgsql(Database.GetDbConnection());
        }
        IEnumerable<IInterceptor>? interceptors = this.GetService<IDbContextOptions>()
            .FindExtension<CoreOptionsExtension>()?.Interceptors;
        if (interceptors is not null) {
            builder.AddInterceptors(interceptors.OfType<DbCommandInterceptor>());
        }
        TContext module = factory(builder.Options);
        _moduleContexts.Add(module);
        return module;
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess) {
        EnsureModuleSaveIsCoordinated();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) {
        EnsureModuleSaveIsCoordinated();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    internal void EnsureModuleSaveIsCoordinated() {
        if (!IsCoordinatingModuleSave && _moduleContexts.Any(module => module.ChangeTracker.HasChanges())) {
            throw new InvalidOperationException("Module changes must be saved through IUnitOfWork so all contexts commit atomically.");
        }
    }
}

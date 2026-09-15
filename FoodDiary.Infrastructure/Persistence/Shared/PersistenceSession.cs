using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Data.Common;

namespace FoodDiary.Infrastructure.Persistence.Shared;

/// <summary>Tracks the contexts participating in one scoped unit of work.</summary>
internal sealed class PersistenceSession(DbContext rootContext) : IModuleContextFactory, IModuleChangeTrackerSource {
    private readonly List<DbContext> _contexts = [];
    private readonly Dictionary<DbContext, int> _saveOrders = [];

    internal DbContext RootContext { get; } = rootContext;
    internal IReadOnlyList<DbContext> Contexts => _contexts;
    internal bool IsSaving { get; set; }
    internal DbTransaction? ActiveTransaction { get; private set; }

    public IReadOnlyList<EntityEntry> GetModuleEntries<TContext>() where TContext : DbContext =>
        [.. _contexts.OfType<TContext>().SelectMany(context => context.ChangeTracker.Entries())];

    public TContext CreateModuleContext<TContext>(Func<DbContextOptions<TContext>, TContext> factory, int saveOrder = 100)
        where TContext : DbContext {
        ArgumentNullException.ThrowIfNull(factory);
        var builder = new DbContextOptionsBuilder<TContext>();
        foreach (IDbContextOptionsExtension extension in RootContext.GetService<IDbContextOptions>().Extensions
                     .Where(extension => extension.Info.IsDatabaseProvider)) {
            ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(extension);
        }
        if (RootContext.Database.IsRelational()) {
            builder.UseNpgsql(RootContext.Database.GetDbConnection());
        }
        IEnumerable<IInterceptor>? interceptors = RootContext.GetService<IDbContextOptions>()
            .FindExtension<CoreOptionsExtension>()?.Interceptors;
        if (interceptors is not null) {
            builder.AddInterceptors(interceptors.OfType<DbCommandInterceptor>());
        }
        TContext context = factory(builder.Options);
        if (ActiveTransaction is { } transaction) {
            context.Database.UseTransaction(transaction);
        }
        if (context is SharedPersistenceDbContext shared) {
            shared.AttachSession(this);
        }
        _contexts.Add(context);
        _saveOrders.Add(context, saveOrder);
        return context;
    }

    internal int GetSaveOrder(DbContext context) => ReferenceEquals(context, RootContext) ? 0 : _saveOrders[context];

    internal bool HasOtherChanges(DbContext caller) =>
        (!ReferenceEquals(caller, RootContext) && RootContext.ChangeTracker.HasChanges()) ||
        _contexts.Any(context => !ReferenceEquals(caller, context) && context.ChangeTracker.HasChanges());

    internal async Task<T> WithTransactionAsync<T>(DbTransaction transaction, Func<Task<T>> operation, CancellationToken cancellationToken) {
        ActiveTransaction = transaction;
        try {
            foreach (DbContext context in _contexts) {
                await context.Database.UseTransactionAsync(transaction, cancellationToken).ConfigureAwait(false);
            }
            return await operation().ConfigureAwait(false);
        } finally {
            ActiveTransaction = null;
            foreach (DbContext context in _contexts) {
                await context.Database.UseTransactionAsync(transaction: null, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}

using System.Data.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Infrastructure.Persistence;
using FoodDiary.Persistence.Runtime.Persistence.Shared;
using FoodDiary.Persistence.Runtime.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class ModuleSaveFailureIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task FailedSavepointRollbackPreservesOriginalSaveFailureAsync() {
        await using FoodDiaryDbContext setup = await databaseFixture.CreateDbContextAsync();
        var failure = new InvalidOperationException("Original save failure");
        var rollback = new RejectSavepointRollback();
        await using var context = new FoodDiaryDbContext(new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(setup.Database.GetConnectionString()).AddInterceptors(new RejectSave(failure), rollback).Options);
        UsersDbContext owner = context.CreateModuleContext<UsersDbContext>(options => new UsersDbContext(options));
        owner.Users.Add(User.Create("save-failed@example.com", "hash"));
        var logger = new SaveLogger();
        var unitOfWork = new EfUnitOfWork(context, Substitute.For<IDomainEventPublisher>(), logger);
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        Exception observed = await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.SaveChangesAsync());

        Assert.Same(failure, observed);
        Assert.True(rollback.Attempted);
        Assert.Null(owner.Database.CurrentTransaction);
        Assert.False(context.IsCoordinatingModuleSave);
        Assert.True(unitOfWork.HasPendingChanges);
        Assert.Equal("Rollback failed", logger.Warning?.Message);
        await transaction.RollbackAsync();
        Assert.Empty(await setup.Users.ToListAsync());
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransactionExceptionTranslationPreservesSelectedExceptionAsync(bool translate) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var coordinator = new EfModuleTransactionCoordinator(context, Substitute.For<IUnitOfWork>());
        var original = new InvalidOperationException("Original");
        Exception expected = translate ? new ApplicationException("Translated", original) : original;
        Exception? actual = await Record.ExceptionAsync(() => coordinator.ExecuteAsync(
            (_, _) => Task.FromException(original), _ => expected));
        Assert.Same(expected, actual);
        Assert.Null(context.Database.CurrentTransaction);
    }

    [ExcludeFromCodeCoverage]
    private sealed class SaveLogger : ILogger<EfUnitOfWork> {
        public Exception? Warning { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
            if (logLevel == LogLevel.Warning) { Warning = exception; }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RejectSave(Exception failure) : SaveChangesInterceptor {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default) => throw failure;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RejectSavepointRollback : DbTransactionInterceptor {
        public bool Attempted { get; private set; }
        public override ValueTask<InterceptionResult> RollingBackToSavepointAsync(DbTransaction transaction,
            TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) {
            Attempted = true;
            throw new InvalidOperationException("Rollback failed");
        }
    }
}

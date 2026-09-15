using FoodDiary.Modules.Hydration.Domain.Entities.Tracking;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.MealPlanning.Domain.Entities.Shopping;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using FoodDiary.Modules.Hydration.Infrastructure.Persistence;
using FoodDiary.Modules.MealPlanning.Infrastructure.Persistence;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class EfUnitOfWorkTests {
    [Fact]
    public async Task ModuleTracker_IsSavedByUnitOfWorkAndVisibleThroughLegacyReadModelAsync() {
        await using FoodDiaryDbContext context = CreateContext();
        await using HydrationDbContext module = context.CreateModuleContext<HydrationDbContext>(static options => new HydrationDbContext(options));
        var entry = HydrationEntry.Create(UserId.New(), DateTime.UtcNow, 250);
        module.HydrationEntries.Add(entry);
        var unitOfWork = new EfUnitOfWork(context, Substitute.For<IDomainEventPublisher>(), NullLogger<EfUnitOfWork>.Instance);

        Assert.True(unitOfWork.HasPendingChanges);
        Assert.Throws<InvalidOperationException>(() => FoodDiary.Persistence.Runtime.Persistence.Shared.SharedTransactionBoundary.EnsureCleanEntry(context));
        await unitOfWork.SaveChangesAsync();

        Assert.False(unitOfWork.HasPendingChanges);
        Assert.True(await context.HydrationEntries.AsNoTracking().AnyAsync(item => item.Id == entry.Id));
    }

    [Fact]
    public async Task TransactionAttempt_ResetIncludesModuleTrackerAsync() {
        await using FoodDiaryDbContext context = CreateContext();
        await using HydrationDbContext module = context.CreateModuleContext<HydrationDbContext>(static options => new HydrationDbContext(options));
        module.HydrationEntries.Add(HydrationEntry.Create(UserId.New(), DateTime.UtcNow, 250));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            FoodDiary.Persistence.Runtime.Persistence.Shared.SharedTransactionBoundary.ExecuteAttemptAsync<int>(context, postCommitActionQueue: null,
                () => Task.FromException<int>(new InvalidOperationException("Failed attempt."))));

        Assert.Empty(module.ChangeTracker.Entries());
    }

    [Fact]
    public async Task TransactionBoundary_RejectsPendingPostCommitActionsWithoutDiscardingThem() {
        await using FoodDiaryDbContext context = CreateContext();
        FoodDiary.Application.Abstractions.Common.Abstractions.Persistence.IPostCommitActionQueue queue = Substitute.For<FoodDiary.Application.Abstractions.Common.Abstractions.Persistence.IPostCommitActionQueue>();
        queue.HasActions.Returns(returnThis: true);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            FoodDiary.Persistence.Runtime.Persistence.Shared.SharedTransactionBoundary.EnsureCleanEntry(context, queue));

        Assert.Contains("pending post-commit actions", error.Message, StringComparison.Ordinal);
        queue.DidNotReceive().Discard();
        Assert.Empty(context.ChangeTracker.Entries());
    }
    [Fact]
    public async Task HasPendingChanges_ReflectsChangeTrackerAndSaveChangesPersistsChanges() {
        await using FoodDiaryDbContext context = CreateContext();
        IDomainEventPublisher publisher = Substitute.For<IDomainEventPublisher>();
        publisher.PublishAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var unitOfWork = new EfUnitOfWork(context, publisher, NullLogger<EfUnitOfWork>.Instance);
        var list = ShoppingList.Create(UserId.New(), "Weekly");

        context.ShoppingLists.Add(list);

        Assert.True(unitOfWork.HasPendingChanges);

        await unitOfWork.SaveChangesAsync();

        Assert.False(unitOfWork.HasPendingChanges);
        Assert.NotNull(await context.ShoppingLists.FindAsync(list.Id));
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsStateAddedByDomainEventHandlerInSameSave() {
        await using FoodDiaryDbContext context = CreateContext();
        var source = ShoppingList.Create(UserId.New(), "Before");
        context.ShoppingLists.Add(source);
        source.UpdateName("After");
        IDomainEventPublisher publisher = Substitute.For<IDomainEventPublisher>();
        publisher
            .PublishAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(_ => {
                context.ShoppingLists.Add(ShoppingList.Create(UserId.New(), "From event handler"));
                return Task.CompletedTask;
            });
        var unitOfWork = new EfUnitOfWork(context, publisher, NullLogger<EfUnitOfWork>.Instance);

        await unitOfWork.SaveChangesAsync();

        Assert.Equal(2, await context.ShoppingLists.CountAsync());
        await publisher.Received(1).PublishAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveChangesAsync_WhenPersistenceFails_RetainsEventsAndDoesNotRepublishOnRetry() {
        var failureInterceptor = new FailFirstSaveInterceptor();
        await using FoodDiaryDbContext context = CreateContext(failureInterceptor);
        var source = ShoppingList.Create(UserId.New(), "Before");
        context.ShoppingLists.Add(source);
        source.UpdateName("After");
        IDomainEventPublisher publisher = Substitute.For<IDomainEventPublisher>();
        publisher.PublishAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var unitOfWork = new EfUnitOfWork(context, publisher, NullLogger<EfUnitOfWork>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.SaveChangesAsync());
        Assert.NotEmpty(source.DomainEvents);

        await unitOfWork.SaveChangesAsync();

        Assert.Empty(source.DomainEvents);
        await publisher.Received(1).PublishAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveChangesAsync_WhenEventHandlerRegistersModule_CompletesSaveAsync() {
        await using FoodDiaryDbContext context = CreateContext();
        await using MealPlanningDbContext module = context.CreateModuleContext<MealPlanningDbContext>(static options => new MealPlanningDbContext(options));
        var source = ShoppingList.Create(UserId.New(), "Before");
        module.ShoppingLists.Add(source);
        source.UpdateName("After");
        HydrationDbContext? registeredModule = null;
        IDomainEventPublisher publisher = Substitute.For<IDomainEventPublisher>();
        publisher.PublishAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>()).Returns(_ => {
            registeredModule = context.CreateModuleContext<HydrationDbContext>(static options => new HydrationDbContext(options));
            return Task.CompletedTask;
        });
        var unitOfWork = new EfUnitOfWork(context, publisher, NullLogger<EfUnitOfWork>.Instance);

        try {
            await unitOfWork.SaveChangesAsync();

            Assert.NotNull(registeredModule);
            Assert.False(unitOfWork.HasPendingChanges);
            Assert.Empty(source.DomainEvents);
            Assert.Equal("After", (await context.ShoppingLists.AsNoTracking().SingleAsync()).Name);
            await publisher.Received(1).PublishAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
        } finally {
            if (registeredModule is not null) {
                await registeredModule.DisposeAsync();
            }
        }
    }

    private static FoodDiaryDbContext CreateContext(SaveChangesInterceptor? interceptor = null) {
        DbContextOptionsBuilder<FoodDiaryDbContext> builder = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"), new InMemoryDatabaseRoot());
        if (interceptor is not null) {
            builder.AddInterceptors(interceptor);
        }

        return new FoodDiaryDbContext(builder.Options);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FailFirstSaveInterceptor : SaveChangesInterceptor {
        private bool _failed;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) {
            if (!_failed) {
                _failed = true;
                throw new InvalidOperationException("Simulated transient persistence failure.");
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}

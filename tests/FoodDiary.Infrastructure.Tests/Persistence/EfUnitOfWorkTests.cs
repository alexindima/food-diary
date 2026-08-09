using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class EfUnitOfWorkTests {
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

    private static FoodDiaryDbContext CreateContext(SaveChangesInterceptor? interceptor = null) {
        DbContextOptionsBuilder<FoodDiaryDbContext> builder = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"));
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

using System.Reflection;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class DomainEventDispatchInterceptorTests {
    [Fact]
    public async Task DispatchDomainEventsAsync_PublishesAndClearsTrackedAggregateEvents() {
        await using FoodDiaryDbContext context = CreateContext();
        var list = ShoppingList.Create(UserId.New(), "Before");
        context.ShoppingLists.Add(list);
        list.UpdateName("After");
        IDomainEventPublisher publisher = Substitute.For<IDomainEventPublisher>();
        publisher.PublishAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var interceptor = new DomainEventDispatchInterceptor(
            publisher,
            NullLogger<DomainEventDispatchInterceptor>.Instance);

        await InvokeDispatchDomainEventsAsync(interceptor, context);

        await publisher.Received(1).PublishAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
        Assert.Empty(list.DomainEvents);
    }

    [Fact]
    public async Task SavingChangesAsync_WhenContextIsPresent_DispatchesDomainEventsThroughEfPipeline() {
        IDomainEventPublisher publisher = Substitute.For<IDomainEventPublisher>();
        publisher.PublishAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var interceptor = new DomainEventDispatchInterceptor(
            publisher,
            NullLogger<DomainEventDispatchInterceptor>.Instance);
        await using FoodDiaryDbContext context = CreateContext(interceptor);
        var list = ShoppingList.Create(UserId.New(), "Before");
        context.ShoppingLists.Add(list);
        list.UpdateName("After");

        await context.SaveChangesAsync();

        await publisher.Received(1).PublishAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
        Assert.Empty(list.DomainEvents);
    }

    [Fact]
    public async Task DispatchDomainEventsAsync_WhenNoEvents_DoesNotPublish() {
        await using FoodDiaryDbContext context = CreateContext();
        context.ShoppingLists.Add(ShoppingList.Create(UserId.New(), "List"));
        IDomainEventPublisher publisher = Substitute.For<IDomainEventPublisher>();
        var interceptor = new DomainEventDispatchInterceptor(
            publisher,
            NullLogger<DomainEventDispatchInterceptor>.Instance);

        await InvokeDispatchDomainEventsAsync(interceptor, context);

        await publisher.DidNotReceive().PublishAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
    }

    private static FoodDiaryDbContext CreateContext(SaveChangesInterceptor? interceptor = null) {
        DbContextOptionsBuilder<FoodDiaryDbContext> builder = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"));

        if (interceptor is not null) {
            builder.AddInterceptors(interceptor);
        }

        return new FoodDiaryDbContext(builder.Options);
    }

    private static Task InvokeDispatchDomainEventsAsync(
        DomainEventDispatchInterceptor interceptor,
        FoodDiaryDbContext context) {
        MethodInfo method = typeof(DomainEventDispatchInterceptor)
            .GetMethod("DispatchDomainEventsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Task)method.Invoke(interceptor, [context, CancellationToken.None])!;
    }

}

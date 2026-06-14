using System.Reflection;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Domain.Common;
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
        var publisher = new RecordingDomainEventPublisher();
        var interceptor = new DomainEventDispatchInterceptor(
            publisher,
            NullLogger<DomainEventDispatchInterceptor>.Instance);

        await InvokeDispatchDomainEventsAsync(interceptor, context);

        Assert.Single(publisher.PublishedEvents);
        Assert.Empty(list.DomainEvents);
    }

    [Fact]
    public async Task SavingChangesAsync_WhenContextIsPresent_DispatchesDomainEventsThroughEfPipeline() {
        var publisher = new RecordingDomainEventPublisher();
        var interceptor = new DomainEventDispatchInterceptor(
            publisher,
            NullLogger<DomainEventDispatchInterceptor>.Instance);
        await using FoodDiaryDbContext context = CreateContext(interceptor);
        var list = ShoppingList.Create(UserId.New(), "Before");
        context.ShoppingLists.Add(list);
        list.UpdateName("After");

        await context.SaveChangesAsync();

        Assert.Single(publisher.PublishedEvents);
        Assert.Empty(list.DomainEvents);
    }

    [Fact]
    public async Task DispatchDomainEventsAsync_WhenNoEvents_DoesNotPublish() {
        await using FoodDiaryDbContext context = CreateContext();
        context.ShoppingLists.Add(ShoppingList.Create(UserId.New(), "List"));
        var publisher = new RecordingDomainEventPublisher();
        var interceptor = new DomainEventDispatchInterceptor(
            publisher,
            NullLogger<DomainEventDispatchInterceptor>.Instance);

        await InvokeDispatchDomainEventsAsync(interceptor, context);

        Assert.Empty(publisher.PublishedEvents);
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

    [ExcludeFromCodeCoverage]
    private sealed class RecordingDomainEventPublisher : IDomainEventPublisher {
        private readonly List<IDomainEvent> publishedEvents = [];

        public IReadOnlyList<IDomainEvent> PublishedEvents => publishedEvents;

        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) {
            publishedEvents.Add(domainEvent);
            return Task.CompletedTask;
        }
    }
}

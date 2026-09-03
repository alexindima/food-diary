using FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Achievements;
using FoodDiary.Infrastructure.Persistence.Outbox;
using FoodDiary.Modules.Gamification.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class OutboxReplayStreamTests {
    [Fact]
    public async Task Adapter_PreservesProjectionTrackingFiltersAndBoundedList() {
        DateTime now = new(2026, 9, 3, 10, 0, 0, DateTimeKind.Utc);
        await using FoodDiaryDbContext context = new(new DbContextOptionsBuilder<FoodDiaryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddGamificationModule();
        services.AddGamificationModule();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IOutboxReplayStream stream = Assert.Single(provider.GetServices<IOutboxReplayStream>());
        Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IOutboxReplayStream)).Lifetime);
        Assert.Equal("achievement_evaluation", stream.Name);
        Assert.Equal(3, stream.Order);
        Assert.Null(stream.ReplayRejectionReason);
        var message = AchievementEvaluationOutboxMessage.Create(UserId.New(), now.AddMinutes(-3));
        message.MarkDeadLettered("failure", now);
        context.Add(message);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        OutboxDeadLetterMessageModel listed = Assert.Single(await stream.ListAsync(1));
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Multiple(
            () => Assert.Equal(message.Id, listed.MessageId),
            () => Assert.Equal(message.UserId.Value.ToString(), listed.Summary),
            () => Assert.Equal("failure", listed.LastError),
            () => Assert.Equal(1, listed.AttemptCount));
        OutboxReplayEntry? found = await stream.FindAsync(message.Id, forUpdate: false);
        Assert.NotNull(found);
        Assert.Same(context.ChangeTracker.Entries<AchievementEvaluationOutboxMessage>().Single().Entity, found.Message);
        Assert.Equal(EntityState.Unchanged, context.Entry(found.Message).State);
        Assert.Equal(listed.Summary, found.Summary);
        Assert.Equal(listed.LastError, found.LastError);

        var active = AchievementEvaluationOutboxMessage.Create(UserId.New(), now.AddMinutes(-3));
        var processed = AchievementEvaluationOutboxMessage.Create(UserId.New(), now.AddMinutes(-3));
        processed.MarkDeadLettered("processed", now.AddMinutes(1));
        processed.MarkProcessed(now.AddMinutes(2));
        context.AddRange(active, processed);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        Assert.Equal(message.Id, Assert.Single(await stream.ListAsync(1)).MessageId);
        Assert.Empty(context.ChangeTracker.Entries());
    }
}

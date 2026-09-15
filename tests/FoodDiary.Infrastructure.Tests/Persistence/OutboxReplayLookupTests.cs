using FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Gamification.PersistenceModel.Achievements;
using FoodDiary.Infrastructure.Persistence.Email;
using FoodDiary.Modules.Images.PersistenceModel.Images;
using FoodDiary.Infrastructure.Persistence.Notifications;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class OutboxReplayLookupTests {
    private static readonly DateTime Now = new(2026, 9, 3, 10, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("email")]
    [InlineData("image_object_deletion")]
    [InlineData("notification_web_push")]
    [InlineData("achievement_evaluation")]
    public async Task Preview_WithMissingId_DoesNotReturnAnotherMessage(string streamName) {
        await using FoodDiaryDbContext context = CreateContext();
        using var scope = new OutboxReplayTestScope(context, TimeProvider.System);
        var missingId = Guid.NewGuid();
        Assert.Null(await scope.Service.GetDeadLetterAsync(streamName, missingId));
        IOutboxMessage other = CreateMessage(streamName, "other");
        other.MarkDeadLettered("failure-other", Now);
        context.Add(other);
        await context.SaveChangesAsync();
        scope.ClearTracking();

        Assert.Null(await scope.Service.GetDeadLetterAsync(streamName, missingId));
        Assert.Empty(scope.Entries);
        Assert.Empty(await context.OutboxReplayAudits.ToListAsync());
    }

    [Theory]
    [InlineData("email")]
    [InlineData("image_object_deletion")]
    [InlineData("notification_web_push")]
    [InlineData("achievement_evaluation")]
    public async Task Preview_WithMultipleRows_ReturnsRequestedDeadLetterAndIgnoresActiveRow(string streamName) {
        await using FoodDiaryDbContext context = CreateContext();
        using var scope = new OutboxReplayTestScope(context, TimeProvider.System);
        IOutboxMessage other = CreateMessage(streamName, "other");
        IOutboxMessage requested = CreateMessage(streamName, "requested");
        IOutboxMessage active = CreateMessage(streamName, "active");
        other.MarkDeadLettered("failure-other", Now);
        requested.MarkDeadLettered("failure-requested", Now);
        context.AddRange(other, requested, active);
        await context.SaveChangesAsync();
        scope.ClearTracking();

        OutboxDeadLetterMessageModel? preview = await scope.Service.GetDeadLetterAsync(streamName, requested.Id);
        Assert.NotNull(preview);
        Assert.Multiple(
            () => Assert.Equal(requested.Id, preview.MessageId),
            () => Assert.Equal("failure-requested", preview.LastError),
            () => Assert.Equal(1, preview.AttemptCount),
            () => Assert.Equal(Now, preview.DeadLetteredOnUtc));
        EntityEntry tracked = Assert.Single(scope.Entries);
        Assert.Equal(requested.Id, Assert.IsAssignableFrom<IOutboxMessage>(tracked.Entity).Id);
        Assert.Equal(EntityState.Unchanged, tracked.State);
        scope.ClearTracking();
        Assert.Null(await scope.Service.GetDeadLetterAsync(streamName, active.Id));
        scope.ClearTracking();
        Assert.Null(await scope.Service.GetDeadLetterAsync(streamName, Guid.NewGuid()));
        Assert.Empty(scope.Entries);
        Assert.Empty(await context.OutboxReplayAudits.ToListAsync());
    }

    [Theory]
    [InlineData("image_object_deletion")]
    [InlineData("notification_web_push")]
    [InlineData("achievement_evaluation")]
    public async Task Replay_WithMissingId_DoesNotResetAnotherMessageOrCreateAudit(string streamName) {
        await using FoodDiaryDbContext context = CreateContext();
        using var scope = new OutboxReplayTestScope(context, TimeProvider.System);
        IOutboxMessage other = CreateMessage(streamName, "other");
        other.MarkDeadLettered("failure-other", Now);
        context.Add(other);
        await context.SaveChangesAsync();
        scope.ClearTracking();

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            scope.Service.ReplayAsync(streamName, Guid.NewGuid(), "operator", "reason", 1));
        Assert.Equal("Outbox message was not found.", error.Message);
        OutboxDeadLetterMessageModel remaining = Assert.Single(await scope.Service.ListDeadLettersAsync(streamName, 10));
        Assert.Multiple(
            () => Assert.Equal(other.Id, remaining.MessageId),
            () => Assert.Equal("failure-other", remaining.LastError),
            () => Assert.Equal(1, remaining.AttemptCount));
        Assert.Empty(scope.Entries);
        Assert.Empty(await context.OutboxReplayAudits.ToListAsync());
    }

    private static FoodDiaryDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<FoodDiaryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), new Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot()).Options);

    private static IOutboxMessage CreateMessage(string streamName, string marker) => streamName switch {
        "email" => EmailOutboxMessage.Create(new EmailMessage("from@example.com", "Sender", ["to@example.com"], marker, "body", TextBody: null), Now),
        "image_object_deletion" => ImageObjectDeletionOutboxMessage.Create($"users/test/{marker}.webp", Now),
        "notification_web_push" => NotificationWebPushOutboxMessage.Create(NotificationId.New(), Now),
        "achievement_evaluation" => AchievementEvaluationOutboxMessage.Create(UserId.New(), Now),
        _ => throw new ArgumentOutOfRangeException(nameof(streamName)),
    };
}

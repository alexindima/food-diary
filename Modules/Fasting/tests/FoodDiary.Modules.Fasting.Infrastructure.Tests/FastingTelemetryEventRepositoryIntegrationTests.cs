using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Fasting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class FastingTelemetryEventRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task DeleteOlderThanAsync_DeletesOnlyExpiredEventsWithinBatch() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var cutoffUtc = new DateTime(2030, 8, 19, 12, 0, 0, DateTimeKind.Utc);
        var repository = new FastingTelemetryEventRepository(context);

        await repository.AddAsync(CreateRecord("fast_started", cutoffUtc.AddDays(-3), "session-oldest"));
        await repository.AddAsync(CreateRecord("fast_completed", cutoffUtc.AddDays(-2), "session-older"));
        await repository.AddAsync(CreateRecord("fast_started", cutoffUtc.AddMinutes(1), "session-fresh"));
        await context.SaveChangesAsync();

        int noneDeletedCount = await repository.DeleteOlderThanAsync(cutoffUtc.AddDays(-10), batchSize: 10);
        int firstDeletedCount = await repository.DeleteOlderThanAsync(cutoffUtc, batchSize: 1);
        int secondDeletedCount = await repository.DeleteOlderThanAsync(cutoffUtc, batchSize: 10);

        Assert.Equal(0, noneDeletedCount);
        Assert.Equal(1, firstDeletedCount);
        Assert.Equal(1, secondDeletedCount);
        FastingTelemetryEventRecord remaining = Assert.Single(
            await repository.GetRangeAsync(cutoffUtc.AddDays(-10), cutoffUtc.AddDays(1)));
        int remainingCount = await context.FastingTelemetryEvents.AsNoTracking().CountAsync();
        Assert.Multiple(
            () => Assert.Equal("fast_started", remaining.Name),
            () => Assert.Equal("session-fresh", remaining.SessionId),
            () => Assert.Equal(1, remainingCount));
    }

    private static FastingTelemetryEventRecord CreateRecord(
        string name,
        DateTime occurredAtUtc,
        string sessionId) =>
        new(
            name,
            occurredAtUtc,
            sessionId,
            Protocol: null,
            PlanType: null,
            Status: null,
            OccurrenceKind: null,
            ReminderPresetId: null,
            ReminderSource: null,
            FirstReminderHours: null,
            FollowUpReminderHours: null,
            PlannedDurationHours: null,
            ActualDurationHours: null,
            HungerLevel: null,
            EnergyLevel: null,
            MoodLevel: null,
            SymptomsCount: null,
            HadNotes: null);
}

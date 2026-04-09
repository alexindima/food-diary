using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

public class FastingOccurrenceInvariantTests {
    [Fact]
    public void Create_WithTargetHours_Succeeds() {
        var startedAtUtc = DateTime.UtcNow;

        var occurrence = FastingOccurrence.Create(
            FastingPlanId.New(),
            UserId.New(),
            FastingOccurrenceKind.FastDay,
            startedAtUtc,
            sequenceNumber: 1,
            targetHours: 24,
            notes: "  Long fast  ");

        Assert.Equal(FastingOccurrenceStatus.Active, occurrence.Status);
        Assert.Equal(24, occurrence.InitialTargetHours);
        Assert.Equal(0, occurrence.AddedTargetHours);
        Assert.Equal(24, occurrence.TargetHours);
        Assert.Equal("Long fast", occurrence.Notes);
        Assert.Equal(startedAtUtc, occurrence.StartedAtUtc);
    }

    [Fact]
    public void Extend_IncreasesTargetHours() {
        var occurrence = FastingOccurrence.Create(
            FastingPlanId.New(),
            UserId.New(),
            FastingOccurrenceKind.FastDay,
            DateTime.UtcNow,
            sequenceNumber: 1,
            targetHours: 24);

        occurrence.Extend(12);

        Assert.Equal(24, occurrence.InitialTargetHours);
        Assert.Equal(12, occurrence.AddedTargetHours);
        Assert.Equal(36, occurrence.TargetHours);
    }

    [Fact]
    public void Postpone_ChangesStatusAndSchedule() {
        var scheduledForUtc = DateTime.UtcNow.AddDays(1);
        var occurrence = FastingOccurrence.Schedule(
            FastingPlanId.New(),
            UserId.New(),
            FastingOccurrenceKind.FastDay,
            scheduledForUtc,
            sequenceNumber: 2,
            targetHours: 24);

        var postponedAtUtc = scheduledForUtc;
        var nextScheduledForUtc = scheduledForUtc.AddDays(1);

        occurrence.Postpone(postponedAtUtc, nextScheduledForUtc);

        Assert.Equal(FastingOccurrenceStatus.Postponed, occurrence.Status);
        Assert.Equal(postponedAtUtc, occurrence.EndedAtUtc);
        Assert.Equal(nextScheduledForUtc, occurrence.ScheduledForUtc);
    }

    [Fact]
    public void Complete_AfterInterrupted_Throws() {
        var occurrence = FastingOccurrence.Create(
            FastingPlanId.New(),
            UserId.New(),
            FastingOccurrenceKind.FastingWindow,
            DateTime.UtcNow,
            sequenceNumber: 1,
            targetHours: 16);

        occurrence.Interrupt(DateTime.UtcNow.AddHours(2));

        Assert.Throws<InvalidOperationException>(() => occurrence.Complete(DateTime.UtcNow.AddHours(16)));
    }
}

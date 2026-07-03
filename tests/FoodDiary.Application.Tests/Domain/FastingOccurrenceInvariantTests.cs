using System.Globalization;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

[ExcludeFromCodeCoverage]
public class FastingOccurrenceInvariantTests {
    [Fact]
    public void Create_WithTargetHours_Succeeds() {
        DateTime startedAtUtc = DateTime.UtcNow;

        var occurrence = FastingOccurrence.Create(
            FastingPlanId.New(),
            UserId.New(),
            FastingOccurrenceKind.FastDay,
            startedAtUtc,
            sequenceNumber: 1,
            targetHours: 24,
            notes: "  Long fast  ");

        Assert.Multiple(
            () => Assert.Equal(FastingOccurrenceStatus.Active, occurrence.Status),
            () => Assert.Equal(24, occurrence.InitialTargetHours),
            () => Assert.Equal(0, occurrence.AddedTargetHours),
            () => Assert.Equal(24, occurrence.TargetHours),
            () => Assert.Equal("Long fast", occurrence.Notes),
            () => Assert.Equal(startedAtUtc, occurrence.StartedAtUtc));
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

        Assert.Multiple(
            () => Assert.Equal(24, occurrence.InitialTargetHours),
            () => Assert.Equal(12, occurrence.AddedTargetHours),
            () => Assert.Equal(36, occurrence.TargetHours));
    }

    [Fact]
    public void Reduce_DecreasesTargetHours() {
        var occurrence = FastingOccurrence.Create(
            FastingPlanId.New(),
            UserId.New(),
            FastingOccurrenceKind.FastDay,
            DateTime.UtcNow,
            sequenceNumber: 1,
            targetHours: 36);

        occurrence.Reduce(8);

        Assert.Multiple(
            () => Assert.Equal(36, occurrence.InitialTargetHours),
            () => Assert.Equal(-8, occurrence.AddedTargetHours),
            () => Assert.Equal(28, occurrence.TargetHours));
    }

    [Fact]
    public void Postpone_ChangesStatusAndSchedule() {
        DateTime scheduledForUtc = DateTime.UtcNow.AddDays(1);
        var occurrence = FastingOccurrence.Schedule(
            FastingPlanId.New(),
            UserId.New(),
            FastingOccurrenceKind.FastDay,
            scheduledForUtc,
            sequenceNumber: 2,
            targetHours: 24);

        DateTime postponedAtUtc = scheduledForUtc;
        DateTime nextScheduledForUtc = scheduledForUtc.AddDays(1);

        occurrence.Postpone(postponedAtUtc, nextScheduledForUtc);

        Assert.Multiple(
            () => Assert.Equal(FastingOccurrenceStatus.Postponed, occurrence.Status),
            () => Assert.Equal(postponedAtUtc, occurrence.EndedAtUtc),
            () => Assert.Equal(nextScheduledForUtc, occurrence.ScheduledForUtc));
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

    [Fact]
    public void Create_WithEmptyIds_Throws() {
        Assert.Throws<ArgumentException>(() =>
            FastingOccurrence.Create(FastingPlanId.Empty, UserId.New(), FastingOccurrenceKind.FastDay, DateTime.UtcNow, 1));
        Assert.Throws<ArgumentException>(() =>
            FastingOccurrence.Create(FastingPlanId.New(), UserId.Empty, FastingOccurrenceKind.FastDay, DateTime.UtcNow, 1));
    }

    [Fact]
    public void Schedule_WithEmptyIds_Throws() {
        Assert.Throws<ArgumentException>(() =>
            FastingOccurrence.Schedule(FastingPlanId.Empty, UserId.New(), FastingOccurrenceKind.FastDay, DateTime.UtcNow, 1));
        Assert.Throws<ArgumentException>(() =>
            FastingOccurrence.Schedule(FastingPlanId.New(), UserId.Empty, FastingOccurrenceKind.FastDay, DateTime.UtcNow, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidSequence_Throws(int sequenceNumber) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingOccurrence.Create(FastingPlanId.New(), UserId.New(), FastingOccurrenceKind.FastDay, DateTime.UtcNow, sequenceNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(169)]
    public void Create_WithInvalidTargetHours_Throws(int targetHours) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingOccurrence.Create(FastingPlanId.New(), UserId.New(), FastingOccurrenceKind.FastDay, DateTime.UtcNow, 1, targetHours));
    }

    [Fact]
    public void Create_WithUnspecifiedTimestamp_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingOccurrence.Create(FastingPlanId.New(), UserId.New(), FastingOccurrenceKind.FastDay, new DateTime(2026, 3, 27), 1));
    }

    [Fact]
    public void Schedule_WithValidValues_CreatesScheduledOccurrence() {
        DateTime scheduledForUtc = DateTime.UtcNow.AddDays(1);

        var occurrence = FastingOccurrence.Schedule(
            FastingPlanId.New(),
            UserId.New(),
            FastingOccurrenceKind.EatDay,
            scheduledForUtc,
            sequenceNumber: 2,
            notes: "  Eat day  ");

        Assert.Multiple(
            () => Assert.Equal(FastingOccurrenceStatus.Scheduled, occurrence.Status),
            () => Assert.Equal(scheduledForUtc, occurrence.ScheduledForUtc),
            () => Assert.Equal(scheduledForUtc, occurrence.StartedAtUtc),
            () => Assert.Equal("Eat day", occurrence.Notes));
    }

    [Fact]
    public void Start_FromScheduled_ActivatesOccurrence() {
        var occurrence = FastingOccurrence.Schedule(
            FastingPlanId.New(),
            UserId.New(),
            FastingOccurrenceKind.FastDay,
            DateTime.UtcNow.AddDays(1),
            sequenceNumber: 1);
        DateTime startedAtUtc = DateTime.UtcNow;

        occurrence.Start(startedAtUtc);

        Assert.Multiple(
            () => Assert.Equal(FastingOccurrenceStatus.Active, occurrence.Status),
            () => Assert.Equal(startedAtUtc, occurrence.StartedAtUtc),
            () => Assert.Null(occurrence.EndedAtUtc));
        Assert.NotNull(occurrence.ModifiedOnUtc);
    }

    [Fact]
    public void Start_WhenAlreadyActive_DoesNotSetModifiedOnUtc() {
        DateTime startedAtUtc = DateTime.UtcNow;
        var occurrence = FastingOccurrence.Create(
            FastingPlanId.New(),
            UserId.New(),
            FastingOccurrenceKind.FastDay,
            startedAtUtc,
            sequenceNumber: 1);

        occurrence.Start(startedAtUtc.AddHours(1));

        Assert.Equal(startedAtUtc, occurrence.StartedAtUtc);
        Assert.Null(occurrence.ModifiedOnUtc);
    }

    [Fact]
    public void Start_FromCompleted_Throws() {
        var occurrence = FastingOccurrence.Create(FastingPlanId.New(), UserId.New(), FastingOccurrenceKind.FastDay, DateTime.UtcNow, 1);
        occurrence.Complete(DateTime.UtcNow.AddHours(1));

        Assert.Throws<InvalidOperationException>(() => occurrence.Start(DateTime.UtcNow.AddHours(2)));
    }

    [Fact]
    public void TerminalTransitions_SetExpectedStatuses() {
        var interrupted = FastingOccurrence.Create(FastingPlanId.New(), UserId.New(), FastingOccurrenceKind.FastDay, DateTime.UtcNow, 1);
        var skipped = FastingOccurrence.Schedule(FastingPlanId.New(), UserId.New(), FastingOccurrenceKind.EatDay, DateTime.UtcNow.AddDays(1), 2);

        interrupted.Interrupt(DateTime.UtcNow.AddHours(1));
        skipped.Skip(DateTime.UtcNow.AddDays(1));

        Assert.Equal(FastingOccurrenceStatus.Interrupted, interrupted.Status);
        Assert.NotNull(interrupted.EndedAtUtc);
        Assert.Equal(FastingOccurrenceStatus.Skipped, skipped.Status);
        Assert.NotNull(skipped.EndedAtUtc);
    }

    [Fact]
    public void Postpone_WhenNextScheduleIsNotLater_Throws() {
        var occurrence = FastingOccurrence.Schedule(
            FastingPlanId.New(),
            UserId.New(),
            FastingOccurrenceKind.FastDay,
            DateTime.UtcNow.AddDays(1),
            sequenceNumber: 1);
        DateTime postponedAtUtc = DateTime.UtcNow.AddDays(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => occurrence.Postpone(postponedAtUtc, postponedAtUtc));
    }

    [Fact]
    public void ExtendAndReduce_WhenNotActiveOrMissingTarget_Throw() {
        var scheduled = FastingOccurrence.Schedule(FastingPlanId.New(), UserId.New(), FastingOccurrenceKind.FastDay, DateTime.UtcNow.AddDays(1), 1, 24);
        var withoutTarget = FastingOccurrence.Create(FastingPlanId.New(), UserId.New(), FastingOccurrenceKind.FastDay, DateTime.UtcNow, 1);

        Assert.Throws<InvalidOperationException>(() => scheduled.Extend(1));
        Assert.Throws<InvalidOperationException>(() => scheduled.Reduce(1));
        Assert.Throws<InvalidOperationException>(() => withoutTarget.Extend(1));
        Assert.Throws<InvalidOperationException>(() => withoutTarget.Reduce(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ExtendAndReduce_WithInvalidHours_Throw(int hours) {
        var occurrence = FastingOccurrence.Create(FastingPlanId.New(), UserId.New(), FastingOccurrenceKind.FastDay, DateTime.UtcNow, 1, 24);

        Assert.Throws<ArgumentOutOfRangeException>(() => occurrence.Extend(hours));
        Assert.Throws<ArgumentOutOfRangeException>(() => occurrence.Reduce(hours));
    }

    [Fact]
    public void UpdateNotes_NormalizesAndAvoidsUnchangedUpdates() {
        var occurrence = FastingOccurrence.Create(
            FastingPlanId.New(),
            UserId.New(),
            FastingOccurrenceKind.FastDay,
            DateTime.UtcNow,
            sequenceNumber: 1,
            notes: "Initial");

        occurrence.UpdateNotes("  Initial  ");
        Assert.Null(occurrence.ModifiedOnUtc);

        occurrence.UpdateNotes("  New  ");
        Assert.Equal("New", occurrence.Notes);
        Assert.NotNull(occurrence.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateNotes_WithTooLongValue_Throws() {
        var occurrence = FastingOccurrence.Create(FastingPlanId.New(), UserId.New(), FastingOccurrenceKind.FastDay, DateTime.UtcNow, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => occurrence.UpdateNotes(new string('n', 501)));
    }

    [Fact]
    public void UpdateCheckIn_NormalizesSymptomsAndNotes() {
        var occurrence = FastingOccurrence.Create(FastingPlanId.New(), UserId.New(), FastingOccurrenceKind.FastDay, DateTime.UtcNow, 1);
        DateTime checkedInAtUtc = DateTime.UtcNow.AddHours(1);

        occurrence.UpdateCheckIn(
            hungerLevel: 1,
            energyLevel: 2,
            moodLevel: 3,
            symptoms: ["  headache  ", "HEADACHE", " tired "],
            checkInNotes: "  ok  ",
            checkedInAtUtc);

        Assert.Multiple(
            () => Assert.Equal(1, occurrence.HungerLevel),
            () => Assert.Equal(2, occurrence.EnergyLevel),
            () => Assert.Equal(3, occurrence.MoodLevel),
            () => Assert.Equal("headache,tired", occurrence.Symptoms),
            () => Assert.Equal("ok", occurrence.CheckInNotes),
            () => Assert.Equal(checkedInAtUtc, occurrence.CheckInAtUtc));
    }

    [Fact]
    public void UpdateCheckIn_WithBlankSymptomsAndNotes_NormalizesToNull() {
        var occurrence = FastingOccurrence.Create(FastingPlanId.New(), UserId.New(), FastingOccurrenceKind.FastDay, DateTime.UtcNow, 1);

        occurrence.UpdateCheckIn(1, 2, 3, ["   "], "   ", DateTime.UtcNow);

        Assert.Null(occurrence.Symptoms);
        Assert.Null(occurrence.CheckInNotes);
    }

    [Fact]
    public void UpdateCheckIn_WithInvalidValues_Throws() {
        var occurrence = FastingOccurrence.Create(FastingPlanId.New(), UserId.New(), FastingOccurrenceKind.FastDay, DateTime.UtcNow, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => occurrence.UpdateCheckIn(0, 2, 3, symptoms: null, checkInNotes: null, DateTime.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() => occurrence.UpdateCheckIn(1, 6, 3, symptoms: null, checkInNotes: null, DateTime.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() => occurrence.UpdateCheckIn(1, 2, 6, symptoms: null, checkInNotes: null, DateTime.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() => occurrence.UpdateCheckIn(1, 2, 3, Enumerable.Range(0, 9).Select(i => string.Create(CultureInfo.InvariantCulture, $"s{i}")), checkInNotes: null, DateTime.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() => occurrence.UpdateCheckIn(1, 2, 3, [new string('s', 201)], checkInNotes: null, DateTime.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() => occurrence.UpdateCheckIn(1, 2, 3, symptoms: null, new string('n', 501), DateTime.UtcNow));
        Assert.Throws<ArgumentOutOfRangeException>(() => occurrence.UpdateCheckIn(1, 2, 3, symptoms: null, checkInNotes: null, new DateTime(2026, 3, 27)));
    }
}

using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Modules.Fasting.Domain.Enums;
using FoodDiary.Modules.Fasting.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class FastingCompletionChronologyTests {
    [Fact]
    public void FastingTransitions_EnforceStatusAndChronology() {
        DateTime startedAt = DateTime.UtcNow;
        var active = FastingOccurrence.Create(
            FastingPlanId.New(), UserId.New(), FastingOccurrenceKind.FastDay, startedAt, 1);
        var scheduled = FastingOccurrence.Schedule(
            FastingPlanId.New(), UserId.New(), FastingOccurrenceKind.FastDay, startedAt.AddDays(1), 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => active.Complete(startedAt.AddSeconds(-1)));
        Assert.Throws<InvalidOperationException>(() => scheduled.Complete(startedAt.AddDays(2)));
        Assert.Multiple(
            () => Assert.Equal(FastingOccurrenceStatus.Active, active.Status),
            () => Assert.Null(active.EndedAtUtc),
            () => Assert.Equal(FastingOccurrenceStatus.Scheduled, scheduled.Status));
    }
}

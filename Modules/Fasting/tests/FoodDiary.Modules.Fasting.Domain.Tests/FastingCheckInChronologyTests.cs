using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Modules.Fasting.Domain.Enums;
using FoodDiary.Modules.Fasting.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class FastingCheckInChronologyTests {
    private static readonly DateTime Now = new(2026, 8, 19, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FastingOccurrence_CheckInRequiresActiveChronologicalOccurrence() {
        var occurrence = FastingOccurrence.Create(
            FastingPlanId.New(),
            UserId.New(),
            FastingOccurrenceKind.FastDay,
            Now,
            sequenceNumber: 1,
            targetHours: 24);

        Assert.Throws<ArgumentOutOfRangeException>(() => occurrence.UpdateCheckIn(
            1, 2, 3, symptoms: null, checkInNotes: null, Now.AddTicks(-1)));
        Assert.Null(occurrence.CheckInAtUtc);

        occurrence.Complete(Now.AddHours(24));
        Assert.Throws<InvalidOperationException>(() => occurrence.UpdateCheckIn(
            1, 2, 3, symptoms: null, checkInNotes: null, Now.AddHours(12)));
    }
}

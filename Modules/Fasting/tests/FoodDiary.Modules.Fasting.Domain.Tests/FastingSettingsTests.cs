using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Modules.Fasting.Domain.Enums;
using FoodDiary.Modules.Fasting.Domain.ValueObjects.Settings;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class FastingSettingsTests {
    [Fact]
    public void ModeSettings_MapOnlyTheirOwnStorageFields() {
        DateTime now = new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
        var intermittent = FastingPlan.CreateWithSettings(UserId.New(), IntermittentFastingSettings.Create(FastingProtocol.Fast16Eat8, 16, 8), now);
        var extended = FastingPlan.CreateWithSettings(UserId.New(), ExtendedFastingSettings.Create(FastingProtocol.Fast72, 72), now);
        var cyclic = FastingPlan.CreateWithSettings(UserId.New(), CyclicFastingSettings.FromDateTimeEncoding(1, 3, 16, 8, now), now);
        Assert.Multiple(
            () => Assert.Equal(16, intermittent.IntermittentFastHours),
            () => Assert.Null(intermittent.ExtendedTargetHours),
            () => Assert.Null(intermittent.CyclicFastDays),
            () => Assert.Equal(72, extended.ExtendedTargetHours),
            () => Assert.Null(extended.IntermittentFastHours),
            () => Assert.Equal(1, cyclic.CyclicFastDays),
            () => Assert.Equal(now.Date, cyclic.CyclicAnchorDateUtc),
            () => Assert.Null(cyclic.ExtendedTargetHours));
    }

    [Fact]
    public void Settings_RejectInvalidProtocolWindowAndUnspecifiedAnchor() {
        Assert.Throws<ArgumentOutOfRangeException>(() => ExtendedFastingSettings.Create(FastingProtocol.Fast16Eat8, 72));
        Assert.Throws<ArgumentOutOfRangeException>(() => DailyFastingWindow.FromHours(17, 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => CyclicFastingSettings.FromDateTimeEncoding(1, 3, 16, 8, new DateTime(2026, 10, 8)));
        var minimum = FastingCycleDay.FromDateTimeEncoding(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
        Assert.Equal(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc), minimum.ToUtcDateTime());
    }

}

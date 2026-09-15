using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Wearables.Domain.Entities;
using FoodDiary.Modules.Wearables.Domain.Enums;

namespace FoodDiary.Modules.Wearables.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class WearableSyncEntryValidationTests {

    [Fact]
    public void WearableSyncEntry_RejectsNonFiniteValuesAndNormalizesDateToUtc() {
        var unspecified = new DateTime(2026, 4, 28, 17, 45, 0, DateTimeKind.Unspecified);
        var entry = WearableSyncEntry.Create(
            UserId.New(),
            WearableProvider.Fitbit,
            WearableDataType.Steps,
            unspecified,
            10_000);

        Assert.Multiple(
            () => Assert.Equal(DateTimeKind.Utc, entry.Date.Kind),
            () => Assert.Equal(new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc), entry.Date),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => entry.UpdateValue(double.NaN)),
            () => Assert.Equal(10_000, entry.Value));
    }
}

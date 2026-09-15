using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Cycles.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class CycleSettingsAtomicityTests {
    [Fact]
    public void UpdateSettings_WhenLateValidationFails_PreservesAllFields() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => profile.UpdateSettings(new CycleProfileSettings(
            CycleTrackingMode.TryingToConceive,
            AverageCycleLength: 28,
            AveragePeriodLength: 99,
            LutealLength: 14,
            IsRegular: null,
            IsOnboardingComplete: null,
            ShowFertilityEstimates: null,
            DiscreetNotifications: null,
            Notes: null)));
        Assert.Multiple(
            () => Assert.Equal(CycleTrackingMode.PeriodTracking, profile.Mode),
            () => Assert.Equal(5, profile.AveragePeriodLength),
            () => Assert.Null(profile.ModifiedOnUtc));
    }
}

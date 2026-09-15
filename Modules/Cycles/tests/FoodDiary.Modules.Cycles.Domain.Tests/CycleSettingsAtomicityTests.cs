using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Cycles.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class CycleSettingsAtomicityTests {
    [Theory]
    [InlineData(CycleTrackingMode.Pregnancy, null, null, CycleTrackingMode.Pregnancy, CycleTrackingGoal.PeriodAwareness, CycleReproductiveState.Pregnancy)]
    [InlineData(CycleTrackingMode.PeriodTracking, null, null, CycleTrackingMode.PeriodTracking, CycleTrackingGoal.PeriodAwareness, CycleReproductiveState.Cycling)]
    [InlineData(CycleTrackingMode.TryingToConceive, null, null, CycleTrackingMode.TryingToConceive, CycleTrackingGoal.TryingToConceive, CycleReproductiveState.Cycling)]
    [InlineData(CycleTrackingMode.PostpartumLactation, null, null, CycleTrackingMode.PostpartumLactation, CycleTrackingGoal.PeriodAwareness, CycleReproductiveState.Postpartum)]
    [InlineData(CycleTrackingMode.Pregnancy, CycleTrackingGoal.TryingToConceive, CycleReproductiveState.Cycling, CycleTrackingMode.TryingToConceive, CycleTrackingGoal.TryingToConceive, CycleReproductiveState.Cycling)]
    [InlineData(CycleTrackingMode.Pregnancy, CycleTrackingGoal.TryingToConceive, null, CycleTrackingMode.Pregnancy, CycleTrackingGoal.TryingToConceive, CycleReproductiveState.Pregnancy)]
    [InlineData(CycleTrackingMode.TryingToConceive, null, CycleReproductiveState.Lactation, CycleTrackingMode.PostpartumLactation, CycleTrackingGoal.TryingToConceive, CycleReproductiveState.Lactation)]
    public void UpdateSettings_KeepsLegacyAndExplicitStateConsistent(
        CycleTrackingMode requestedMode, CycleTrackingGoal? goal, CycleReproductiveState? state,
        CycleTrackingMode expectedMode, CycleTrackingGoal expectedGoal, CycleReproductiveState expectedState) {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 1, 1), CycleTrackingMode.NoPeriod);
        profile.UpdateSettings(new CycleProfileSettings(requestedMode,
            AverageCycleLength: null, AveragePeriodLength: null, LutealLength: null, IsRegular: null,
            IsOnboardingComplete: null, ShowFertilityEstimates: null, DiscreetNotifications: null,
            Notes: null, Goal: goal, ReproductiveState: state));
        Assert.Multiple(
            () => Assert.Equal(expectedMode, profile.Mode),
            () => Assert.Equal(expectedGoal, profile.Goal),
            () => Assert.Equal(expectedState, profile.ReproductiveState));
    }

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
            () => Assert.Equal(CycleTrackingGoal.PeriodAwareness, profile.Goal),
            () => Assert.Equal(CycleReproductiveState.Cycling, profile.ReproductiveState),
            () => Assert.Equal(5, profile.AveragePeriodLength),
            () => Assert.Null(profile.ModifiedOnUtc));
    }
}

using FoodDiary.Modules.Tdee.Application.Common;
using FoodDiary.Modules.Tdee.Application.Services;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

namespace FoodDiary.Modules.Tdee.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class CalculationWeightBoundaryTests {
    [Theory]
    [InlineData(80, 70, 1700)]
    [InlineData(70, 80, 2500)]
    [InlineData(80, 80, 2200)]
    public void ValidatedWeights_KeepTheirOwnerValuesAndProduceTheExistingCalorieAdjustment(double currentKg, double desiredKg, double expectedKcal) {
        var measured = MeasuredWeightKg.Create(currentKg);
        var desired = DesiredWeightKg.Create(desiredKg);
        var current = CalculationMeasuredWeight.FromMeasurement(measured);
        var target = CalculationDesiredWeight.FromMeasurement(desired);
        EstimatedDailyEnergyKcal energy = EstimatedDailyEnergyKcal.FromOptional(2200)!;

        double? result = TdeeCalculator.SuggestCalorieTargetForProfile(energy, current, target);

        Assert.Multiple(
            () => Assert.Same(measured, current.Validated),
            () => Assert.Same(desired, target.Validated),
            () => Assert.Equal(currentKg, current.Value),
            () => Assert.Equal(desiredKg, target.Value),
            () => Assert.Equal(expectedKcal, result));
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(68.25)]
    [InlineData(500)]
    public void StoredValidWeights_AreValidatedWithoutChangingKilograms(double kilograms) {
        CalculationDesiredWeight desired = CalculationDesiredWeight.FromOptionalStoredValue(kilograms)!;
        CalculationMeasuredWeight measured = CalculationMeasuredWeight.FromOptionalStoredValue(kilograms)!;
        DesiredWeightKg validatedDesired = Assert.IsType<DesiredWeightKg>(desired.Validated);
        MeasuredWeightKg validatedMeasured = Assert.IsType<MeasuredWeightKg>(measured.Validated);

        Assert.Multiple(
            () => Assert.Equal(kilograms, desired.Value),
            () => Assert.Equal(kilograms, validatedDesired.Value),
            () => Assert.Equal(kilograms, measured.Value),
            () => Assert.Equal(kilograms, validatedMeasured.Value));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(500.001)]
    public void LegacyInvalidWeights_RemainAvailableToTheCompatibilityPolicyWithoutClaimingValidation(double kilograms) {
        CalculationDesiredWeight desired = CalculationDesiredWeight.FromOptionalStoredValue(kilograms)!;
        CalculationMeasuredWeight measured = CalculationMeasuredWeight.FromOptionalStoredValue(kilograms)!;

        Assert.Multiple(
            () => Assert.Equal(kilograms, desired.Value),
            () => Assert.Null(desired.Validated),
            () => Assert.Equal(kilograms, measured.Value),
            () => Assert.Null(measured.Validated));
    }

    [Fact]
    public void MissingWeights_StayAbsentAndTypedFactoriesRejectMissingOwnerValues() {
        Assert.Null(CalculationDesiredWeight.FromOptionalStoredValue(value: null));
        Assert.Null(CalculationMeasuredWeight.FromOptionalStoredValue(value: null));
        Assert.Throws<ArgumentNullException>("value", () => CalculationDesiredWeight.FromMeasurement(null!));
        Assert.Throws<ArgumentNullException>("value", () => CalculationMeasuredWeight.FromMeasurement(null!));
    }
}

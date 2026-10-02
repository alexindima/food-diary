using FoodDiary.Modules.Cycles.Application.Commands.UpsertCycleDay;
using FluentValidation.TestHelper;

namespace FoodDiary.Modules.Cycles.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FertilitySignalValidationTests {
    [Theory]
    [InlineData(33.99)]
    [InlineData(42.01)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public async Task InvalidTemperature_UsesClientValidationError(double temperature) {
        var signal = new FertilitySignalCommandModel(temperature, OvulationTestResult: null, CervicalFluid: null, HadSex: null, Notes: null, ClearNotes: false);
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(CreateCommand(signal));
        result.ShouldHaveValidationErrorFor("FertilitySignal.BasalBodyTemperatureCelsius").WithErrorCode("Validation.Invalid");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(34.0)]
    [InlineData(42.0)]
    public async Task ValidTemperature_Passes(double? temperature) {
        var signal = new FertilitySignalCommandModel(temperature, OvulationTestResult: null, CervicalFluid: null, HadSex: null, Notes: null, ClearNotes: false);
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(CreateCommand(signal));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(128, false)]
    [InlineData(129, true)]
    [InlineData(501, true)]
    [InlineData(1024, true)]
    public async Task CervicalFluid_RespectsTrimmedPersistenceLimit(int length, bool invalid) {
        var signal = new FertilitySignalCommandModel(BasalBodyTemperatureCelsius: null, OvulationTestResult: null, CervicalFluid: "  " + new string('x', length) + "  ", HadSex: null, Notes: null, ClearNotes: false);
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(CreateCommand(signal));
        if (invalid) {
            result.ShouldHaveValidationErrorFor("FertilitySignal.CervicalFluid").WithErrorCode("Validation.Invalid");
        } else {
            result.ShouldNotHaveAnyValidationErrors();
        }
    }

    private static UpsertCycleDayCommand CreateCommand(FertilitySignalCommandModel signal) =>
        new(UserId: Guid.NewGuid(), CycleProfileId: Guid.NewGuid(), Date: new DateOnly(2026, 4, 2), Bleeding: null, Symptoms: [], FertilitySignal: signal);
}

using FluentValidation.TestHelper;
using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Application.Cycles.Models;

namespace FoodDiary.Application.Tests.Cycles;

public class CyclesValidatorTests {
    [Fact]
    public async Task CreateCycle_WithNullUserId_HasError() {
        var result = await new CreateCycleCommandValidator().TestValidateAsync(
            new CreateCycleCommand(null, DateTime.UtcNow, null, null, null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task CreateCycle_WithAverageLengthOutOfRange_HasError() {
        var result = await new CreateCycleCommandValidator().TestValidateAsync(
            new CreateCycleCommand(Guid.NewGuid(), DateTime.UtcNow, 10, null, null));
        result.ShouldHaveValidationErrorFor(c => c.AverageLength);
    }

    [Fact]
    public async Task CreateCycle_WithLutealLengthOutOfRange_HasError() {
        var result = await new CreateCycleCommandValidator().TestValidateAsync(
            new CreateCycleCommand(Guid.NewGuid(), DateTime.UtcNow, null, 5, null));
        result.ShouldHaveValidationErrorFor(c => c.LutealLength);
    }

    [Fact]
    public async Task CreateCycle_WithValidData_NoErrors() {
        var result = await new CreateCycleCommandValidator().TestValidateAsync(
            new CreateCycleCommand(Guid.NewGuid(), DateTime.UtcNow, 28, 14, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpsertCycleDay_WithNullUserId_HasError() {
        var result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            new UpsertCycleDayCommand(null, Guid.NewGuid(), DateTime.UtcNow, false, new DailySymptomsModel(0, 0, 0, 0, 0, 0, 0), null, false));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task UpsertCycleDay_WithEmptyCycleId_HasError() {
        var result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            new UpsertCycleDayCommand(Guid.NewGuid(), Guid.Empty, DateTime.UtcNow, false, new DailySymptomsModel(0, 0, 0, 0, 0, 0, 0), null, false));
        result.ShouldHaveValidationErrorFor(c => c.CycleId);
    }

    [Fact]
    public async Task UpsertCycleDay_WithNullSymptoms_HasError() {
        var result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            new UpsertCycleDayCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, false, null!, null, false));
        result.ShouldHaveValidationErrorFor(c => c.Symptoms);
    }

    [Fact]
    public async Task UpsertCycleDay_WithClearNotesAndNotes_HasError() {
        var result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            new UpsertCycleDayCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, false, new DailySymptomsModel(0, 0, 0, 0, 0, 0, 0), "notes", true));
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task UpsertCycleDay_WithSymptomOutOfRange_HasError() {
        var result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            new UpsertCycleDayCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, false, new DailySymptomsModel(10, 0, 0, 0, 0, 0, 0), null, false));
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task UpsertCycleDay_WithValidData_NoErrors() {
        var result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            new UpsertCycleDayCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, true, new DailySymptomsModel(3, 5, 2, 1, 4, 7, 3), "notes", false));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

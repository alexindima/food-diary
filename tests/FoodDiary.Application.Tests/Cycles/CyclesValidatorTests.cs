using FluentValidation.TestHelper;
using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Commands.UpsertCycleFactor;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Tests.Cycles;

[ExcludeFromCodeCoverage]
public class CyclesValidatorTests {
    [Fact]
    public async Task CreateCycle_WithNullUserId_HasError() {
        TestValidationResult<CreateCycleCommand> result = await new CreateCycleCommandValidator().TestValidateAsync(
            CreateCommand(UseNullUserId: true));

        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task CreateCycle_WithAverageLengthOutOfRange_HasError() {
        TestValidationResult<CreateCycleCommand> result = await new CreateCycleCommandValidator().TestValidateAsync(
            CreateCommand(AverageCycleLength: 10));

        result.ShouldHaveValidationErrorFor(c => c.AverageCycleLength);
    }

    [Fact]
    public async Task CreateCycle_WithValidData_Passes() {
        TestValidationResult<CreateCycleCommand> result = await new CreateCycleCommandValidator().TestValidateAsync(CreateCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpsertCycleDay_WithEmptyProfileId_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(CycleProfileId: Guid.Empty));

        result.ShouldHaveValidationErrorFor(c => c.CycleProfileId);
    }

    [Fact]
    public async Task UpsertCycleDay_WithClearNotesAndValue_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(Bleeding: new BleedingLogCommandModel((int)BleedingType.Bleeding, (int)CycleFlowLevel.Light, PainImpact: null, Notes: "notes", ClearNotes: true)));

        result.ShouldHaveValidationErrorFor("Bleeding");
    }

    [Fact]
    public async Task UpsertCycleDay_WithValidData_Passes() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(CreateDayCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpsertCycleFactor_WithEndBeforeStart_HasError() {
        DateTime startDate = new(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);

        TestValidationResult<UpsertCycleFactorCommand> result = await new UpsertCycleFactorCommandValidator().TestValidateAsync(
            CreateFactorCommand(StartDate: startDate, EndDate: startDate.AddDays(-1)));

        result.ShouldHaveValidationErrorFor(c => c.EndDate);
    }

    [Fact]
    public async Task UpsertCycleFactor_WithClearNotesAndValue_HasError() {
        TestValidationResult<UpsertCycleFactorCommand> result = await new UpsertCycleFactorCommandValidator().TestValidateAsync(
            CreateFactorCommand(Notes: "notes", ClearNotes: true));

        result.ShouldHaveValidationErrorFor(string.Empty);
    }

    [Fact]
    public async Task UpsertCycleFactor_WithValidData_Passes() {
        TestValidationResult<UpsertCycleFactorCommand> result = await new UpsertCycleFactorCommandValidator().TestValidateAsync(CreateFactorCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static CreateCycleCommand CreateCommand(
        bool UseNullUserId = false,
        int? AverageCycleLength = 28) =>
        new(
            UseNullUserId ? null : Guid.NewGuid(),
            DateTime.UtcNow,
            (int)CycleTrackingMode.PeriodTracking,
            AverageCycleLength,
            AveragePeriodLength: 5,
            LutealLength: 14,
            IsRegular: false,
            IsOnboardingComplete: false,
            ShowFertilityEstimates: false,
            DiscreetNotifications: true,
            Notes: null);

    private static UpsertCycleDayCommand CreateDayCommand(
        Guid? CycleProfileId = null,
        BleedingLogCommandModel? Bleeding = null) =>
        new(
            Guid.NewGuid(),
            CycleProfileId ?? Guid.NewGuid(),
            DateTime.UtcNow,
            Bleeding ?? new BleedingLogCommandModel((int)BleedingType.Bleeding, (int)CycleFlowLevel.Light, PainImpact: null, Notes: null, ClearNotes: false),
            [new SymptomLogCommandModel((int)CycleSymptomCategory.Pain, 3, [], Note: null, ClearNote: false)],
            FertilitySignal: null);

    private static UpsertCycleFactorCommand CreateFactorCommand(
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        string? Notes = null,
        bool ClearNotes = false) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            (int)CycleFactorType.HormonalContraception,
            StartDate ?? DateTime.UtcNow,
            EndDate,
            Notes,
            ClearNotes);
}

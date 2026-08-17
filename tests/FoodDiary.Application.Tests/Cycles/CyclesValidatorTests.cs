using FluentValidation.TestHelper;
using FoodDiary.Application.Cycles.Commands.ClearCycleDay;
using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Commands.DeleteCycleProfile;
using FoodDiary.Application.Cycles.Commands.UpsertCycleFactor;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Application.Cycles.Queries.GetCycleNutritionSummary;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Tests.Cycles;

[ExcludeFromCodeCoverage]
public class CyclesValidatorTests {
    [Fact]
    public async Task DeleteCycleProfile_WithNullUserId_HasError() {
        TestValidationResult<DeleteCycleProfileCommand> result = await new DeleteCycleProfileCommandValidator().TestValidateAsync(
            new DeleteCycleProfileCommand(UserId: null, Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(command => command.UserId);
    }

    [Fact]
    public async Task DeleteCycleProfile_WithEmptyProfileId_HasError() {
        TestValidationResult<DeleteCycleProfileCommand> result = await new DeleteCycleProfileCommandValidator().TestValidateAsync(
            new DeleteCycleProfileCommand(Guid.NewGuid(), Guid.Empty));

        result.ShouldHaveValidationErrorFor(command => command.CycleProfileId);
    }

    [Fact]
    public async Task CreateCycle_WithNullUserId_HasError() {
        TestValidationResult<CreateCycleCommand> result = await new CreateCycleCommandValidator().TestValidateAsync(
            CreateCommand(useNullUserId: true));

        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task CreateCycle_WithAverageLengthOutOfRange_HasError() {
        TestValidationResult<CreateCycleCommand> result = await new CreateCycleCommandValidator().TestValidateAsync(
            CreateCommand(averageCycleLength: 10));

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
            CreateDayCommand(cycleProfileId: Guid.Empty));

        result.ShouldHaveValidationErrorFor(c => c.CycleProfileId);
    }

    [Fact]
    public async Task UpsertCycleDay_WithNullUserId_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(useNullUserId: true));

        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task UpsertCycleDay_WithEmptyUserId_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(userId: Guid.Empty));

        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task UpsertCycleDay_WithNullSymptoms_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(useNullSymptoms: true));

        result.ShouldHaveValidationErrorFor(c => c.Symptoms);
    }

    [Fact]
    public async Task UpsertCycleDay_WithClearNotesAndValue_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(bleeding: new BleedingLogCommandModel((int)BleedingType.Bleeding, (int)CycleFlowLevel.Light, PainImpact: null, Notes: "notes", ClearNotes: true)));

        result.ShouldHaveValidationErrorFor("Bleeding");
    }

    [Theory]
    [InlineData(999, (int)CycleFlowLevel.Light)]
    [InlineData((int)BleedingType.Bleeding, 999)]
    public async Task UpsertCycleDay_WithInvalidBleedingEnum_HasError(int type, int flow) {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(bleeding: new BleedingLogCommandModel(type, flow, PainImpact: null, Notes: null, ClearNotes: false)));

        Assert.Contains(result.Errors, error => error.PropertyName is "Bleeding.Type" or "Bleeding.Flow");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public async Task UpsertCycleDay_WithInvalidBleedingPainImpact_HasError(int painImpact) {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(bleeding: new BleedingLogCommandModel((int)BleedingType.Bleeding, (int)CycleFlowLevel.Light, painImpact, Notes: null, ClearNotes: false)));

        result.ShouldHaveValidationErrorFor("Bleeding.PainImpact");
    }

    [Fact]
    public async Task UpsertCycleDay_WithInvalidSymptomCategory_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(symptoms: [new SymptomLogCommandModel(999, 3, [], Note: null, ClearNote: false)]));

        result.ShouldHaveValidationErrorFor("Symptoms[0].Category");
    }

    [Fact]
    public async Task UpsertCycleDay_WithNullSymptomTags_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(symptoms: [new SymptomLogCommandModel((int)CycleSymptomCategory.Pain, 3, null!, Note: null, ClearNote: false)]));

        result.ShouldHaveValidationErrorFor("Symptoms[0].Tags");
    }

    [Fact]
    public async Task UpsertCycleDay_WithClearSymptomNoteAndValue_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(symptoms: [new SymptomLogCommandModel((int)CycleSymptomCategory.Pain, 3, [], Note: "note", ClearNote: true)]));

        result.ShouldHaveValidationErrorFor("Symptoms[0]");
    }

    [Fact]
    public async Task UpsertCycleDay_WithInvalidFertilityTemperature_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(fertilitySignal: new FertilitySignalCommandModel(43, OvulationTestResult: null, CervicalFluid: null, HadSex: null, Notes: null, ClearNotes: false)));

        result.ShouldHaveValidationErrorFor("FertilitySignal.BasalBodyTemperatureCelsius");
    }

    [Fact]
    public async Task UpsertCycleDay_WithInvalidOvulationTestResult_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(fertilitySignal: new FertilitySignalCommandModel(BasalBodyTemperatureCelsius: null, OvulationTestResult: 999, CervicalFluid: null, HadSex: null, Notes: null, ClearNotes: false)));

        result.ShouldHaveValidationErrorFor("FertilitySignal.OvulationTestResult");
    }

    [Fact]
    public async Task UpsertCycleDay_WithClearFertilityNotesAndValue_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(fertilitySignal: new FertilitySignalCommandModel(BasalBodyTemperatureCelsius: null, OvulationTestResult: null, CervicalFluid: null, HadSex: null, Notes: "note", ClearNotes: true)));

        result.ShouldHaveValidationErrorFor("FertilitySignal");
    }

    [Fact]
    public async Task UpsertCycleDay_WithInvalidClearSymptomCategory_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(clearSymptomCategories: [999]));

        result.ShouldHaveValidationErrorFor("ClearSymptomCategories[0]");
    }

    [Fact]
    public async Task UpsertCycleDay_WithValidData_Passes() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(CreateDayCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpsertCycleFactor_WithEndBeforeStart_HasError() {
        DateOnly startDate = new(2026, 4, 2);

        TestValidationResult<UpsertCycleFactorCommand> result = await new UpsertCycleFactorCommandValidator().TestValidateAsync(
            CreateFactorCommand(startDate: startDate, endDate: startDate.AddDays(-1)));

        result.ShouldHaveValidationErrorFor(c => c.EndDate);
    }

    [Fact]
    public async Task UpsertCycleFactor_WithClearNotesAndValue_HasError() {
        TestValidationResult<UpsertCycleFactorCommand> result = await new UpsertCycleFactorCommandValidator().TestValidateAsync(
            CreateFactorCommand(notes: "notes", clearNotes: true));

        result.ShouldHaveValidationErrorFor(string.Empty);
    }

    [Fact]
    public async Task UpsertCycleFactor_WithValidData_Passes() {
        TestValidationResult<UpsertCycleFactorCommand> result = await new UpsertCycleFactorCommandValidator().TestValidateAsync(CreateFactorCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ClearCycleDay_WithNullUserId_HasError() {
        TestValidationResult<ClearCycleDayCommand> result = await new ClearCycleDayCommandValidator().TestValidateAsync(
            CreateClearDayCommand(useNullUserId: true));

        result.ShouldHaveValidationErrorFor(command => command.UserId);
    }

    [Fact]
    public async Task ClearCycleDay_WithEmptyUserId_HasError() {
        TestValidationResult<ClearCycleDayCommand> result = await new ClearCycleDayCommandValidator().TestValidateAsync(
            CreateClearDayCommand(userId: Guid.Empty));

        result.ShouldHaveValidationErrorFor(command => command.UserId);
    }

    [Fact]
    public async Task ClearCycleDay_WithEmptyProfileId_HasError() {
        TestValidationResult<ClearCycleDayCommand> result = await new ClearCycleDayCommandValidator().TestValidateAsync(
            CreateClearDayCommand(cycleProfileId: Guid.Empty));

        result.ShouldHaveValidationErrorFor(command => command.CycleProfileId);
    }

    [Fact]
    public async Task ClearCycleDay_WithValidData_Passes() {
        TestValidationResult<ClearCycleDayCommand> result = await new ClearCycleDayCommandValidator().TestValidateAsync(CreateClearDayCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task GetCycleNutritionSummary_WithNullUserId_HasError() {
        TestValidationResult<GetCycleNutritionSummaryQuery> result = await new GetCycleNutritionSummaryQueryValidator().TestValidateAsync(
            new GetCycleNutritionSummaryQuery(UserId: null, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7), DateOnly.FromDateTime(DateTime.UtcNow)));

        result.ShouldHaveValidationErrorFor(query => query.UserId);
    }

    [Fact]
    public async Task GetCycleNutritionSummary_WithInvertedDates_HasError() {
        TestValidationResult<GetCycleNutritionSummaryQuery> result = await new GetCycleNutritionSummaryQueryValidator().TestValidateAsync(
            new GetCycleNutritionSummaryQuery(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1)));

        result.ShouldHaveValidationErrorFor(query => query.DateFrom);
    }

    [Fact]
    public async Task GetCycleNutritionSummary_WithTooLargeRange_HasError() {
        DateOnly from = new(2025, 1, 1);

        TestValidationResult<GetCycleNutritionSummaryQuery> result = await new GetCycleNutritionSummaryQueryValidator().TestValidateAsync(
            new GetCycleNutritionSummaryQuery(Guid.NewGuid(), from, from.AddDays(367)));

        Assert.Contains(result.Errors, error => string.Equals(error.ErrorCode, "Validation.Invalid", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetCycleNutritionSummary_WithValidData_Passes() {
        TestValidationResult<GetCycleNutritionSummaryQuery> result = await new GetCycleNutritionSummaryQueryValidator().TestValidateAsync(
            new GetCycleNutritionSummaryQuery(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7), DateOnly.FromDateTime(DateTime.UtcNow)));

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static CreateCycleCommand CreateCommand(
        bool useNullUserId = false,
        int? averageCycleLength = 28) =>
        new(
            useNullUserId ? null : Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            (int)CycleTrackingMode.PeriodTracking,
            averageCycleLength,
            AveragePeriodLength: 5,
            LutealLength: 14,
            IsRegular: false,
            IsOnboardingComplete: false,
            ShowFertilityEstimates: false,
            DiscreetNotifications: true,
            Notes: null,
            CycleTrackingConsentGranted: true);

    private static UpsertCycleDayCommand CreateDayCommand(
        bool useNullUserId = false,
        Guid? userId = null,
        Guid? cycleProfileId = null,
        BleedingLogCommandModel? bleeding = null,
        bool useNullSymptoms = false,
        IReadOnlyList<SymptomLogCommandModel>? symptoms = null,
        FertilitySignalCommandModel? fertilitySignal = null,
        IReadOnlyCollection<int>? clearSymptomCategories = null) =>
        new(
            useNullUserId ? null : userId ?? Guid.NewGuid(),
            cycleProfileId ?? Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            bleeding ?? new BleedingLogCommandModel((int)BleedingType.Bleeding, (int)CycleFlowLevel.Light, PainImpact: null, Notes: null, ClearNotes: false),
            useNullSymptoms ? null! : symptoms ?? [new SymptomLogCommandModel((int)CycleSymptomCategory.Pain, 3, [], Note: null, ClearNote: false)],
            fertilitySignal,
            ClearSymptomCategories: clearSymptomCategories);

    private static UpsertCycleFactorCommand CreateFactorCommand(
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? notes = null,
        bool clearNotes = false) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            (int)CycleFactorType.HormonalContraception,
            startDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            endDate,
            notes,
            clearNotes);

    private static ClearCycleDayCommand CreateClearDayCommand(
        bool useNullUserId = false,
        Guid? userId = null,
        Guid? cycleProfileId = null) =>
        new(
            useNullUserId ? null : userId ?? Guid.NewGuid(),
            cycleProfileId ?? Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow));
}

using FluentValidation.TestHelper;
using FoodDiary.Application.Cycles.Commands.ClearCycleDay;
using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Commands.UpsertCycleFactor;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Application.Cycles.Queries.GetCycleNutritionSummary;
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
    public async Task UpsertCycleDay_WithNullUserId_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(UseNullUserId: true));

        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task UpsertCycleDay_WithEmptyUserId_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(UserId: Guid.Empty));

        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task UpsertCycleDay_WithNullSymptoms_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(UseNullSymptoms: true));

        result.ShouldHaveValidationErrorFor(c => c.Symptoms);
    }

    [Fact]
    public async Task UpsertCycleDay_WithClearNotesAndValue_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(Bleeding: new BleedingLogCommandModel((int)BleedingType.Bleeding, (int)CycleFlowLevel.Light, PainImpact: null, Notes: "notes", ClearNotes: true)));

        result.ShouldHaveValidationErrorFor("Bleeding");
    }

    [Theory]
    [InlineData(999, (int)CycleFlowLevel.Light)]
    [InlineData((int)BleedingType.Bleeding, 999)]
    public async Task UpsertCycleDay_WithInvalidBleedingEnum_HasError(int type, int flow) {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(Bleeding: new BleedingLogCommandModel(type, flow, PainImpact: null, Notes: null, ClearNotes: false)));

        Assert.Contains(result.Errors, error => error.PropertyName is "Bleeding.Type" or "Bleeding.Flow");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public async Task UpsertCycleDay_WithInvalidBleedingPainImpact_HasError(int painImpact) {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(Bleeding: new BleedingLogCommandModel((int)BleedingType.Bleeding, (int)CycleFlowLevel.Light, painImpact, Notes: null, ClearNotes: false)));

        result.ShouldHaveValidationErrorFor("Bleeding.PainImpact");
    }

    [Fact]
    public async Task UpsertCycleDay_WithInvalidSymptomCategory_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(Symptoms: [new SymptomLogCommandModel(999, 3, [], Note: null, ClearNote: false)]));

        result.ShouldHaveValidationErrorFor("Symptoms[0].Category");
    }

    [Fact]
    public async Task UpsertCycleDay_WithNullSymptomTags_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(Symptoms: [new SymptomLogCommandModel((int)CycleSymptomCategory.Pain, 3, null!, Note: null, ClearNote: false)]));

        result.ShouldHaveValidationErrorFor("Symptoms[0].Tags");
    }

    [Fact]
    public async Task UpsertCycleDay_WithClearSymptomNoteAndValue_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(Symptoms: [new SymptomLogCommandModel((int)CycleSymptomCategory.Pain, 3, [], Note: "note", ClearNote: true)]));

        result.ShouldHaveValidationErrorFor("Symptoms[0]");
    }

    [Fact]
    public async Task UpsertCycleDay_WithInvalidFertilityTemperature_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(FertilitySignal: new FertilitySignalCommandModel(43, OvulationTestResult: null, CervicalFluid: null, HadSex: null, Notes: null, ClearNotes: false)));

        result.ShouldHaveValidationErrorFor("FertilitySignal.BasalBodyTemperatureCelsius");
    }

    [Fact]
    public async Task UpsertCycleDay_WithInvalidOvulationTestResult_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(FertilitySignal: new FertilitySignalCommandModel(BasalBodyTemperatureCelsius: null, OvulationTestResult: 999, CervicalFluid: null, HadSex: null, Notes: null, ClearNotes: false)));

        result.ShouldHaveValidationErrorFor("FertilitySignal.OvulationTestResult");
    }

    [Fact]
    public async Task UpsertCycleDay_WithClearFertilityNotesAndValue_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(FertilitySignal: new FertilitySignalCommandModel(BasalBodyTemperatureCelsius: null, OvulationTestResult: null, CervicalFluid: null, HadSex: null, Notes: "note", ClearNotes: true)));

        result.ShouldHaveValidationErrorFor("FertilitySignal");
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

    [Fact]
    public async Task ClearCycleDay_WithNullUserId_HasError() {
        TestValidationResult<ClearCycleDayCommand> result = await new ClearCycleDayCommandValidator().TestValidateAsync(
            CreateClearDayCommand(UseNullUserId: true));

        result.ShouldHaveValidationErrorFor(command => command.UserId);
    }

    [Fact]
    public async Task ClearCycleDay_WithEmptyUserId_HasError() {
        TestValidationResult<ClearCycleDayCommand> result = await new ClearCycleDayCommandValidator().TestValidateAsync(
            CreateClearDayCommand(UserId: Guid.Empty));

        result.ShouldHaveValidationErrorFor(command => command.UserId);
    }

    [Fact]
    public async Task ClearCycleDay_WithEmptyProfileId_HasError() {
        TestValidationResult<ClearCycleDayCommand> result = await new ClearCycleDayCommandValidator().TestValidateAsync(
            CreateClearDayCommand(CycleProfileId: Guid.Empty));

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
            new GetCycleNutritionSummaryQuery(UserId: null, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow));

        result.ShouldHaveValidationErrorFor(query => query.UserId);
    }

    [Fact]
    public async Task GetCycleNutritionSummary_WithInvertedDates_HasError() {
        TestValidationResult<GetCycleNutritionSummaryQuery> result = await new GetCycleNutritionSummaryQueryValidator().TestValidateAsync(
            new GetCycleNutritionSummaryQuery(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(-1)));

        result.ShouldHaveValidationErrorFor(query => query.DateFrom);
    }

    [Fact]
    public async Task GetCycleNutritionSummary_WithTooLargeRange_HasError() {
        DateTime from = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        TestValidationResult<GetCycleNutritionSummaryQuery> result = await new GetCycleNutritionSummaryQueryValidator().TestValidateAsync(
            new GetCycleNutritionSummaryQuery(Guid.NewGuid(), from, from.AddDays(367)));

        Assert.Contains(result.Errors, error => string.Equals(error.ErrorCode, "Validation.Invalid", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetCycleNutritionSummary_WithValidData_Passes() {
        TestValidationResult<GetCycleNutritionSummaryQuery> result = await new GetCycleNutritionSummaryQueryValidator().TestValidateAsync(
            new GetCycleNutritionSummaryQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-7), DateTime.UtcNow));

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
        bool UseNullUserId = false,
        Guid? UserId = null,
        Guid? CycleProfileId = null,
        BleedingLogCommandModel? Bleeding = null,
        bool UseNullSymptoms = false,
        IReadOnlyList<SymptomLogCommandModel>? Symptoms = null,
        FertilitySignalCommandModel? FertilitySignal = null) =>
        new(
            UseNullUserId ? null : UserId ?? Guid.NewGuid(),
            CycleProfileId ?? Guid.NewGuid(),
            DateTime.UtcNow,
            Bleeding ?? new BleedingLogCommandModel((int)BleedingType.Bleeding, (int)CycleFlowLevel.Light, PainImpact: null, Notes: null, ClearNotes: false),
            UseNullSymptoms ? null! : Symptoms ?? [new SymptomLogCommandModel((int)CycleSymptomCategory.Pain, 3, [], Note: null, ClearNote: false)],
            FertilitySignal);

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

    private static ClearCycleDayCommand CreateClearDayCommand(
        bool UseNullUserId = false,
        Guid? UserId = null,
        Guid? CycleProfileId = null) =>
        new(
            UseNullUserId ? null : UserId ?? Guid.NewGuid(),
            CycleProfileId ?? Guid.NewGuid(),
            DateTime.UtcNow);
}

using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;
using FluentValidation.TestHelper;
using FoodDiary.Modules.Cycles.Application.Commands.ClearCycleDay;
using FoodDiary.Modules.Cycles.Application.Commands.CreateCycle;
using FoodDiary.Modules.Cycles.Application.Commands.DeleteCycleProfile;
using FoodDiary.Modules.Cycles.Application.Commands.UpsertCycleFactor;
using FoodDiary.Modules.Cycles.Application.Commands.UpsertCycleDay;
using FoodDiary.Modules.Cycles.Application.Queries.GetCycleNutritionSummary;

namespace FoodDiary.Modules.Cycles.Application.Tests;

[ExcludeFromCodeCoverage]
public class CyclesValidatorTests {
    [Theory]
    [InlineData(0, false)]
    [InlineData(1024, false)]
    [InlineData(1025, true)]
    public async Task TextFields_ValidateTrimmedDomainLimit(int length, bool invalid) {
        string text = "  " + new string('x', length) + "  ";
        TestValidationResult<CreateCycleCommand> create = await new CreateCycleCommandValidator()
            .TestValidateAsync(CreateCommand() with { Notes = text });
        TestValidationResult<UpsertCycleFactorCommand> factor = await new UpsertCycleFactorCommandValidator()
            .TestValidateAsync(CreateFactorCommand(notes: text));
        TestValidationResult<UpsertCycleDayCommand> day = await new UpsertCycleDayCommandValidator()
            .TestValidateAsync(CreateDayCommand(
                bleeding: new BleedingLogCommandModel((int)BleedingType.Bleeding, (int)CycleFlowLevel.Light, PainImpact: null, Notes: text, ClearNotes: false),
                symptoms: [new SymptomLogCommandModel((int)CycleSymptomCategory.Pain, 3, [], text, ClearNote: false)],
                fertilitySignal: new FertilitySignalCommandModel(BasalBodyTemperatureCelsius: null, OvulationTestResult: null, CervicalFluid: text, HadSex: null, Notes: text, ClearNotes: false)));

        if (invalid) {
            create.ShouldHaveValidationErrorFor(command => command.Notes).WithErrorCode("Validation.Invalid");
            factor.ShouldHaveValidationErrorFor(command => command.Notes).WithErrorCode("Validation.Invalid");
            day.ShouldHaveValidationErrorFor("Bleeding.Notes").WithErrorCode("Validation.Invalid");
            day.ShouldHaveValidationErrorFor("Symptoms[0].Note").WithErrorCode("Validation.Invalid");
            day.ShouldHaveValidationErrorFor("FertilitySignal.Notes").WithErrorCode("Validation.Invalid");
            day.ShouldHaveValidationErrorFor("FertilitySignal.CervicalFluid").WithErrorCode("Validation.Invalid");
        } else {
            create.ShouldNotHaveAnyValidationErrors();
            factor.ShouldNotHaveAnyValidationErrors();
            day.ShouldNotHaveAnyValidationErrors();
        }
    }

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
    public async Task UpsertCycleDay_WithNullTagElement_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(symptoms: [new SymptomLogCommandModel(
                (int)CycleSymptomCategory.Pain, 3, [null!], Note: null, ClearNote: false)]));

        result.ShouldHaveValidationErrorFor("Symptoms[0].Tags[0]");
    }

    [Fact]
    public async Task UpsertCycleDay_WithTooManyTags_HasError() {
        string[] tags = [.. Enumerable.Range(0, CycleSymptomEntry.MaxTagsCount + 1)
            .Select(index => FormattableString.Invariant($"tag-{index}"))];
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(symptoms: [new SymptomLogCommandModel(
                (int)CycleSymptomCategory.Pain, 3, tags, Note: null, ClearNote: false)]));

        result.ShouldHaveValidationErrorFor("Symptoms[0].Tags");
    }

    [Fact]
    public async Task UpsertCycleDay_WithOversizedTag_HasError() {
        TestValidationResult<UpsertCycleDayCommand> result = await new UpsertCycleDayCommandValidator().TestValidateAsync(
            CreateDayCommand(symptoms: [new SymptomLogCommandModel(
                (int)CycleSymptomCategory.Pain,
                3,
                [new string('t', CycleSymptomEntry.MaxTagLength + 1)],
                Note: null,
                ClearNote: false)]));

        result.ShouldHaveValidationErrorFor("Symptoms[0].Tags[0]");
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

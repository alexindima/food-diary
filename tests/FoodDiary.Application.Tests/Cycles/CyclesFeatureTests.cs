using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Abstractions.Cycles.Models;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Commands.ClearCycleDay;
using FoodDiary.Application.Cycles.Commands.UpsertCycleFactor;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Queries.GetCycleNutritionSummary;
using FoodDiary.Application.Cycles.Queries.GetCurrentCycle;
using FoodDiary.Application.Cycles.Services;
using System.Reflection;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Cycles;

[ExcludeFromCodeCoverage]
public class CyclesFeatureTests {
    [Fact]
    public async Task CreateCycleCommandValidator_WithInvalidLength_Fails() {
        var validator = new CreateCycleCommandValidator();
        var command = new CreateCycleCommand(
            Guid.NewGuid(),
            DateTime.UtcNow,
            (int)CycleTrackingMode.PeriodTracking,
            AverageCycleLength: 10,
            AveragePeriodLength: 20,
            LutealLength: 20,
            IsRegular: false,
            IsOnboardingComplete: false,
            ShowFertilityEstimates: false,
            DiscreetNotifications: true,
            Notes: null);

        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task UpsertCycleDayCommandValidator_WithOutOfRangeSymptoms_Fails() {
        var validator = new UpsertCycleDayCommandValidator();
        var command = new UpsertCycleDayCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            Bleeding: null,
            Symptoms: [new SymptomLogCommandModel((int)CycleSymptomCategory.Pain, 11, [], Note: null, ClearNote: false)],
            FertilitySignal: null);

        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task UpsertCycleDayCommandHandler_WithEmptyCycleId_ReturnsValidationFailure() {
        var handler = new UpsertCycleDayCommandHandler(
            new NoopCycleRepository(),
            CreateCurrentUserAccessService(User.Create("cycle-empty@example.com", "hash")));

        Result<CycleLogDayModel> result = await handler.Handle(
            new UpsertCycleDayCommand(
                Guid.NewGuid(),
                Guid.Empty,
                DateTime.UtcNow,
                Bleeding: null,
                Symptoms: [],
                FertilitySignal: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("CycleProfileId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCycleCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-cycle@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);
        var handler = new CreateCycleCommandHandler(new NoopCycleRepository(), CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(CreateCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task CreateCycleCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new CreateCycleCommandHandler(
            new NoopCycleRepository(),
            CreateCurrentUserAccessService(User.Create("cycle-create-empty-user@example.com", "hash")));

        Result<CycleModel> result = await handler.Handle(CreateCommand(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateCycleCommandHandler_WhenCurrentProfileExists_UpdatesExistingProfile() {
        var user = User.Create("cycle-create-existing@example.com", "hash");
        var profile = CycleProfile.Create(
            user.Id,
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            notes: "old");
        var repository = new InMemoryCycleRepository(profile);
        var handler = new CreateCycleCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new CreateCycleCommand(
                user.Id.Value,
                new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
                (int)CycleTrackingMode.TryingToConceive,
                AverageCycleLength: 30,
                AveragePeriodLength: 4,
                LutealLength: 13,
                IsRegular: true,
                IsOnboardingComplete: true,
                ShowFertilityEstimates: true,
                DiscreetNotifications: false,
                Notes: " updated "),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(profile.Id.Value, result.Value.Id);
        Assert.Equal(CycleTrackingMode.TryingToConceive, result.Value.Mode);
        Assert.Equal(30, result.Value.AverageCycleLength);
        Assert.Equal("updated", result.Value.Notes);
        Assert.True(repository.WasUpdated);
    }

    [Fact]
    public async Task GetCurrentCycleQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("cycle-current-deleted@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);
        GetCurrentCycleQueryHandler handler = CreateCurrentCycleHandler(
            new NoopCycleRepository(),
            CreateCurrentUserAccessService(user));

        Result<CycleModel?> result = await handler.Handle(new GetCurrentCycleQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetCurrentCycleQueryHandler_WithEmptyUserId_ReturnsInvalidToken() {
        GetCurrentCycleQueryHandler handler = CreateCurrentCycleHandler(
            new NoopCycleRepository(),
            CreateCurrentUserAccessService(User.Create("cycle-current-empty@example.com", "hash")));

        Result<CycleModel?> result = await handler.Handle(new GetCurrentCycleQuery(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpsertCycleDayCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new UpsertCycleDayCommandHandler(
            new NoopCycleRepository(),
            CreateCurrentUserAccessService(User.Create("cycle-day-empty-user@example.com", "hash")));

        Result<CycleLogDayModel> result = await handler.Handle(
            new UpsertCycleDayCommand(
                Guid.Empty,
                Guid.NewGuid(),
                DateTime.UtcNow,
                Bleeding: null,
                Symptoms: [],
                FertilitySignal: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpsertCycleDayCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("cycle-day-deleted-user@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);
        var profile = CycleProfile.Create(user.Id, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpsertCycleDayCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<CycleLogDayModel> result = await handler.Handle(
            new UpsertCycleDayCommand(
                user.Id.Value,
                profile.Id.Value,
                DateTime.UtcNow,
                Bleeding: null,
                Symptoms: [],
                FertilitySignal: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.False(repository.WasUpdated);
    }

    [Fact]
    public async Task UpsertCycleDayCommandHandler_WhenProfileMissing_ReturnsNotFound() {
        var user = User.Create("cycle-day-missing@example.com", "hash");
        var handler = new UpsertCycleDayCommandHandler(new NoopCycleRepository(), CreateCurrentUserAccessService(user));

        Result<CycleLogDayModel> result = await handler.Handle(
            new UpsertCycleDayCommand(
                user.Id.Value,
                Guid.NewGuid(),
                DateTime.UtcNow,
                new BleedingLogCommandModel((int)BleedingType.Bleeding, (int)CycleFlowLevel.Medium, PainImpact: 2, Notes: null, ClearNotes: false),
                Symptoms: [],
                FertilitySignal: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Cycle.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task UpsertCycleDayCommandHandler_WithValidCommand_UpdatesProfileAndReturnsDay() {
        var user = User.Create("cycle-day-success@example.com", "hash");
        var profile = CycleProfile.Create(user.Id, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpsertCycleDayCommandHandler(repository, CreateCurrentUserAccessService(user));
        DateTime date = new(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);

        Result<CycleLogDayModel> result = await handler.Handle(
            new UpsertCycleDayCommand(
                user.Id.Value,
                profile.Id.Value,
                date,
                new BleedingLogCommandModel((int)BleedingType.Bleeding, (int)CycleFlowLevel.Medium, PainImpact: 3, Notes: "note", ClearNotes: false),
                [new SymptomLogCommandModel((int)CycleSymptomCategory.Craving, 7, ["sweet"], Note: null, ClearNote: false)],
                FertilitySignal: null),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(profile.Id.Value, result.Value.CycleProfileId);
        Assert.Single(result.Value.BleedingEntries);
        Assert.Single(result.Value.Symptoms);
        Assert.True(repository.WasUpdated);
    }

    [Fact]
    public async Task UpsertCycleDayCommandHandler_WithNoBleedingAndFertilitySignal_UpdatesProfileAndReturnsDay() {
        var user = User.Create("cycle-day-fertility@example.com", "hash");
        DateTime date = new(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc);
        var profile = CycleProfile.Create(user.Id, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpsertCycleDayCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<CycleLogDayModel> result = await handler.Handle(
            new UpsertCycleDayCommand(
                user.Id.Value,
                profile.Id.Value,
                date,
                Bleeding: null,
                Symptoms: [],
                new FertilitySignalCommandModel(
                    BasalBodyTemperatureCelsius: 36.62,
                    OvulationTestResult: (int)OvulationTestResult.Positive,
                    CervicalFluid: "egg white",
                    HadSex: true,
                    Notes: "peak",
                    ClearNotes: false)),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(result.Value.BleedingEntries);
        FertilitySignalModel signal = Assert.IsType<FertilitySignalModel>(result.Value.FertilitySignal);
        Assert.Equal(36.62, signal.BasalBodyTemperatureCelsius);
        Assert.Equal(OvulationTestResult.Positive, signal.OvulationTestResult);
        Assert.Equal("egg white", signal.CervicalFluid);
        Assert.True(signal.HadSex);
        Assert.Equal("peak", signal.Notes);
        Assert.True(repository.WasUpdated);
    }

    [Fact]
    public async Task ClearCycleDayCommandHandler_WithExistingDay_RemovesAllDayLogs() {
        var user = User.Create("cycle-day-clear@example.com", "hash");
        DateTime date = new(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);
        var profile = CycleProfile.Create(user.Id, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        profile.UpsertBleedingEntry(date, BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: 3, notes: "note");
        profile.UpsertSymptomEntry(date, CycleSymptomCategory.Craving, 7, ["sweet"], note: null);
        profile.UpsertFertilitySignal(date, 36.62, OvulationTestResult.Positive, "egg white", hadSex: true, notes: null);
        var repository = new InMemoryCycleRepository(profile);
        var handler = new ClearCycleDayCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(
            new ClearCycleDayCommand(user.Id.Value, profile.Id.Value, date),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(profile.BleedingEntries);
        Assert.Empty(profile.SymptomEntries);
        Assert.Empty(profile.FertilitySignals);
        Assert.True(repository.WasUpdated);
    }

    [Fact]
    public async Task ClearCycleDayCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new ClearCycleDayCommandHandler(
            new NoopCycleRepository(),
            CreateCurrentUserAccessService(User.Create("cycle-clear-empty-user@example.com", "hash")));

        Result result = await handler.Handle(
            new ClearCycleDayCommand(Guid.Empty, Guid.NewGuid(), DateTime.UtcNow),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ClearCycleDayCommandHandler_WithEmptyProfileId_ReturnsValidationFailure() {
        var user = User.Create("cycle-clear-empty-profile@example.com", "hash");
        var handler = new ClearCycleDayCommandHandler(new NoopCycleRepository(), CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(
            new ClearCycleDayCommand(user.Id.Value, Guid.Empty, DateTime.UtcNow),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task ClearCycleDayCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("cycle-clear-deleted-user@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);
        var profile = CycleProfile.Create(user.Id, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new ClearCycleDayCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(
            new ClearCycleDayCommand(user.Id.Value, profile.Id.Value, DateTime.UtcNow),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.False(repository.WasUpdated);
    }

    [Fact]
    public async Task ClearCycleDayCommandHandler_WhenProfileMissing_ReturnsNotFound() {
        var user = User.Create("cycle-clear-missing-profile@example.com", "hash");
        var handler = new ClearCycleDayCommandHandler(new NoopCycleRepository(), CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(
            new ClearCycleDayCommand(user.Id.Value, Guid.NewGuid(), DateTime.UtcNow),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Cycle.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task UpsertCycleFactorCommandHandler_WithInvalidType_ReturnsValidationFailure() {
        var user = User.Create("cycle-factor-invalid@example.com", "hash");
        var profile = CycleProfile.Create(user.Id, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        var handler = new UpsertCycleFactorCommandHandler(new InMemoryCycleRepository(profile), CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new UpsertCycleFactorCommand(
                user.Id.Value,
                profile.Id.Value,
                Type: 999,
                StartDate: DateTime.UtcNow,
                EndDate: null,
                Notes: null,
                ClearNotes: false),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task UpsertCycleFactorCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new UpsertCycleFactorCommandHandler(
            new NoopCycleRepository(),
            CreateCurrentUserAccessService(User.Create("cycle-factor-empty-user@example.com", "hash")));

        Result<CycleModel> result = await handler.Handle(
            new UpsertCycleFactorCommand(
                Guid.Empty,
                Guid.NewGuid(),
                (int)CycleFactorType.HormonalContraception,
                DateTime.UtcNow,
                EndDate: null,
                Notes: null,
                ClearNotes: false),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpsertCycleFactorCommandHandler_WithEmptyProfileId_ReturnsValidationFailure() {
        var user = User.Create("cycle-factor-empty-profile@example.com", "hash");
        var handler = new UpsertCycleFactorCommandHandler(new NoopCycleRepository(), CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new UpsertCycleFactorCommand(
                user.Id.Value,
                Guid.Empty,
                (int)CycleFactorType.HormonalContraception,
                DateTime.UtcNow,
                EndDate: null,
                Notes: null,
                ClearNotes: false),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task UpsertCycleFactorCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("cycle-factor-deleted-user@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);
        var profile = CycleProfile.Create(user.Id, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpsertCycleFactorCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new UpsertCycleFactorCommand(
                user.Id.Value,
                profile.Id.Value,
                (int)CycleFactorType.HormonalContraception,
                DateTime.UtcNow,
                EndDate: null,
                Notes: null,
                ClearNotes: false),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.False(repository.WasUpdated);
    }

    [Fact]
    public async Task UpsertCycleFactorCommandHandler_WhenProfileMissing_ReturnsNotFound() {
        var user = User.Create("cycle-factor-missing-profile@example.com", "hash");
        var handler = new UpsertCycleFactorCommandHandler(new NoopCycleRepository(), CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new UpsertCycleFactorCommand(
                user.Id.Value,
                Guid.NewGuid(),
                (int)CycleFactorType.HormonalContraception,
                DateTime.UtcNow,
                EndDate: null,
                Notes: null,
                ClearNotes: false),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Cycle.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task UpsertCycleFactorCommandHandler_WithValidCommand_UpdatesProfileAndReturnsCycle() {
        var user = User.Create("cycle-factor-success@example.com", "hash");
        var profile = CycleProfile.Create(user.Id, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        var repository = new InMemoryCycleRepository(profile);
        var handler = new UpsertCycleFactorCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result<CycleModel> result = await handler.Handle(
            new UpsertCycleFactorCommand(
                user.Id.Value,
                profile.Id.Value,
                (int)CycleFactorType.HormonalContraception,
                new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc),
                EndDate: null,
                Notes: "pill",
                ClearNotes: false),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Single(result.Value.Factors);
        Assert.Equal(CycleFactorType.HormonalContraception, result.Value.Factors.Single().Type);
        Assert.True(repository.WasUpdated);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithCycleLogsAndMeals_ReturnsBleedingComparison() {
        var user = User.Create("cycle-nutrition@example.com", "hash");
        DateTime startDate = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var profile = CycleProfile.Create(user.Id, startDate);
        profile.UpsertBleedingEntry(startDate, BleedingType.Bleeding, CycleFlowLevel.Heavy, painImpact: 8, notes: null);
        profile.UpsertSymptomEntry(startDate.AddDays(1), CycleSymptomCategory.Craving, 6, ["sweet"], note: null);
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new InMemoryCycleRepository(profile),
            CreateStatisticsReadService([
                CreateNutritionBucket(startDate, calories: 2100, fiber: 18),
                CreateNutritionBucket(startDate.AddDays(1), calories: 1800, fiber: 28),
            ]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, startDate, startDate.AddDays(2)),
            CancellationToken.None);

        ResultAssert.Success(result);
        CycleNutritionSummaryModel summary = Assert.IsType<CycleNutritionSummaryModel>(result.Value);
        Assert.Equal(2, summary.LoggedCycleDays);
        Assert.Equal(2, summary.DaysWithMeals);
        Assert.Equal(1, summary.BleedingDays);
        Assert.Equal(2100, summary.AverageCaloriesOnBleedingDays);
        Assert.Equal(1800, summary.AverageCaloriesOnNonBleedingCycleDays);
        Assert.Equal(18, summary.AverageFiberOnBleedingDays);
        Assert.Equal(28, summary.AverageFiberOnNonBleedingCycleDays);
        Assert.Equal(8, summary.AveragePainImpactOnDaysWithMeals);
        Assert.False(summary.HasEnoughNutritionData);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithEnoughGroupData_MarksSummaryReliable() {
        var user = User.Create("cycle-nutrition-enough@example.com", "hash");
        DateTime startDate = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var profile = CycleProfile.Create(user.Id, startDate);
        profile.UpsertBleedingEntry(startDate, BleedingType.Bleeding, CycleFlowLevel.Heavy, painImpact: 8, notes: null);
        profile.UpsertBleedingEntry(startDate.AddDays(1), BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: 6, notes: null);
        profile.UpsertSymptomEntry(startDate.AddDays(2), CycleSymptomCategory.Craving, 4, [], note: null);
        profile.UpsertSymptomEntry(startDate.AddDays(3), CycleSymptomCategory.Energy, 5, [], note: null);
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new InMemoryCycleRepository(profile),
            CreateStatisticsReadService([
                CreateNutritionBucket(startDate, calories: 2100, fiber: 18),
                CreateNutritionBucket(startDate.AddDays(1), calories: 2000, fiber: 20),
                CreateNutritionBucket(startDate.AddDays(2), calories: 1800, fiber: 28),
                CreateNutritionBucket(startDate.AddDays(3), calories: 1900, fiber: 26),
            ]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, startDate, startDate.AddDays(4)),
            CancellationToken.None);

        ResultAssert.Success(result);
        CycleNutritionSummaryModel summary = Assert.IsType<CycleNutritionSummaryModel>(result.Value);
        Assert.True(summary.HasEnoughNutritionData);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithMissingCycle_ReturnsNull() {
        var user = User.Create("cycle-nutrition-missing@example.com", "hash");
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new NoopCycleRepository(),
            CreateStatisticsReadService([]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithEmptyUserId_ReturnsInvalidToken() {
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new NoopCycleRepository(),
            CreateStatisticsReadService([]),
            CreateCurrentUserAccessService(User.Create("cycle-nutrition-empty-user@example.com", "hash")));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(Guid.Empty, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithInvertedDates_ReturnsValidationFailure() {
        var user = User.Create("cycle-nutrition-inverted@example.com", "hash");
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new NoopCycleRepository(),
            CreateStatisticsReadService([]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, DateTime.UtcNow, DateTime.UtcNow.AddDays(-1)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("DateFrom", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithTooLargeRange_ReturnsValidationFailure() {
        var user = User.Create("cycle-nutrition-long-range@example.com", "hash");
        DateTime from = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new NoopCycleRepository(),
            CreateStatisticsReadService([]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, from, from.AddDays(367)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("one year", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("cycle-nutrition-deleted@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new NoopCycleRepository(),
            CreateStatisticsReadService([]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithFertilitySignalOnly_IncludesLoggedDay() {
        var user = User.Create("cycle-nutrition-fertility@example.com", "hash");
        DateTime startDate = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var profile = CycleProfile.Create(user.Id, startDate);
        profile.UpsertFertilitySignal(startDate.AddDays(1), 36.62, OvulationTestResult.Positive, "egg white", hadSex: true, notes: null);
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new InMemoryCycleRepository(profile),
            CreateStatisticsReadService([CreateNutritionBucket(startDate.AddDays(1), calories: 1900, fiber: 22)]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, startDate, startDate.AddDays(2)),
            CancellationToken.None);

        ResultAssert.Success(result);
        CycleNutritionSummaryModel summary = Assert.IsType<CycleNutritionSummaryModel>(result.Value);
        Assert.Equal(1, summary.LoggedCycleDays);
        Assert.Equal(1, summary.DaysWithMeals);
        Assert.Equal(0, summary.BleedingDays);
        Assert.Equal(1900, summary.AverageCaloriesOnNonBleedingCycleDays);
    }

    [Fact]
    public void CycleMappings_ToModel_SortsLogsByDate() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow);
        profile.UpsertBleedingEntry(new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), BleedingType.Bleeding, CycleFlowLevel.Light, painImpact: null, notes: null);
        profile.UpsertBleedingEntry(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), BleedingType.Bleeding, CycleFlowLevel.Heavy, painImpact: null, notes: null);
        profile.UpsertSymptomEntry(new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc), CycleSymptomCategory.Pain, 4, [], note: null);
        profile.UpsertSymptomEntry(new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc), CycleSymptomCategory.Craving, 6, [], note: null);
        profile.UpsertFertilitySignal(new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc), 36.7, OvulationTestResult.Positive, "egg white", hadSex: true, notes: null);
        profile.UpsertFertilitySignal(new DateTime(2026, 2, 4, 0, 0, 0, DateTimeKind.Utc), 36.5, OvulationTestResult.Negative, "sticky", hadSex: false, notes: null);

        CycleModel response = profile.ToModel();

        Assert.Equal(
            [new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)],
            response.BleedingEntries.Select(day => day.Date));
        Assert.Equal(
            [new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc)],
            response.Symptoms.Select(day => day.Date));
        Assert.Equal(
            [new DateTime(2026, 2, 4, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc)],
            response.FertilitySignals.Select(day => day.Date));
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_ReturnsRangeAndConfidence() {
        var profile = CycleProfile.Create(UserId.New(), new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), showFertilityEstimates: true);

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile);

        Assert.NotNull(predictions.NextPeriodStartFrom);
        Assert.NotNull(predictions.NextPeriodStartTo);
        Assert.NotNull(predictions.OvulationFrom);
        Assert.Equal("Learning", predictions.Confidence);
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_WithActivePredictionLimitingFactor_ReturnsLimitedPrediction() {
        var profile = CycleProfile.Create(UserId.New(), new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), showFertilityEstimates: true);
        profile.UpsertFactor(
            CycleFactorType.HormonalContraception,
            new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc),
            endDate: null,
            notes: null);

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile);

        Assert.Null(predictions.NextPeriodStartFrom);
        Assert.Null(predictions.NextPeriodStartTo);
        Assert.Null(predictions.OvulationFrom);
        Assert.Null(predictions.OvulationTo);
        Assert.Equal("Predictions are limited by the active tracking mode.", predictions.Rationale);
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_WithEndedPredictionLimitingFactor_ReturnsRange() {
        var profile = CycleProfile.Create(UserId.New(), new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), showFertilityEstimates: true);
        profile.UpsertFactor(
            CycleFactorType.HormonalContraception,
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
            notes: null);

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile);

        Assert.NotNull(predictions.NextPeriodStartFrom);
        Assert.NotNull(predictions.OvulationFrom);
    }

    [Theory]
    [InlineData(CycleConfidence.High, 1)]
    [InlineData(CycleConfidence.Medium, 2)]
    [InlineData(CycleConfidence.Low, 4)]
    [InlineData(CycleConfidence.Learning, 7)]
    public void CyclePredictionService_CalculatePredictions_UsesConfidenceWindow(CycleConfidence confidence, int expectedWindowDays) {
        DateTime trackingStart = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var profile = CycleProfile.Create(UserId.New(), trackingStart, showFertilityEstimates: true);
        SetPrivateProperty(profile, nameof(CycleProfile.Confidence), confidence);

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile);

        DateTime expectedNextPeriodStart = trackingStart.AddDays(profile.AverageCycleLength);
        Assert.Equal(expectedNextPeriodStart.AddDays(-expectedWindowDays), predictions.NextPeriodStartFrom);
        Assert.Equal(expectedNextPeriodStart.AddDays(expectedWindowDays), predictions.NextPeriodStartTo);
    }

    [Fact]
    public void FertilitySignalModel_ConstructsExpectedValues() {
        var id = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var date = new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc);

        var model = new FertilitySignalModel(
            id,
            profileId,
            date,
            BasalBodyTemperatureCelsius: 36.62,
            OvulationTestResult.Positive,
            CervicalFluid: "egg white",
            HadSex: true,
            Notes: "peak");

        Assert.Equal(id, model.Id);
        Assert.Equal(profileId, model.CycleProfileId);
        Assert.Equal(date, model.Date);
        Assert.Equal(36.62, model.BasalBodyTemperatureCelsius);
        Assert.Equal(OvulationTestResult.Positive, model.OvulationTestResult);
        Assert.Equal("egg white", model.CervicalFluid);
        Assert.True(model.HadSex);
        Assert.Equal("peak", model.Notes);
    }

    [Fact]
    public void FertilitySignalCommandModel_ConstructsExpectedValues() {
        var model = new FertilitySignalCommandModel(
            BasalBodyTemperatureCelsius: 36.62,
            OvulationTestResult: (int)FoodDiary.Domain.Enums.OvulationTestResult.Positive,
            CervicalFluid: "egg white",
            HadSex: true,
            Notes: "peak",
            ClearNotes: false);

        Assert.Equal(36.62, model.BasalBodyTemperatureCelsius);
        Assert.Equal((int)FoodDiary.Domain.Enums.OvulationTestResult.Positive, model.OvulationTestResult);
        Assert.Equal("egg white", model.CervicalFluid);
        Assert.True(model.HadSex);
        Assert.Equal("peak", model.Notes);
        Assert.False(model.ClearNotes);
    }

    private static CreateCycleCommand CreateCommand(Guid userId) =>
        new(
            userId,
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            (int)CycleTrackingMode.PeriodTracking,
            AverageCycleLength: 28,
            AveragePeriodLength: 5,
            LutealLength: 14,
            IsRegular: false,
            IsOnboardingComplete: false,
            ShowFertilityEstimates: false,
            DiscreetNotifications: true,
            Notes: null);

    private static DashboardStatisticsBucketReadModel CreateNutritionBucket(DateTime date, double calories, double fiber) =>
        new(date, date, calories, AverageProteins: 0, AverageFats: 0, AverageCarbs: 0, AverageFiber: fiber, TotalFiber: fiber);

    private static GetCurrentCycleQueryHandler CreateCurrentCycleHandler(
        ICycleReadRepository cycleRepository,
        ICurrentUserAccessService currentUserAccessService) =>
        new(new CycleReadService(cycleRepository, CreateStatisticsReadService([])), currentUserAccessService);

    private static GetCycleNutritionSummaryQueryHandler CreateCycleNutritionSummaryHandler(
        ICycleReadRepository cycleRepository,
        IDashboardStatisticsReadService statisticsReadService,
        ICurrentUserAccessService currentUserAccessService) =>
        new(new CycleReadService(cycleRepository, statisticsReadService), currentUserAccessService);

    private static void SetPrivateProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value) {
        PropertyInfo? property = typeof(TTarget).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        property!.SetValue(target, value);
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoopCycleRepository : ICycleRepository {
        public Task<CycleProfile> AddAsync(CycleProfile profile, CancellationToken cancellationToken = default) => Task.FromResult(profile);

        public Task UpdateAsync(CycleProfile profile, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<CycleProfile?> GetByIdAsync(CycleProfileId id, UserId userId, bool includeDetails = false, bool asTracking = false, CancellationToken cancellationToken = default) => Task.FromResult<CycleProfile?>(null);

        public Task<CycleProfile?> GetCurrentAsync(UserId userId, bool includeDetails = false, CancellationToken cancellationToken = default) => Task.FromResult<CycleProfile?>(null);

        public Task<CycleProfileReadModel?> GetCurrentReadModelAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CycleProfileReadModel?>(null);

        public Task<IReadOnlyList<CycleProfile>> GetByUserAsync(UserId userId, bool includeDetails = false, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CycleProfile>>([]);
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryCycleRepository(CycleProfile profile) : ICycleRepository {
        public bool WasUpdated { get; private set; }

        public Task<CycleProfile> AddAsync(CycleProfile profile, CancellationToken cancellationToken = default) => Task.FromResult(profile);

        public Task UpdateAsync(CycleProfile profile, CancellationToken cancellationToken = default) {
            WasUpdated = true;
            return Task.CompletedTask;
        }

        public Task<CycleProfile?> GetByIdAsync(CycleProfileId id, UserId userId, bool includeDetails = false, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(profile.Id == id && profile.UserId == userId ? profile : null);

        public Task<CycleProfile?> GetCurrentAsync(UserId userId, bool includeDetails = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(profile.UserId == userId ? profile : null);

        public Task<CycleProfileReadModel?> GetCurrentReadModelAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(profile.UserId == userId ? ToReadModel(profile) : null);

        public Task<IReadOnlyList<CycleProfile>> GetByUserAsync(UserId userId, bool includeDetails = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CycleProfile>>(profile.UserId == userId ? [profile] : []);
        private static CycleProfileReadModel ToReadModel(CycleProfile profile) =>
            new(
                profile.Id.Value,
                profile.UserId.Value,
                profile.Mode,
                profile.Confidence,
                profile.TrackingStartDate,
                profile.AverageCycleLength,
                profile.AveragePeriodLength,
                profile.LutealLength,
                profile.IsRegular,
                profile.IsOnboardingComplete,
                profile.ShowFertilityEstimates,
                profile.DiscreetNotifications,
                profile.Notes,
                [.. profile.BleedingEntries.Select(static entry => new BleedingEntryReadModel(
                    entry.Id.Value,
                    entry.CycleProfileId.Value,
                    entry.Date,
                    entry.Type,
                    entry.Flow,
                    entry.PainImpact,
                    entry.Notes))],
                [.. profile.SymptomEntries.Select(static entry => new CycleSymptomEntryReadModel(
                    entry.Id.Value,
                    entry.CycleProfileId.Value,
                    entry.Date,
                    entry.Category,
                    entry.Intensity,
                    entry.Tags,
                    entry.Note))],
                [.. profile.Factors.Select(static factor => new CycleFactorReadModel(
                    factor.Id.Value,
                    factor.CycleProfileId.Value,
                    factor.Type,
                    factor.StartDate,
                    factor.EndDate,
                    factor.Notes))],
                [.. profile.FertilitySignals.Select(static signal => new FertilitySignalReadModel(
                    signal.Id.Value,
                    signal.CycleProfileId.Value,
                    signal.Date,
                    signal.BasalBodyTemperatureCelsius,
                    signal.OvulationTestResult,
                    signal.CervicalFluid,
                    signal.HadSex,
                    signal.Notes))]);
    }

    private static IDashboardStatisticsReadService CreateStatisticsReadService(IReadOnlyList<DashboardStatisticsBucketReadModel> buckets) {
        IDashboardStatisticsReadService service = Substitute.For<IDashboardStatisticsReadService>();
        service
            .GetStatisticsAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(buckets)));
        return service;
    }

    private static ICurrentUserAccessService CreateCurrentUserAccessService(User? user) {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        service
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId userId = call.Arg<UserId>();
                Error? error = user switch {
                    null => Errors.Authentication.InvalidToken,
                    { Id: var id } when id != userId => Errors.Authentication.InvalidToken,
                    { DeletedAt: not null } => Errors.Authentication.AccountDeleted,
                    _ => null,
                };
                return Task.FromResult(error);
            });

        return service;
    }
}

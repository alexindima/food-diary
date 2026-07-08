using FoodDiary.Results;
using FoodDiary.Application.Cycles.Commands.ClearCycleDay;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Tests.Cycles;

public partial class CyclesFeatureTests {

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
}

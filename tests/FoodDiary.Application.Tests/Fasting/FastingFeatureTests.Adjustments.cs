using FoodDiary.Results;
using FoodDiary.Application.Fasting.Commands.ExtendActiveFasting;
using FoodDiary.Application.Fasting.Commands.ReduceActiveFastingTarget;
using FoodDiary.Application.Fasting.Commands.UpdateCurrentFastingCheckIn;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Fasting;

public partial class FastingFeatureTests {
    [Fact]
    public async Task ExtendActiveFasting_WhenSessionIsActive_Succeeds() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateExtended(userId, FastingProtocol.F72_0, 72, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 72);
        var handler = new ExtendActiveFastingCommandHandler(new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId));

        Result<FastingSessionModel> result = await handler.Handle(
            new ExtendActiveFastingCommand(userId.Value, 24), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(72, result.Value.InitialPlannedDurationHours);
        Assert.Equal(24, result.Value.AddedDurationHours);
        Assert.Equal(96, result.Value.PlannedDurationHours);
    }

    [Fact]
    public async Task ExtendActiveFasting_WhenNoActiveSession_ReturnsFailure() {
        var userId = UserId.New();
        var handler = new ExtendActiveFastingCommandHandler(new InMemoryFastingPlanRepository(),
            new InMemoryFastingOccurrenceRepository(),
            CreateCurrentUserAccessService(userId));

        Result<FastingSessionModel> result = await handler.Handle(
            new ExtendActiveFastingCommand(userId.Value, 24), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("NoActiveSession", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtendActiveFasting_WithInvalidAdditionalHours_ReturnsValidationFailure() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateExtended(userId, FastingProtocol.F72_0, 72, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 72);
        var handler = new ExtendActiveFastingCommandHandler(new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId));

        Result<FastingSessionModel> result = await handler.Handle(new ExtendActiveFastingCommand(userId.Value, 0), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Equal(72, occurrence.TargetHours);
    }

    [Fact]
    public async Task ExtendActiveFasting_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new ExtendActiveFastingCommandHandler(new InMemoryFastingPlanRepository(),
            new InMemoryFastingOccurrenceRepository(),
            CreateCurrentUserAccessService(user: null));

        Result<FastingSessionModel> result = await handler.Handle(
            new ExtendActiveFastingCommand(Guid.Empty, 24), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ExtendActiveFasting_WhenCurrentPlanCannotBeLoaded_ReturnsNoActiveSession() {
        var userId = UserId.New();
        var occurrence = FastingOccurrence.Create(FastingPlanId.New(), userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 72);
        var handler = new ExtendActiveFastingCommandHandler(new InMemoryFastingPlanRepository(),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId));

        Result<FastingSessionModel> result = await handler.Handle(
            new ExtendActiveFastingCommand(userId.Value, 24), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("NoActiveSession", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtendActiveFasting_WhenCurrentOccurrenceCannotBeExtended_ReturnsNoActiveSession() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateExtended(userId, FastingProtocol.F72_0, 72, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 72);
        occurrence.Complete(FixedNow.AddHours(1));
        var handler = new ExtendActiveFastingCommandHandler(new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId));

        Result<FastingSessionModel> result = await handler.Handle(
            new ExtendActiveFastingCommand(userId.Value, 24), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("NoActiveSession", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtendActiveFasting_WhenOccurrenceHasNoTarget_ReturnsNoActiveSession() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateExtended(userId, FastingProtocol.F72_0, 72, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, targetHours: null);
        var handler = new ExtendActiveFastingCommandHandler(new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId));

        Result<FastingSessionModel> result = await handler.Handle(
            new ExtendActiveFastingCommand(userId.Value, 24), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("NoActiveSession", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtendActiveFasting_WithDeletedUser_ReturnsAccountDeleted() {
        User user = CreateUser(UserId.New());
        user.DeleteAccount(FixedNow);
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F72_0, 72, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow, 1, 72);
        var handler = new ExtendActiveFastingCommandHandler(new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(user));

        Result<FastingSessionModel> result = await handler.Handle(
            new ExtendActiveFastingCommand(user.Id.Value, 24), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Equal(72, occurrence.TargetHours);
    }

    [Fact]
    public async Task ExtendActiveFasting_WithIntermittentPlan_ReturnsValidationFailure() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateIntermittent(userId, FastingProtocol.F16_8, 16, 8, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, FixedNow, 1, 16);
        var handler = new ExtendActiveFastingCommandHandler(new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId));

        Result<FastingSessionModel> result = await handler.Handle(
            new ExtendActiveFastingCommand(userId.Value, 4),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Only extended fasting", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReduceActiveFastingTarget_WhenSessionIsActive_Succeeds() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateExtended(userId, FastingProtocol.F72_0, 72, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 72);
        var handler = new ReduceActiveFastingTargetCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new ReduceActiveFastingTargetCommand(userId.Value, 8), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(72, result.Value.InitialPlannedDurationHours);
        Assert.Equal(-8, result.Value.AddedDurationHours);
        Assert.Equal(64, result.Value.PlannedDurationHours);
        Assert.Equal("Active", result.Value.Status);
    }

    [Fact]
    public async Task ReduceActiveFastingTarget_WhenNewTargetAlreadyReached_CompletesSession() {
        var userId = UserId.New();
        DateTime now = FixedNow;
        var plan = FastingPlan.CreateExtended(userId, FastingProtocol.F36_0, 36, now.AddHours(-30));
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, now.AddHours(-30), 1, 36);
        var handler = new ReduceActiveFastingTargetCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new ReduceActiveFastingTargetCommand(userId.Value, 8), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("Completed", result.Value.Status);
        Assert.NotNull(result.Value.EndedAtUtc);
        Assert.Equal(FastingPlanStatus.Stopped, plan.Status);
    }

    [Fact]
    public async Task ReduceActiveFastingTarget_WithDeletedUser_ReturnsAccountDeleted() {
        User user = CreateUser(UserId.New());
        user.DeleteAccount(FixedNow);
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F72_0, 72, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow, 1, 72);
        var handler = new ReduceActiveFastingTargetCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new ReduceActiveFastingTargetCommand(user.Id.Value, 8), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Equal(72, occurrence.TargetHours);
    }

    [Fact]
    public async Task ReduceActiveFastingTarget_WhenNoCurrentOccurrence_ReturnsNoActiveSession() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateExtended(userId, FastingProtocol.F72_0, 72, FixedNow);
        var handler = new ReduceActiveFastingTargetCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(new ReduceActiveFastingTargetCommand(userId.Value, 8), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Fasting.NoActiveSession", result.Error.Code);
    }

    [Fact]
    public async Task ReduceActiveFastingTarget_WithIntermittentPlan_ReturnsValidationFailure() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateIntermittent(userId, FastingProtocol.F16_8, 16, 8, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, FixedNow, 1, 16);
        var handler = new ReduceActiveFastingTargetCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new ReduceActiveFastingTargetCommand(userId.Value, 12),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Only extended fasting target", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReduceActiveFastingTarget_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new ReduceActiveFastingTargetCommandHandler(
            new InMemoryFastingPlanRepository(),
            new InMemoryFastingOccurrenceRepository(),
            CreateCurrentUserAccessService(user: null),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new ReduceActiveFastingTargetCommand(Guid.Empty, 8), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ReduceActiveFastingTarget_WhenCurrentPlanCannotBeLoaded_ReturnsNoActiveSession() {
        var userId = UserId.New();
        var occurrence = FastingOccurrence.Create(FastingPlanId.New(), userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 72);
        var handler = new ReduceActiveFastingTargetCommandHandler(
            new InMemoryFastingPlanRepository(),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new ReduceActiveFastingTargetCommand(userId.Value, 8), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("NoActiveSession", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReduceActiveFastingTarget_WithInvalidReducedHours_ReturnsValidationFailure() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateExtended(userId, FastingProtocol.F72_0, 72, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 72);
        var handler = new ReduceActiveFastingTargetCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new ReduceActiveFastingTargetCommand(userId.Value, 0), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Reduced fasting hours", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReduceActiveFastingTarget_WhenOccurrenceHasNoTarget_ReturnsNoActiveSession() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateExtended(userId, FastingProtocol.F72_0, 72, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, targetHours: null);
        var handler = new ReduceActiveFastingTargetCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new ReduceActiveFastingTargetCommand(userId.Value, 8), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("NoActiveSession", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateCurrentFastingCheckIn_WithDeletedUser_ReturnsAccountDeleted() {
        User user = CreateUser(UserId.New());
        user.DeleteAccount(FixedNow);
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F72_0, 72, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow, 1, 72);
        var checkInRepo = new InMemoryFastingCheckInRepository();
        var handler = new UpdateCurrentFastingCheckInCommandHandler(
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            checkInRepo,
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new UpdateCurrentFastingCheckInCommand(user.Id.Value, 3, 3, 3, ["good"], "steady"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Null(occurrence.CheckInAtUtc);
        Assert.Empty(checkInRepo.Stored);
    }

    [Fact]
    public async Task UpdateCurrentFastingCheckIn_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new UpdateCurrentFastingCheckInCommandHandler(
            new InMemoryFastingOccurrenceRepository(),
            new InMemoryFastingCheckInRepository(),
            CreateCurrentUserAccessService(user: null),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new UpdateCurrentFastingCheckInCommand(UserId: null, 3, 3, 3, ["good"], "steady"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateCurrentFastingCheckIn_WhenNoActiveSession_ReturnsFailure() {
        User user = CreateUser(UserId.New());
        var checkInRepo = new InMemoryFastingCheckInRepository();
        var handler = new UpdateCurrentFastingCheckInCommandHandler(
            new InMemoryFastingOccurrenceRepository(),
            checkInRepo,
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new UpdateCurrentFastingCheckInCommand(user.Id.Value, 3, 3, 3, ["good"], "steady"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Fasting.NoActiveSession", result.Error.Code);
        Assert.Empty(checkInRepo.Stored);
    }

    [Fact]
    public async Task UpdateCurrentFastingCheckIn_WithInvalidLevels_ReturnsValidationFailure() {
        User user = CreateUser(UserId.New());
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddHours(-1));
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-1), 1, 36);
        var checkInRepo = new InMemoryFastingCheckInRepository();
        var handler = new UpdateCurrentFastingCheckInCommandHandler(
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            checkInRepo,
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new UpdateCurrentFastingCheckInCommand(user.Id.Value, 0, 3, 3, ["good"], "steady"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Empty(checkInRepo.Stored);
        Assert.Null(occurrence.CheckInAtUtc);
    }

    [Fact]
    public async Task UpdateCurrentFastingCheckIn_WithActiveSession_AddsCheckInAndUpdatesSession() {
        User user = CreateUser(UserId.New());
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddHours(-1));
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-1), 1, 36);
        AttachNavigation(occurrence, plan, user);
        var checkInRepo = new InMemoryFastingCheckInRepository();
        var handler = new UpdateCurrentFastingCheckInCommandHandler(
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            checkInRepo,
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new UpdateCurrentFastingCheckInCommand(user.Id.Value, 4, 5, 3, ["tired", "focused"], "steady"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(FixedNow, occurrence.CheckInAtUtc);
        Assert.Single(checkInRepo.Stored);
        Assert.Equal(4, result.Value.HungerLevel);
        Assert.Equal(5, result.Value.EnergyLevel);
        Assert.Equal(["tired", "focused"], result.Value.Symptoms);
        Assert.Single(result.Value.CheckIns);
    }

}

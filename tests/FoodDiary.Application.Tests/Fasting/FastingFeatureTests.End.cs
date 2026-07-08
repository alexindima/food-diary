using FoodDiary.Results;
using FoodDiary.Application.Fasting.Commands.EndFasting;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Fasting;

public partial class FastingFeatureTests {
    [Fact]
    public async Task EndFasting_WhenActiveSession_Succeeds() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateIntermittent(userId, FastingProtocol.F16_8, 16, 8, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, FixedNow, 1, 16);
        var handler = new EndFastingCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new EndFastingCommand(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(result.Value.IsCompleted);
        Assert.Equal("Completed", result.Value.Status);
    }

    [Fact]
    public async Task EndCyclicFastDay_StopsPlanAndInterruptsCurrentOccurrence() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateCyclic(userId, 1, 3, 16, 8, FixedNow, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 24);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(occurrence);
        var handler = new EndFastingCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            occurrenceRepo,
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new EndFastingCommand(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("FastDay", result.Value.OccurrenceKind);
        Assert.Equal("Interrupted", result.Value.Status);
        Assert.NotNull(result.Value.EndedAtUtc);
        Assert.Equal(FastingPlanStatus.Stopped, plan.Status);
        Assert.DoesNotContain(occurrenceRepo.StoredOccurrences, x => x.Kind == FastingOccurrenceKind.EatDay && x.Status == FastingOccurrenceStatus.Active);
    }

    [Fact]
    public async Task EndCyclicFastDay_InMultiDayFastBlock_StopsPlanAndInterruptsCurrentOccurrence() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateCyclic(userId, 3, 1, 16, 8, FixedNow, FixedNow);
        var first = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow.AddDays(-1), 1, 24);
        first.Complete(FixedNow.AddHours(-12));
        var current = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 2, 24);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current);
        occurrenceRepo.StoredOccurrences.Insert(0, first);
        var handler = new EndFastingCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            occurrenceRepo,
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new EndFastingCommand(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("FastDay", result.Value.OccurrenceKind);
        Assert.Equal("Interrupted", result.Value.Status);
        Assert.NotNull(result.Value.EndedAtUtc);
        Assert.Equal(FastingPlanStatus.Stopped, plan.Status);
    }

    [Fact]
    public async Task EndExtendedFasting_BeforeTarget_ReturnsInterruptedStatus() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateExtended(userId, FastingProtocol.F72_0, 72, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 72);
        var handler = new EndFastingCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new EndFastingCommand(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(result.Value.IsCompleted);
        Assert.Equal("Interrupted", result.Value.Status);
    }

    [Fact]
    public async Task EndFasting_WhenNoActiveSession_ReturnsFailure() {
        var userId = UserId.New();
        var handler = new EndFastingCommandHandler(
            new InMemoryFastingPlanRepository(),
            new InMemoryFastingOccurrenceRepository(),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new EndFastingCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("NoActiveSession", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EndFasting_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new EndFastingCommandHandler(
            new InMemoryFastingPlanRepository(),
            new InMemoryFastingOccurrenceRepository(),
            CreateCurrentUserAccessService(user: null),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(new EndFastingCommand(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task EndFasting_WhenCurrentPlanCannotBeLoaded_ReturnsNoActiveSession() {
        var userId = UserId.New();
        var occurrence = FastingOccurrence.Create(FastingPlanId.New(), userId, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-40), 1, 36);
        var handler = new EndFastingCommandHandler(
            new InMemoryFastingPlanRepository(),
            new InMemoryFastingOccurrenceRepository(occurrence),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(new EndFastingCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Fasting.NoActiveSession", result.Error.Code);
        Assert.Equal(FastingOccurrenceStatus.Active, occurrence.Status);
    }

    [Fact]
    public async Task EndExtendedFasting_AfterTarget_ReturnsCompletedStatus() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateExtended(userId, FastingProtocol.F36_0, 36, FixedNow.AddHours(-40));
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-40), 1, 36);
        var handler = new EndFastingCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(new EndFastingCommand(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("Completed", result.Value.Status);
        Assert.Equal(FastingPlanStatus.Stopped, plan.Status);
    }

    [Fact]
    public async Task EndFasting_WithDeletedUser_ReturnsAccountDeleted() {
        User user = CreateUser(UserId.New());
        user.DeleteAccount(FixedNow);
        var plan = FastingPlan.CreateIntermittent(user.Id, FastingProtocol.F16_8, 16, 8, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastingWindow, FixedNow, 1, 16);
        var handler = new EndFastingCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new EndFastingCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Equal(FastingOccurrenceStatus.Active, occurrence.Status);
    }

}

using FoodDiary.Results;
using FoodDiary.Application.Fasting.Commands.PostponeCyclicDay;
using FoodDiary.Application.Fasting.Commands.SkipCyclicDay;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Fasting;

public partial class FastingFeatureTests {
    [Fact]
    public async Task SkipCyclicDay_WhenActiveFastDay_CreatesEatDayOccurrence() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateCyclic(userId, 1, 3, 16, 8, FixedNow, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 24);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current: occurrence);
        var handler = new SkipCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            occurrenceRepo,
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new SkipCyclicDayCommand(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("EatDay", result.Value.OccurrenceKind);
        Assert.Equal(1, result.Value.CyclicPhaseDayNumber);
        Assert.Equal(3, result.Value.CyclicPhaseDayTotal);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal("Skipped", occurrence.Status.ToString());
        Assert.Contains(occurrenceRepo.StoredOccurrences, x => x.Kind == FastingOccurrenceKind.EatDay);
    }

    [Fact]
    public async Task SkipCyclicDay_WhenActiveEatDay_StartsFastPhaseFromFirstDay() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateCyclic(userId, 10, 10, 16, 8, FixedNow, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.EatDay, FixedNow, 15, 8);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current: occurrence);
        var handler = new SkipCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            occurrenceRepo,
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new SkipCyclicDayCommand(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("FastDay", result.Value.OccurrenceKind);
        Assert.Equal(1, result.Value.CyclicPhaseDayNumber);
        Assert.Equal(10, result.Value.CyclicPhaseDayTotal);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal("Skipped", occurrence.Status.ToString());
        Assert.Contains(occurrenceRepo.StoredOccurrences, x => x.Kind == FastingOccurrenceKind.FastDay && x.SequenceNumber == 21);
    }

    [Fact]
    public async Task SkipCyclicDay_WithDeletedUser_ReturnsAccountDeleted() {
        User user = CreateUser(UserId.New());
        user.DeleteAccount(FixedNow);
        var plan = FastingPlan.CreateCyclic(user.Id, 1, 3, 16, 8, FixedNow, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow, 1, 24);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current: occurrence);
        var handler = new SkipCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            occurrenceRepo,
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new SkipCyclicDayCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Equal(FastingOccurrenceStatus.Active, occurrence.Status);
        Assert.Single(occurrenceRepo.StoredOccurrences);
    }

    [Fact]
    public async Task SkipCyclicDay_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new SkipCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(),
            new InMemoryFastingOccurrenceRepository(),
            CreateCurrentUserAccessService(UserId.New()),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(new SkipCyclicDayCommand(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task SkipCyclicDay_WhenNoCurrentOccurrence_ReturnsNoActiveSession() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateCyclic(userId, 1, 3, 16, 8, FixedNow, FixedNow);
        var handler = new SkipCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(new SkipCyclicDayCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Fasting.NoActiveSession", result.Error.Code);
    }

    [Fact]
    public async Task SkipCyclicDay_WhenCurrentPlanCannotBeLoaded_ReturnsNoActiveSession() {
        var userId = UserId.New();
        var occurrence = FastingOccurrence.Create(FastingPlanId.New(), userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 24);
        var handler = new SkipCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(new SkipCyclicDayCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Fasting.NoActiveSession", result.Error.Code);
    }

    [Fact]
    public async Task SkipCyclicDay_WithIntermittentPlan_ReturnsInvalidCyclicAction() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateIntermittent(userId, FastingProtocol.F16_8, 16, 8, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, FixedNow, 1, 16);
        var handler = new SkipCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(new SkipCyclicDayCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Fasting.InvalidCyclicAction", result.Error.Code);
    }

    [Fact]
    public async Task SkipCyclicDay_WhenOccurrenceCannotBeSkipped_ReturnsInvalidCyclicAction() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateCyclic(userId, 1, 3, 16, 8, FixedNow, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 24);
        occurrence.Complete(FixedNow.AddHours(1));
        var handler = new SkipCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new PassthroughCurrentFastingOccurrenceRepository(occurrence),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(new SkipCyclicDayCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Fasting.InvalidCyclicAction", result.Error.Code);
        Assert.Contains("cannot be skipped", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostponeCyclicDay_WhenActiveFastDay_CreatesEatDayOccurrence() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateCyclic(userId, 1, 3, 16, 8, FixedNow, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 24);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current: occurrence);
        var handler = new PostponeCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            occurrenceRepo,
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new PostponeCyclicDayCommand(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("EatDay", result.Value.OccurrenceKind);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal("Postponed", occurrence.Status.ToString());
        Assert.Contains(occurrenceRepo.StoredOccurrences, x => x.Kind == FastingOccurrenceKind.EatDay);
    }

    [Fact]
    public async Task PostponeCyclicDay_WhenActiveFastDayInMultiDayPhase_CreatesNextFastDayOccurrence() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateCyclic(userId, 10, 3, 16, 8, FixedNow, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 24);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current: occurrence);
        var handler = new PostponeCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            occurrenceRepo,
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new PostponeCyclicDayCommand(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("FastDay", result.Value.OccurrenceKind);
        Assert.Equal(2, result.Value.CyclicPhaseDayNumber);
        Assert.Equal(10, result.Value.CyclicPhaseDayTotal);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal("Postponed", occurrence.Status.ToString());
        Assert.Contains(occurrenceRepo.StoredOccurrences, x => x.Kind == FastingOccurrenceKind.FastDay && x.SequenceNumber == 2);
    }

    [Fact]
    public async Task PostponeCyclicDay_WhenActiveLastEatDay_StartsFastPhaseFromFirstDay() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateCyclic(userId, 10, 10, 16, 8, FixedNow, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.EatDay, FixedNow, 20, 8);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current: occurrence);
        var handler = new PostponeCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            occurrenceRepo,
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new PostponeCyclicDayCommand(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("FastDay", result.Value.OccurrenceKind);
        Assert.Equal(1, result.Value.CyclicPhaseDayNumber);
        Assert.Equal(10, result.Value.CyclicPhaseDayTotal);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal("Postponed", occurrence.Status.ToString());
        Assert.Contains(occurrenceRepo.StoredOccurrences, x => x.Kind == FastingOccurrenceKind.FastDay && x.SequenceNumber == 21);
    }

    [Fact]
    public async Task PostponeCyclicDay_WhenActiveEatDayInMultiDayPhase_CreatesNextEatDayOccurrence() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateCyclic(userId, 2, 2, 16, 8, FixedNow, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.EatDay, FixedNow, 3, 8);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current: occurrence);
        var handler = new PostponeCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            occurrenceRepo,
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new PostponeCyclicDayCommand(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("EatDay", result.Value.OccurrenceKind);
        Assert.Equal(2, result.Value.CyclicPhaseDayNumber);
        Assert.Equal(2, result.Value.CyclicPhaseDayTotal);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal("Postponed", occurrence.Status.ToString());
        Assert.Contains(occurrenceRepo.StoredOccurrences, x => x.Kind == FastingOccurrenceKind.EatDay && x.SequenceNumber == 4);
    }

    [Fact]
    public async Task PostponeCyclicDay_WithDeletedUser_ReturnsAccountDeleted() {
        User user = CreateUser(UserId.New());
        user.DeleteAccount(FixedNow);
        var plan = FastingPlan.CreateCyclic(user.Id, 1, 3, 16, 8, FixedNow, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow, 1, 24);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current: occurrence);
        var handler = new PostponeCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            occurrenceRepo,
            CreateCurrentUserAccessService(user),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(
            new PostponeCyclicDayCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Equal(FastingOccurrenceStatus.Active, occurrence.Status);
        Assert.Single(occurrenceRepo.StoredOccurrences);
    }

    [Fact]
    public async Task PostponeCyclicDay_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new PostponeCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(),
            new InMemoryFastingOccurrenceRepository(),
            CreateCurrentUserAccessService(UserId.New()),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(new PostponeCyclicDayCommand(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task PostponeCyclicDay_WhenNoCurrentOccurrence_ReturnsNoActiveSession() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateCyclic(userId, 1, 3, 16, 8, FixedNow, FixedNow);
        var handler = new PostponeCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(new PostponeCyclicDayCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Fasting.NoActiveSession", result.Error.Code);
    }

    [Fact]
    public async Task PostponeCyclicDay_WhenCurrentPlanCannotBeLoaded_ReturnsNoActiveSession() {
        var userId = UserId.New();
        var occurrence = FastingOccurrence.Create(FastingPlanId.New(), userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 24);
        var handler = new PostponeCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(new PostponeCyclicDayCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Fasting.NoActiveSession", result.Error.Code);
    }

    [Fact]
    public async Task PostponeCyclicDay_WithIntermittentPlan_ReturnsInvalidCyclicAction() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateIntermittent(userId, FastingProtocol.F16_8, 16, 8, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, FixedNow, 1, 16);
        var handler = new PostponeCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(new PostponeCyclicDayCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Fasting.InvalidCyclicAction", result.Error.Code);
    }

    [Fact]
    public async Task PostponeCyclicDay_WhenOccurrenceCannotBePostponed_ReturnsInvalidCyclicAction() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateCyclic(userId, 1, 3, 16, 8, FixedNow, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 24);
        occurrence.Complete(FixedNow.AddHours(1));
        var handler = new PostponeCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new PassthroughCurrentFastingOccurrenceRepository(occurrence),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider());

        Result<FastingSessionModel> result = await handler.Handle(new PostponeCyclicDayCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Fasting.InvalidCyclicAction", result.Error.Code);
        Assert.Contains("cannot be postponed", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostponeCyclicDay_WhenNextDateCannotBeCalculated_ReturnsInvalidCyclicAction() {
        var userId = UserId.New();
        var now = DateTime.SpecifyKind(DateTime.MaxValue.Date, DateTimeKind.Utc);
        var plan = FastingPlan.CreateCyclic(userId, 1, 3, 16, 8, FixedNow, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 24);
        var handler = new PostponeCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            CreateCurrentUserAccessService(userId),
            new FixedDateTimeProvider(now));

        Result<FastingSessionModel> result = await handler.Handle(new PostponeCyclicDayCommand(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Fasting.InvalidCyclicAction", result.Error.Code);
        Assert.Contains("later date", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

}

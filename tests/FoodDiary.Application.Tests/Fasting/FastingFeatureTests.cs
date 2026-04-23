using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Abstractions.Persistence;
using FoodDiary.Application.Fasting.Commands.EndFasting;
using FoodDiary.Application.Fasting.Commands.ExtendActiveFasting;
using FoodDiary.Application.Fasting.Commands.PostponeCyclicDay;
using FoodDiary.Application.Fasting.Commands.ReduceActiveFastingTarget;
using FoodDiary.Application.Fasting.Commands.SkipCyclicDay;
using FoodDiary.Application.Fasting.Commands.StartFasting;
using FoodDiary.Application.Fasting.Common;
using FoodDiary.Application.Fasting.Queries.GetCurrentFasting;
using FoodDiary.Application.Fasting.Queries.GetFastingHistory;
using FoodDiary.Application.Fasting.Queries.GetFastingInsights;
using FoodDiary.Application.Fasting.Queries.GetFastingOverview;
using FoodDiary.Application.Fasting.Queries.GetFastingStats;
using FoodDiary.Application.Fasting.Services;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace FoodDiary.Application.Tests.Fasting;

public class FastingFeatureTests {
    private static readonly DateTime FixedNow = new(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task StartFasting_WithValidData_CreatesSession() {
        var user = User.Create("user@example.com", "hash");
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(planRepo, occurrenceRepo, new StubUserRepository(user), new FixedDateTimeProvider(), new StubUnitOfWork());

        var result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "F16_8", null, null, null, null, null, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("F16_8", result.Value.Protocol);
        Assert.Equal(16, result.Value.InitialPlannedDurationHours);
        Assert.Equal(0, result.Value.AddedDurationHours);
        Assert.Equal(16, result.Value.PlannedDurationHours);
        Assert.False(result.Value.IsCompleted);
        Assert.Equal("Active", result.Value.Status);
    }

    [Fact]
    public async Task StartFasting_WithCustomIntermittent_CreatesSession() {
        var user = User.Create("user@example.com", "hash");
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(planRepo, occurrenceRepo, new StubUserRepository(user), new FixedDateTimeProvider(), new StubUnitOfWork());

        var result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "CustomIntermittent", null, 17, null, null, null, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("CustomIntermittent", result.Value.Protocol);
        Assert.Equal(17, result.Value.InitialPlannedDurationHours);
        Assert.Equal(17, result.Value.PlannedDurationHours);
        Assert.Equal("Active", result.Value.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(24)]
    [InlineData(30)]
    public async Task StartFasting_WithInvalidCustomIntermittentDuration_ReturnsFailure(int duration) {
        var user = User.Create("user@example.com", "hash");
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(planRepo, occurrenceRepo, new StubUserRepository(user), new FixedDateTimeProvider(), new StubUnitOfWork());

        var result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "CustomIntermittent", null, duration, null, null, null, null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task StartFasting_WhenAlreadyActive_ReturnsFailure() {
        var user = User.Create("user@example.com", "hash");
        var existingPlan = FastingPlan.CreateIntermittent(user.Id, FastingProtocol.F16_8, 16, 8, FixedNow);
        var planRepo = new InMemoryFastingPlanRepository(active: existingPlan);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(planRepo, occurrenceRepo, new StubUserRepository(user), new FixedDateTimeProvider(), new StubUnitOfWork());

        var result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "F18_6", null, null, null, null, null, null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("AlreadyActive", result.Error.Code);
    }

    [Fact]
    public async Task StartFasting_WithInvalidProtocol_ReturnsFailure() {
        var user = User.Create("user@example.com", "hash");
        var handler = new StartFastingCommandHandler(
            new InMemoryFastingPlanRepository(), new InMemoryFastingOccurrenceRepository(), new StubUserRepository(user), new FixedDateTimeProvider(), new StubUnitOfWork());

        var result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, "InvalidProtocol", null, null, null, null, null, null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task StartFasting_WithNullUserId_ReturnsFailure() {
        var handler = new StartFastingCommandHandler(
            new InMemoryFastingPlanRepository(), new InMemoryFastingOccurrenceRepository(), new StubUserRepository(null), new FixedDateTimeProvider(), new StubUnitOfWork());

        var result = await handler.Handle(
            new StartFastingCommand(null, "F16_8", null, null, null, null, null, null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task StartFasting_WithCyclicPlan_CreatesCyclicSession() {
        var user = User.Create("user@example.com", "hash");
        var planRepo = new InMemoryFastingPlanRepository();
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        var handler = new StartFastingCommandHandler(planRepo, occurrenceRepo, new StubUserRepository(user), new FixedDateTimeProvider(), new StubUnitOfWork());

        var result = await handler.Handle(
            new StartFastingCommand(user.Id.Value, null, "Cyclic", null, 1, 3, 16, 8, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Cyclic", result.Value.PlanType);
        Assert.Equal("FastDay", result.Value.OccurrenceKind);
        Assert.Equal(1, result.Value.CyclicFastDays);
        Assert.Equal(3, result.Value.CyclicEatDays);
    }

    [Fact]
    public async Task EndFasting_WhenActiveSession_Succeeds() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateIntermittent(userId, FastingProtocol.F16_8, 16, 8, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, FixedNow, 1, 16);
        var handler = new EndFastingCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            new FixedDateTimeProvider(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new EndFastingCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
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
            new FixedDateTimeProvider(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new EndFastingCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
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
            new FixedDateTimeProvider(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new EndFastingCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
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
            new FixedDateTimeProvider(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new EndFastingCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsCompleted);
        Assert.Equal("Interrupted", result.Value.Status);
    }

    [Fact]
    public async Task EndFasting_WhenNoActiveSession_ReturnsFailure() {
        var handler = new EndFastingCommandHandler(
            new InMemoryFastingPlanRepository(), new InMemoryFastingOccurrenceRepository(), new FixedDateTimeProvider(), new StubUnitOfWork());

        var result = await handler.Handle(
            new EndFastingCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NoActiveSession", result.Error.Code);
    }

    [Fact]
    public async Task ExtendActiveFasting_WhenSessionIsActive_Succeeds() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateExtended(userId, FastingProtocol.F72_0, 72, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 72);
        var handler = new ExtendActiveFastingCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new ExtendActiveFastingCommand(userId.Value, 24), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(72, result.Value.InitialPlannedDurationHours);
        Assert.Equal(24, result.Value.AddedDurationHours);
        Assert.Equal(96, result.Value.PlannedDurationHours);
    }

    [Fact]
    public async Task ExtendActiveFasting_WhenNoActiveSession_ReturnsFailure() {
        var handler = new ExtendActiveFastingCommandHandler(
            new InMemoryFastingPlanRepository(),
            new InMemoryFastingOccurrenceRepository(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new ExtendActiveFastingCommand(Guid.NewGuid(), 24), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NoActiveSession", result.Error.Code);
    }

    [Fact]
    public async Task ReduceActiveFastingTarget_WhenSessionIsActive_Succeeds() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateExtended(userId, FastingProtocol.F72_0, 72, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 72);
        var handler = new ReduceActiveFastingTargetCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            new FixedDateTimeProvider(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new ReduceActiveFastingTargetCommand(userId.Value, 8), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(72, result.Value.InitialPlannedDurationHours);
        Assert.Equal(-8, result.Value.AddedDurationHours);
        Assert.Equal(64, result.Value.PlannedDurationHours);
        Assert.Equal("Active", result.Value.Status);
    }

    [Fact]
    public async Task ReduceActiveFastingTarget_WhenNewTargetAlreadyReached_CompletesSession() {
        var userId = UserId.New();
        var now = FixedNow;
        var plan = FastingPlan.CreateExtended(userId, FastingProtocol.F36_0, 36, now.AddHours(-30));
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, now.AddHours(-30), 1, 36);
        var handler = new ReduceActiveFastingTargetCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            new InMemoryFastingOccurrenceRepository(current: occurrence),
            new FixedDateTimeProvider(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new ReduceActiveFastingTargetCommand(userId.Value, 8), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Completed", result.Value.Status);
        Assert.NotNull(result.Value.EndedAtUtc);
        Assert.Equal(FastingPlanStatus.Stopped, plan.Status);
    }

    [Fact]
    public async Task SkipCyclicDay_WhenActiveFastDay_CreatesEatDayOccurrence() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateCyclic(userId, 1, 3, 16, 8, FixedNow, FixedNow);
        var occurrence = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastDay, FixedNow, 1, 24);
        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current: occurrence);
        var handler = new SkipCyclicDayCommandHandler(
            new InMemoryFastingPlanRepository(active: plan),
            occurrenceRepo,
            new FixedDateTimeProvider(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new SkipCyclicDayCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
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
            new FixedDateTimeProvider(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new SkipCyclicDayCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("FastDay", result.Value.OccurrenceKind);
        Assert.Equal(1, result.Value.CyclicPhaseDayNumber);
        Assert.Equal(10, result.Value.CyclicPhaseDayTotal);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal("Skipped", occurrence.Status.ToString());
        Assert.Contains(occurrenceRepo.StoredOccurrences, x => x.Kind == FastingOccurrenceKind.FastDay && x.SequenceNumber == 21);
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
            new FixedDateTimeProvider(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new PostponeCyclicDayCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
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
            new FixedDateTimeProvider(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new PostponeCyclicDayCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
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
            new FixedDateTimeProvider(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new PostponeCyclicDayCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
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
            new FixedDateTimeProvider(),
            new StubUnitOfWork());

        var result = await handler.Handle(
            new PostponeCyclicDayCommand(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("EatDay", result.Value.OccurrenceKind);
        Assert.Equal(2, result.Value.CyclicPhaseDayNumber);
        Assert.Equal(2, result.Value.CyclicPhaseDayTotal);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal("Postponed", occurrence.Status.ToString());
        Assert.Contains(occurrenceRepo.StoredOccurrences, x => x.Kind == FastingOccurrenceKind.EatDay && x.SequenceNumber == 4);
    }

    [Fact]
    public async Task GetFastingInsights_WithCurrentAndHistory_ReturnsInsightsAndPrompt() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateIntermittent(userId, FastingProtocol.F16_8, 16, 8, FixedNow.AddDays(-5));
        var current = FastingOccurrence.Create(
            plan.Id,
            userId,
            FastingOccurrenceKind.FastingWindow,
            FixedNow.AddHours(-13),
            1,
            16);
        var historyOne = FastingOccurrence.Create(
            plan.Id,
            userId,
            FastingOccurrenceKind.FastingWindow,
            FixedNow.AddDays(-3),
            1,
            16);
        historyOne.UpdateCheckIn(5, 5, 5, ["headache"], "note", FixedNow.AddDays(-3).AddHours(8));
        historyOne.Complete(FixedNow.AddDays(-3).AddHours(16));

        var historyTwo = FastingOccurrence.Create(
            plan.Id,
            userId,
            FastingOccurrenceKind.FastingWindow,
            FixedNow.AddDays(-2),
            1,
            16);
        historyTwo.UpdateCheckIn(4, 4, 4, ["headache"], "note", FixedNow.AddDays(-2).AddHours(8));
        historyTwo.Complete(FixedNow.AddDays(-2).AddHours(16));

        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current: current);
        occurrenceRepo.StoredOccurrences.InsertRange(0, [historyOne, historyTwo]);
        var handler = new GetFastingInsightsQueryHandler(
            occurrenceRepo,
            new FastingAnalyticsService(occurrenceRepo, new InMemoryFastingCheckInRepository()),
            new FixedDateTimeProvider());

        var result = await handler.Handle(new GetFastingInsightsQuery(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value.Alerts, x => x.Id == "mid");
        Assert.Contains(result.Value.Insights, x => x.Id == "symptom-headache");
    }

    [Fact]
    public async Task GetCurrentFasting_WithActiveSession_ReturnsSessionWithCheckIns() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateIntermittent(userId, FastingProtocol.F16_8, 16, 8, FixedNow.AddDays(-1));
        var current = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, FixedNow.AddHours(-4), 1, 16);
        var checkIn = FastingCheckIn.Create(current.Id, userId, 2, 4, 4, ["weakness"], "steady", FixedNow.AddHours(-1));
        var handler = new GetCurrentFastingQueryHandler(
            new InMemoryFastingOccurrenceRepository(current),
            new InMemoryFastingCheckInRepository(checkIn));

        var result = await handler.Handle(new GetCurrentFastingQuery(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(current.Id.Value, result.Value!.Id);
        Assert.Single(result.Value.CheckIns);
        Assert.Equal(2, result.Value.CheckIns[0].HungerLevel);
    }

    [Fact]
    public async Task GetFastingHistory_ReturnsPagedSessionsWithCheckIns() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateIntermittent(userId, FastingProtocol.F16_8, 16, 8, FixedNow.AddDays(-10));
        var latest = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, FixedNow.AddDays(-1), 1, 16);
        latest.Complete(FixedNow.AddDays(-1).AddHours(16));
        var earlier = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, FixedNow.AddDays(-2), 1, 16);
        earlier.Complete(FixedNow.AddDays(-2).AddHours(16));

        var latestCheckIn = FastingCheckIn.Create(latest.Id, userId, 3, 4, 5, ["good"], "latest", FixedNow.AddDays(-1).AddHours(8));
        var earlierCheckIn = FastingCheckIn.Create(earlier.Id, userId, 2, 3, 4, ["headache"], "earlier", FixedNow.AddDays(-2).AddHours(8));

        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        occurrenceRepo.StoredOccurrences.AddRange([latest, earlier]);
        var handler = new GetFastingHistoryQueryHandler(
            new FastingAnalyticsService(
                occurrenceRepo,
                new InMemoryFastingCheckInRepository(latestCheckIn, earlierCheckIn)));

        var result = await handler.Handle(
            new GetFastingHistoryQuery(userId.Value, FixedNow.AddDays(-7), FixedNow, 1, 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Data);
        Assert.Equal(2, result.Value.TotalItems);
        Assert.Equal(2, result.Value.TotalPages);
        Assert.Single(result.Value.Data[0].CheckIns);
        Assert.Equal("latest", result.Value.Data[0].CheckIns[0].Notes);
    }

    [Fact]
    public async Task GetFastingStats_ComputesRatesAndTopSymptom() {
        var userId = UserId.New();
        var now = FixedNow;

        var oldCompleted = FastingOccurrence.Create(FastingPlanId.New(), userId, FastingOccurrenceKind.FastingWindow, now.AddDays(-40), 1, 16);
        oldCompleted.Complete(now.AddDays(-40).AddHours(16));

        var completedOne = FastingOccurrence.Create(FastingPlanId.New(), userId, FastingOccurrenceKind.FastingWindow, now.AddDays(-2), 1, 16);
        completedOne.Complete(now.AddDays(-2).AddHours(16));

        var completedTwo = FastingOccurrence.Create(FastingPlanId.New(), userId, FastingOccurrenceKind.FastingWindow, now.AddDays(-1), 1, 16);
        completedTwo.Complete(now.AddDays(-1).AddHours(16));

        var active = FastingOccurrence.Create(FastingPlanId.New(), userId, FastingOccurrenceKind.FastingWindow, now, 1, 16);
        var oldCheckIn = FastingCheckIn.Create(oldCompleted.Id, userId, 3, 4, 4, ["weakness"], "old", now.AddDays(-40).AddHours(8));
        var completedOneCheckIn = FastingCheckIn.Create(completedOne.Id, userId, 2, 4, 4, ["dizziness"], "one", now.AddDays(-2).AddHours(8));
        var completedTwoCheckIn = FastingCheckIn.Create(completedTwo.Id, userId, 3, 5, 5, ["dizziness"], "two", now.AddDays(-1).AddHours(8));

        var occurrenceRepo = new InMemoryFastingOccurrenceRepository();
        occurrenceRepo.StoredOccurrences.AddRange([oldCompleted, completedOne, completedTwo, active]);
        var handler = new GetFastingStatsQueryHandler(
            new FastingAnalyticsService(
                occurrenceRepo,
                new InMemoryFastingCheckInRepository(oldCheckIn, completedOneCheckIn, completedTwoCheckIn)),
            new FixedDateTimeProvider());

        var result = await handler.Handle(new GetFastingStatsQuery(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.TotalCompleted);
        Assert.Equal(2, result.Value.CurrentStreak);
        Assert.Equal(66.7, result.Value.CompletionRateLast30Days);
        Assert.Equal(66.7, result.Value.CheckInRateLast30Days);
        Assert.Equal("dizziness", result.Value.TopSymptom);
        Assert.Equal(now.AddDays(-1).AddHours(8), result.Value.LastCheckInAtUtc);
    }

    [Fact]
    public async Task GetFastingOverview_ReturnsCurrentStatsInsightsAndHistory() {
        var userId = UserId.New();
        var plan = FastingPlan.CreateIntermittent(userId, FastingProtocol.F16_8, 16, 8, FixedNow.AddDays(-5));
        var current = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, FixedNow.AddHours(-13), 1, 16);
        current.UpdateCheckIn(2, 2, 2, ["weakness"], "current", FixedNow.AddHours(-1));
        var currentCheckIn = FastingCheckIn.Create(current.Id, userId, 2, 2, 2, ["weakness"], "current", FixedNow.AddHours(-1));

        var historyOne = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, FixedNow.AddDays(-3), 1, 16);
        historyOne.UpdateCheckIn(5, 5, 5, ["headache"], "history-one", FixedNow.AddDays(-3).AddHours(8));
        historyOne.Complete(FixedNow.AddDays(-3).AddHours(16));

        var historyTwo = FastingOccurrence.Create(plan.Id, userId, FastingOccurrenceKind.FastingWindow, FixedNow.AddDays(-2), 1, 16);
        historyTwo.UpdateCheckIn(4, 4, 4, ["headache"], "history-two", FixedNow.AddDays(-2).AddHours(8));
        historyTwo.Complete(FixedNow.AddDays(-2).AddHours(16));

        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(current);
        occurrenceRepo.StoredOccurrences.InsertRange(0, [historyOne, historyTwo]);
        var checkInRepo = new InMemoryFastingCheckInRepository(currentCheckIn);
        var handler = new GetFastingOverviewQueryHandler(
            occurrenceRepo,
            checkInRepo,
            new FastingAnalyticsService(occurrenceRepo, checkInRepo),
            new FixedDateTimeProvider());

        var result = await handler.Handle(new GetFastingOverviewQuery(userId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.CurrentSession);
        Assert.Single(result.Value.CurrentSession!.CheckIns);
        Assert.Equal(2, result.Value.Stats.TotalCompleted);
        Assert.Contains(result.Value.Insights.Alerts, x => x.Id == "current-warning");
        Assert.Contains(result.Value.Insights.Insights, x => x.Id == "symptom-headache");
        Assert.Equal(1, result.Value.History.Page);
        Assert.True(result.Value.History.Data.Count >= 3);
    }

    [Fact]
    public void GetDefaultHistoryWindow_UsesCanonicalUtcMonthWindow() {
        var service = new FastingAnalyticsService(new InMemoryFastingOccurrenceRepository(), new InMemoryFastingCheckInRepository());
        var (fromUtc, toUtc) = service.GetDefaultHistoryWindow(new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc), fromUtc);
        Assert.Equal(new DateTime(2026, 2, 28, 23, 59, 59, 999, DateTimeKind.Utc).AddTicks(9999), toUtc);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenOccurrenceHasRealCheckIn_SuppressesCheckInReminder() {
        var user = User.Create("fasting-notifications@example.com", "hash");
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddDays(-1));
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-13), 1, 36);
        AttachNavigation(occurrence, plan, user);

        var occurrenceRepo = new InMemoryFastingOccurrenceRepository(occurrence);
        var checkInRepo = new InMemoryFastingCheckInRepository(
            FastingCheckIn.Create(occurrence.Id, user.Id, 2, 4, 4, ["weakness"], "steady", FixedNow.AddHours(-1)));
        var notificationRepo = new InMemorySchedulerNotificationRepository();
        var notificationPusher = new RecordingNotificationPusher();
        var webPushSender = new RecordingWebPushNotificationSender();
        var scheduler = new FastingNotificationScheduler(
            occurrenceRepo,
            checkInRepo,
            notificationRepo,
            notificationPusher,
            webPushSender,
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        var created = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, created);
        Assert.Empty(notificationRepo.Stored);
        Assert.Empty(webPushSender.Sent);
        Assert.Empty(notificationPusher.ChangedUsers);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenOnlyLegacySummaryCheckInExists_SuppressesCheckInReminder() {
        var user = User.Create("fasting-summary@example.com", "hash");
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddDays(-1));
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-13), 1, 36);
        occurrence.UpdateCheckIn(2, 4, 4, ["weakness"], "legacy", FixedNow.AddHours(-2));
        AttachNavigation(occurrence, plan, user);

        var notificationRepo = new InMemorySchedulerNotificationRepository();
        var notificationPusher = new RecordingNotificationPusher();
        var webPushSender = new RecordingWebPushNotificationSender();
        var scheduler = new FastingNotificationScheduler(
            new InMemoryFastingOccurrenceRepository(occurrence),
            new InMemoryFastingCheckInRepository(),
            notificationRepo,
            notificationPusher,
            webPushSender,
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        var created = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(0, created);
        Assert.Empty(notificationRepo.Stored);
        Assert.Empty(webPushSender.Sent);
        Assert.Empty(notificationPusher.ChangedUsers);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenNoCheckInExists_CreatesReminderAndPushesUnreadCount() {
        var user = User.Create("fasting-reminder@example.com", "hash");
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddDays(-1));
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-13), 1, 36);
        AttachNavigation(occurrence, plan, user);

        var notificationRepo = new InMemorySchedulerNotificationRepository();
        var notificationPusher = new RecordingNotificationPusher();
        var webPushSender = new RecordingWebPushNotificationSender();
        var scheduler = new FastingNotificationScheduler(
            new InMemoryFastingOccurrenceRepository(occurrence),
            new InMemoryFastingCheckInRepository(),
            notificationRepo,
            notificationPusher,
            webPushSender,
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        var created = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(1, created);
        Assert.Single(notificationRepo.Stored);
        Assert.Single(webPushSender.Sent);
        Assert.Single(notificationPusher.UnreadCountUsers);
        Assert.Single(notificationPusher.ChangedUsers);
        Assert.Equal(user.Id.Value, notificationPusher.UnreadCountUsers[0]);
        Assert.Equal(user.Id.Value, notificationPusher.ChangedUsers[0]);
        Assert.Equal(NotificationTypes.FastingCheckInReminder, notificationRepo.Stored[0].Type);
        Assert.Equal($"fasting-check-in-reminder:{occurrence.Id.Value}:{user.FastingCheckInReminderHours}", notificationRepo.Stored[0].ReferenceId);
    }

    [Fact]
    public async Task ProcessDueNotificationsAsync_WhenPastFollowUpThreshold_CreatesTwoRemindersOnceAndDeduplicatesLaterRuns() {
        var user = User.Create("fasting-reminder-thresholds@example.com", "hash");
        var plan = FastingPlan.CreateExtended(user.Id, FastingProtocol.F36_0, 36, FixedNow.AddDays(-1));
        var occurrence = FastingOccurrence.Create(plan.Id, user.Id, FastingOccurrenceKind.FastDay, FixedNow.AddHours(-21), 1, 36);
        AttachNavigation(occurrence, plan, user);

        var notificationRepo = new InMemorySchedulerNotificationRepository();
        var notificationPusher = new RecordingNotificationPusher();
        var webPushSender = new RecordingWebPushNotificationSender();
        var scheduler = new FastingNotificationScheduler(
            new InMemoryFastingOccurrenceRepository(occurrence),
            new InMemoryFastingCheckInRepository(),
            notificationRepo,
            notificationPusher,
            webPushSender,
            new FixedDateTimeProvider(),
            NullLogger<FastingNotificationScheduler>.Instance);

        var firstCreated = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);
        var secondCreated = await scheduler.ProcessDueNotificationsAsync(CancellationToken.None);

        Assert.Equal(2, firstCreated);
        Assert.Equal(0, secondCreated);
        Assert.Equal(2, notificationRepo.Stored.Count);
        Assert.Equal(2, webPushSender.Sent.Count);
        Assert.Contains(notificationRepo.Stored, x => x.ReferenceId == $"fasting-check-in-reminder:{occurrence.Id.Value}:{user.FastingCheckInReminderHours}");
        Assert.Contains(notificationRepo.Stored, x => x.ReferenceId == $"fasting-check-in-reminder:{occurrence.Id.Value}:{user.FastingCheckInFollowUpReminderHours}");
    }

    private sealed class InMemoryFastingPlanRepository(FastingPlan? active = null) : IFastingPlanRepository {
        public Task<FastingPlan?> GetActiveAsync(UserId userId, bool asTracking = false, CancellationToken ct = default) => Task.FromResult(active);
        public Task<FastingPlan?> GetByIdAsync(FastingPlanId id, bool asTracking = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<FastingPlan>> GetByUserAsync(UserId userId, FastingPlanType? type = null, FastingPlanStatus? status = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddAsync(FastingPlan plan, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(FastingPlan plan, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryFastingOccurrenceRepository(FastingOccurrence? current = null) : IFastingOccurrenceRepository {
        public List<FastingOccurrence> StoredOccurrences { get; } = current is null ? [] : [current];

        public Task<FastingOccurrence?> GetCurrentAsync(UserId userId, bool asTracking = false, CancellationToken ct = default) => Task.FromResult(StoredOccurrences.LastOrDefault(x => x.Status == FastingOccurrenceStatus.Active));
        public Task<FastingOccurrence?> GetByIdAsync(FastingOccurrenceId id, bool asTracking = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<FastingOccurrence>> GetActiveAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<FastingOccurrence>>(StoredOccurrences.Where(x => x.Status == FastingOccurrenceStatus.Active).ToList());
        public Task<IReadOnlyList<FastingOccurrence>> GetByPlanAsync(FastingPlanId planId, bool includeCompleted = true, CancellationToken ct = default) {
            IReadOnlyList<FastingOccurrence> occurrences = StoredOccurrences
                .Where(x => x.PlanId == planId)
                .ToList();
            return Task.FromResult(occurrences);
        }
        public Task<IReadOnlyList<FastingOccurrence>> GetByUserAsync(UserId userId, DateTime? from = null, DateTime? to = null, FastingOccurrenceStatus? status = null, CancellationToken ct = default) {
            var query = StoredOccurrences.Where(x => x.UserId == userId);

            if (from.HasValue) {
                query = query.Where(x => x.StartedAtUtc >= from.Value);
            }

            if (to.HasValue) {
                query = query.Where(x => x.StartedAtUtc <= to.Value);
            }

            if (status.HasValue) {
                query = query.Where(x => x.Status == status.Value);
            }

            IReadOnlyList<FastingOccurrence> occurrences = query
                .OrderByDescending(x => x.StartedAtUtc)
                .ToList();

            return Task.FromResult(occurrences);
        }
        public Task<(IReadOnlyList<FastingOccurrence> Items, int TotalItems)> GetPagedByUserAsync(
            UserId userId,
            int page,
            int limit,
            DateTime? from = null,
            DateTime? to = null,
            FastingOccurrenceStatus? status = null,
            CancellationToken ct = default) {
            var query = StoredOccurrences.Where(x => x.UserId == userId);

            if (from.HasValue) {
                query = query.Where(x => x.StartedAtUtc >= from.Value);
            }

            if (to.HasValue) {
                query = query.Where(x => x.StartedAtUtc <= to.Value);
            }

            if (status.HasValue) {
                query = query.Where(x => x.Status == status.Value);
            }

            var ordered = query
                .OrderByDescending(x => x.StartedAtUtc)
                .ToList();

            var items = ordered
                .Skip(Math.Max(0, page - 1) * limit)
                .Take(limit)
                .ToList();

            return Task.FromResult<(IReadOnlyList<FastingOccurrence> Items, int TotalItems)>((items, ordered.Count));
        }
        public Task AddAsync(FastingOccurrence occurrence, CancellationToken ct = default) {
            StoredOccurrences.Add(occurrence);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(FastingOccurrence occurrence, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class InMemoryFastingCheckInRepository(params FastingCheckIn[] seed) : IFastingCheckInRepository {
        private readonly List<FastingCheckIn> _stored = [.. seed];

        public Task AddAsync(FastingCheckIn checkIn, CancellationToken cancellationToken = default) {
            _stored.Add(checkIn);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FastingCheckIn>> GetByOccurrenceIdsAsync(
            IReadOnlyCollection<FastingOccurrenceId> occurrenceIds,
            CancellationToken cancellationToken = default) {
            IReadOnlyList<FastingCheckIn> items = _stored
                .Where(x => occurrenceIds.Contains(x.OccurrenceId))
                .OrderByDescending(x => x.CheckedInAtUtc)
                .ToList();

            return Task.FromResult(items);
        }
    }

    private sealed class InMemorySchedulerNotificationRepository : INotificationRepository {
        public List<Notification> Stored { get; } = [];

        public Task<IReadOnlyList<Notification>> GetByUserAsync(UserId userId, int limit = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Notification>>(Stored.Where(x => x.UserId == userId).Take(limit).ToList());

        public Task<Notification?> GetByIdAsync(NotificationId id, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<Notification?>(Stored.FirstOrDefault(x => x.Id == id));

        public Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default) {
            Stored.Add(notification);
            return Task.FromResult(notification);
        }

        public Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> ExistsAsync(UserId userId, string type, string referenceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored.Any(x => x.UserId == userId && x.Type == type && x.ReferenceId == referenceId));

        public Task<int> GetUnreadCountAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored.Count(x => x.UserId == userId && !x.IsRead));

        public Task<int> GetUnreadCountAsync(UserId userId, string type, CancellationToken cancellationToken = default) =>
            Task.FromResult(Stored.Count(x => x.UserId == userId && !x.IsRead && x.Type == type));

        public Task MarkAllReadAsync(UserId userId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> DeleteExpiredBatchAsync(
            IReadOnlyCollection<string> transientTypes,
            DateTime transientReadOlderThanUtc,
            DateTime transientUnreadOlderThanUtc,
            DateTime standardReadOlderThanUtc,
            DateTime standardUnreadOlderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class RecordingNotificationPusher : INotificationPusher {
        public List<Guid> UnreadCountUsers { get; } = [];
        public List<Guid> ChangedUsers { get; } = [];

        public Task PushUnreadCountAsync(Guid userId, int count, CancellationToken cancellationToken = default) {
            UnreadCountUsers.Add(userId);
            return Task.CompletedTask;
        }

        public Task PushNotificationsChangedAsync(Guid userId, CancellationToken cancellationToken = default) {
            ChangedUsers.Add(userId);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingWebPushNotificationSender : IWebPushNotificationSender {
        public List<Notification> Sent { get; } = [];

        public Task SendAsync(Notification notification, CancellationToken cancellationToken = default) {
            Sent.Add(notification);
            return Task.CompletedTask;
        }
    }

    private sealed class StubUnitOfWork : IUnitOfWork {
        public bool HasPendingChanges => false;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubUserRepository(User? user) : IUserRepository {
        public Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default) => Task.FromResult(user);
        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FixedDateTimeProvider : IDateTimeProvider {
        public DateTime UtcNow => FixedNow;
    }

    private static void AttachNavigation(FastingOccurrence occurrence, FastingPlan plan, User user) {
        SetPrivateProperty(occurrence, nameof(FastingOccurrence.Plan), plan);
        SetPrivateProperty(occurrence, nameof(FastingOccurrence.User), user);
    }

    private static void SetPrivateProperty<TTarget, TValue>(TTarget target, string propertyName, TValue value) {
        var property = typeof(TTarget).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        property!.SetValue(target, value);
    }
}
